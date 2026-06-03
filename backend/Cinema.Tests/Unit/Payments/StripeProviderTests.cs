using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Stripe;

namespace Cinema.Tests.Unit.Payments;

public sealed class StripeProviderTests
{
    private static StripeProvider BuildProvider(
        MockHttpMessageHandler mockHttp,
        string webhookSecret = "whsec_test")
    {
        var httpClient   = new HttpClient(mockHttp) { BaseAddress = new Uri("https://api.stripe.com/") };
        var stripeClient = new StripeClient("sk_test_key",
            httpClient: new SystemNetHttpClient(httpClient));

        var opts = Options.Create(new StripeOptions
        {
            SecretKey     = "sk_test_key",
            WebhookSecret = webhookSecret,
        });

        return new StripeProvider(stripeClient, new NoVerificationStripeWebhookVerifier(), opts);
    }

    private static List<Ticket> MakeTickets()
    {
        // Create tickets with minimal data; Id doesn't matter for metadata (will be empty for new)
        var t1 = new Ticket(1, 1, 1, SeatTypeCode.Standard, 100m, "qr1");
        var t2 = new Ticket(1, 2, 2, SeatTypeCode.Vip, 150m, "qr2");
        // Use reflection to set Id for testing (since EF would assign)
        var t1IdProp = typeof(Ticket).GetProperty("Id")!;
        var t2IdProp = typeof(Ticket).GetProperty("Id")!;
        t1IdProp.SetValue(t1, 101);
        t2IdProp.SetValue(t2, 102);

        return new List<Ticket> { t1, t2 };
    }

    [Fact]
    public async Task CreateIntentAsync_ReturnsClientSecretAndMetadata()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents")
            .Respond("application/json", """
            {
              "id": "pi_test_123",
              "client_secret": "pi_test_123_secret_abc",
              "status": "requires_payment_method",
              "object": "payment_intent",
              "amount": 20000,
              "currency": "uah",
              "livemode": false,
              "created": 1700000000,
              "metadata": {},
              "payment_method_types": ["card"]
            }
            """);

        var provider = BuildProvider(mock);
        var tickets  = MakeTickets();

        var result = await provider.CreateIntentAsync(tickets, 250m, "http://localhost:4200/return");

        result.ExternalId.Should().Be("pi_test_123");
        result.ClientSecret.Should().Be("pi_test_123_secret_abc");
        result.ApprovalUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateAndConfirmWithTokenAsync_WhenSucceeded_ReturnsPaymentIntentId()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api.stripe.com/v1/payment_methods")
            .Respond("application/json", """
            {
              "id": "pm_test_google_pay",
              "object": "payment_method",
              "type": "card",
              "card": {
                "brand": "visa",
                "last4": "4242",
                "exp_month": 12,
                "exp_year": 2030
              },
              "livemode": false
            }
            """);
        mock.When(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents")
            .Respond("application/json", """
            {
              "id": "pi_google_pay_succeeded",
              "object": "payment_intent",
              "amount": 25000,
              "currency": "uah",
              "status": "succeeded",
              "livemode": false,
              "created": 1700000000,
              "payment_method": "pm_test_google_pay",
              "payment_method_types": ["card"],
              "metadata": {}
            }
            """);

        var provider = BuildProvider(mock);

        var result = await provider.CreateAndConfirmWithTokenAsync(MakeTickets(), 250m, "tok_google_pay");

        result.Should().Be("pi_google_pay_succeeded");
    }

    [Fact]
    public async Task CreateAndConfirmWithTokenAsync_WhenNotSucceeded_Throws()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, "https://api.stripe.com/v1/payment_methods")
            .Respond("application/json", """
            {
              "id": "pm_test_google_pay",
              "object": "payment_method",
              "type": "card",
              "card": {
                "brand": "visa",
                "last4": "4242",
                "exp_month": 12,
                "exp_year": 2030
              },
              "livemode": false
            }
            """);
        mock.When(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents")
            .Respond("application/json", """
            {
              "id": "pi_google_pay_requires_action",
              "object": "payment_intent",
              "amount": 25000,
              "currency": "uah",
              "status": "requires_action",
              "livemode": false,
              "created": 1700000000,
              "payment_method": "pm_test_google_pay",
              "payment_method_types": ["card"],
              "metadata": {}
            }
            """);

        var provider = BuildProvider(mock);

        var act = () => provider.CreateAndConfirmWithTokenAsync(MakeTickets(), 250m, "tok_google_pay");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Google Pay payment was not completed.");
    }

    [Fact]
    public async Task EnsureSucceededAsync_WhenIntentMatches_Completes()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.stripe.com/v1/payment_intents/pi_succeeded")
            .Respond("application/json", """
            {
              "id": "pi_succeeded",
              "object": "payment_intent",
              "amount": 25000,
              "currency": "uah",
              "status": "succeeded",
              "livemode": false,
              "created": 1700000000
            }
            """);

        var provider = BuildProvider(mock);

        await provider.EnsureSucceededAsync("pi_succeeded", 250m);
    }

    [Fact]
    public async Task EnsureSucceededAsync_WhenAmountDiffers_Throws()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.stripe.com/v1/payment_intents/pi_wrong_amount")
            .Respond("application/json", """
            {
              "id": "pi_wrong_amount",
              "object": "payment_intent",
              "amount": 100,
              "currency": "uah",
              "status": "succeeded",
              "livemode": false,
              "created": 1700000000
            }
            """);

        var provider = BuildProvider(mock);
        var act = () => provider.EnsureSucceededAsync("pi_wrong_amount", 250m);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Stripe payment was not completed or has an invalid amount.");
    }

    [Fact]
    public async Task HandleWebhookAsync_PaymentIntentSucceeded_ReturnsCompleted()
    {
        var mock     = new MockHttpMessageHandler();
        var provider = BuildProvider(mock);

        var payload = """
        {
          "id": "evt_test",
          "type": "payment_intent.succeeded",
          "api_version": "2024-06-20",
          "object": "event",
          "data": {
            "object": {
              "id": "pi_test_123",
              "object": "payment_intent",
              "amount": 20000,
              "currency": "uah",
              "status": "succeeded",
              "livemode": false,
              "created": 1700000000,
              "metadata": {}
            }
          }
        }
        """;

        var result = await provider.HandleWebhookAsync(payload, new Dictionary<string, string>());

        result.IsValid.Should().BeTrue();
        result.IsCompleted.Should().BeTrue();
        result.ExternalId.Should().Be("pi_test_123");
    }

    [Fact]
    public async Task HandleWebhookAsync_UnknownEventType_ReturnsNotCompleted()
    {
        var provider = BuildProvider(new MockHttpMessageHandler());

        var payload = """
        {
          "id": "evt_other",
          "type": "customer.created",
          "api_version": "2024-06-20",
          "object": "event",
          "data": { "object": { "id": "cus_1", "object": "customer" } }
        }
        """;

        var result = await provider.HandleWebhookAsync(payload, new Dictionary<string, string>());

        result.IsValid.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
    }
}
