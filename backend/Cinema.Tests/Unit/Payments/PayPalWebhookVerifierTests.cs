using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Cinema.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Cinema.Tests.Unit.Payments;

public sealed class PayPalWebhookVerifierTests
{
    private static PayPalWebhookVerifier BuildVerifier(MockHttpMessageHandler mockHttp, string webhookId, string baseUrl)
    {
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri(baseUrl)
        };
        var factory = new MockHttpClientFactory(httpClient);
        var options = Options.Create(new PayPalOptions
        {
            WebhookId = webhookId,
            BaseUrl   = baseUrl
        });
        var logger = NullLogger<PayPalWebhookVerifier>.Instance;
        return new PayPalWebhookVerifier(factory, logger, options);
    }

    [Fact]
    public async Task VerifyAsync_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var webhookId = "wh-test-123";
        var baseUrl   = "https://api-m.sandbox.paypal.com";
        var payload   = """{ "event_type": "PAYMENT.CAPTURE.COMPLETED", "resource": {} }""";
        var headers = new Dictionary<string, string>
        {
            ["PAYPAL-TRANSMISSION-ID"] = "trans-123",
            ["PAYPAL-TRANSMISSION-TIME"] = "2024-01-01T12:00:00Z",
            ["PAYPAL-TRANSMISSION-SIG"] = "sha256=abc123",
            ["PAYPAL-CERT-URL"] = "https://api.paypal.com/v1/notifications/certs/CERT-123",
            ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{baseUrl}/v1/notifications/verify-webhook-signature")
            .Respond(HttpStatusCode.OK, "application/json", """{ "verification_status": "SUCCESS" }""");

        var verifier = BuildVerifier(mockHttp, webhookId, baseUrl);

        // Act
        var result = await verifier.VerifyAsync(payload, headers, "client-id", "client-secret");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WithMissingHeaders_ReturnsFalse()
    {
        var baseUrl = "https://api-m.sandbox.paypal.com";
        var payload = """{ "event_type": "PAYMENT.CAPTURE.COMPLETED" }""";
        var headers = new Dictionary<string, string>(); // empty

        var verifier = BuildVerifier(new MockHttpMessageHandler(), "wh-test", baseUrl);

        var result = await verifier.VerifyAsync(payload, headers, "cid", "secret");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_WhenPayPalReturnsFailure_ReturnsFalse()
    {
        var webhookId = "wh-test-123";
        var baseUrl   = "https://api-m.sandbox.paypal.com";
        var payload   = """{ "event_type": "PAYMENT.CAPTURE.COMPLETED" }""";
        var headers = new Dictionary<string, string>
        {
            ["PAYPAL-TRANSMISSION-ID"] = "trans-123",
            ["PAYPAL-TRANSMISSION-TIME"] = "2024-01-01T12:00:00Z",
            ["PAYPAL-TRANSMISSION-SIG"] = "sha256=abc123",
            ["PAYPAL-CERT-URL"] = "https://api.paypal.com/v1/notifications/certs/CERT-123",
            ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA"
        };

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, $"{baseUrl}/v1/notifications/verify-webhook-signature")
            .Respond(HttpStatusCode.OK, "application/json", """{ "verification_status": "FAILURE" }""");

        var verifier = BuildVerifier(mockHttp, webhookId, baseUrl);

        var result = await verifier.VerifyAsync(payload, headers, "cid", "secret");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_WithEmptyWebhookId_ReturnsFalse()
    {
        var baseUrl = "https://api-m.sandbox.paypal.com";
        var payload = """{ "event_type": "PAYMENT.CAPTURE.COMPLETED" }""";
        var headers = new Dictionary<string, string>
        {
            ["PAYPAL-TRANSMISSION-ID"] = "trans-123",
            ["PAYPAL-TRANSMISSION-TIME"] = "2024-01-01T12:00:00Z",
            ["PAYPAL-TRANSMISSION-SIG"] = "sha256=abc123",
            ["PAYPAL-CERT-URL"] = "https://api.paypal.com/v1/notifications/certs/CERT-123",
            ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA"
        };

        // Build verifier with empty webhookId
        var options = Options.Create(new PayPalOptions { WebhookId = "", BaseUrl = baseUrl });
        var httpClient = new HttpClient(new MockHttpMessageHandler()) { BaseAddress = new Uri(baseUrl) };
        var factory = new MockHttpClientFactory(httpClient);
        var logger = NullLogger<PayPalWebhookVerifier>.Instance;
        var verifier = new PayPalWebhookVerifier(factory, logger, options);

        var result = await verifier.VerifyAsync(payload, headers, "cid", "secret");

        result.Should().BeFalse();
    }

    // Helper to adapt HttpClient to IHttpClientFactory
    private sealed class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public MockHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}
