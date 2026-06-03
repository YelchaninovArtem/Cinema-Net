using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Payments;
using Cinema.Application.Payments;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using System.Net.Http.Json;

namespace Cinema.Tests.Unit.Payments;

/// <summary>
/// Test verifier that always succeeds, used for unit tests.
/// </summary>
internal sealed class TestPayPalWebhookVerifier : IPayPalWebhookVerifier
{
    public Task<bool> VerifyAsync(string payload, IReadOnlyDictionary<string, string> headers, string clientId, string secret, CancellationToken ct = default)
        => Task.FromResult(true);
}

/// <summary>
/// Stub exchange rate service returning a fixed rate for UAH→USD.
/// </summary>
internal sealed class TestExchangeRateService : IExchangeRateService
{
    private readonly decimal _rate;
    public TestExchangeRateService(decimal rate = 0.0225m) => _rate = rate;
    public Task<ExchangeRateDto> GetRateAsync(string baseCurrency, string targetCurrency, CancellationToken ct = default)
        => Task.FromResult(new ExchangeRateDto(baseCurrency, targetCurrency, _rate, DateTime.UtcNow));
}

public sealed class PayPalProviderTests
{
    private static PayPalProvider BuildProvider(MockHttpMessageHandler mockHttp)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("https://api-m.sandbox.paypal.com/")
        };
        var opts = Options.Create(new PayPalOptions
        {
            ClientId     = "test-client-id",
            ClientSecret = "test-client-secret",
            BaseUrl      = "https://api-m.sandbox.paypal.com",
            WebhookId    = "test-webhook-id",
            FallbackUsdToUahRate = 44.5m
        });
        var verifier    = new TestPayPalWebhookVerifier();
        var exchangeRate = new TestExchangeRateService(0.0225m); // approx 1/44.5
        return new PayPalProvider(httpClient, opts, verifier, exchangeRate);
    }

    private static List<Ticket> MakeTickets()
    {
        // Create tickets; set Id via reflection because EF normally does it
        var t1 = new Ticket(1, 1, 1, SeatTypeCode.Standard, 100m, "qr1");
        var t2 = new Ticket(1, 2, 2, SeatTypeCode.Vip, 150m, "qr2");
        var idProp = typeof(Ticket).GetProperty("Id")!;
        idProp.SetValue(t1, 201);
        idProp.SetValue(t2, 202);
        return new List<Ticket> { t1, t2 };
    }

    private static string FakeTokenResponse() => """
        { "access_token": "fake-access-token", "token_type": "Bearer", "expires_in": 32400 }
        """;

    private static string FakeOrderResponse(string orderId) => $$"""
        {
          "id": "{{orderId}}",
          "status": "CREATED",
          "links": [
            { "href": "https://api.paypal.com/v2/checkout/orders/{{orderId}}", "rel": "self",    "method": "GET"  },
            { "href": "https://www.sandbox.paypal.com/checkoutnow?token={{orderId}}", "rel": "approve", "method": "GET" },
            { "href": "https://api.paypal.com/v2/checkout/orders/{{orderId}}", "rel": "update",  "method": "PATCH"},
            { "href": "https://api.paypal.com/v2/checkout/orders/{{orderId}}/capture", "rel": "capture", "method": "POST" }
          ]
        }
        """;

    [Fact]
    public async Task CreateIntentAsync_ReturnsOrderIdAndApprovalUrl()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v1/oauth2/token")
            .Respond("application/json", FakeTokenResponse());
        mock.When(HttpMethod.Post, "https://api-m.sandbox.paypal.com/v2/checkout/orders")
            .Respond("application/json", FakeOrderResponse("ORDER-ABC-123"));

        var provider = BuildProvider(mock);
        var tickets  = MakeTickets();

        var result = await provider.CreateIntentAsync(tickets, 250m, "http://localhost:4200/return");

        result.ExternalId.Should().Be("ORDER-ABC-123");
        result.ClientSecret.Should().BeNull();
        result.ApprovalUrl.Should().Contain("ORDER-ABC-123");
    }

    [Fact]
    public async Task HandleWebhookAsync_CaptureCompleted_ReturnsCompleted()
    {
        var provider = BuildProvider(new MockHttpMessageHandler());

        // PayPal sends event name PAYMENT.CAPTURE.COMPLETED
        var payload = """
        {
          "id": "evt_capture",
          "event_type": "PAYMENT.CAPTURE.COMPLETED",
          "resource": {
            "supplementary_data": {
              "related_ids": {
                "order_id": "ORDER-123"
              }
            }
          }
        }
        """;

        var result = await provider.HandleWebhookAsync(payload, new Dictionary<string, string>());

        result.IsValid.Should().BeTrue();
        result.IsCompleted.Should().BeTrue();
        result.ExternalId.Should().Be("ORDER-123");
    }

    [Fact]
    public async Task HandleWebhookAsync_UnknownEvent_ReturnsNotCompleted()
    {
        var provider = BuildProvider(new MockHttpMessageHandler());

        var payload = """
        {
          "id": "evt_other",
          "event_type": "CUSTOMER.DISPUTE.CREATED",
          "resource": {}
        }
        """;

        var result = await provider.HandleWebhookAsync(payload, new Dictionary<string, string>());

        result.IsValid.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
    }
}