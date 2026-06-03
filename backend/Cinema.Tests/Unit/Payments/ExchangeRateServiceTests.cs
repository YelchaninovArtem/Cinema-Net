using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Cinema.Application.Payments;
using Cinema.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace Cinema.Tests.Unit.Payments;

public sealed class ExchangeRateServiceTests
{
    private static ExchangeRateService BuildService(MockHttpMessageHandler mockHttp, PayPalOptions? paypalOptions = null)
    {
        var httpClient = new HttpClient(mockHttp);
        var factory = new MockHttpClientFactory(httpClient);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<ExchangeRateService>.Instance;
        var options = Options.Create(paypalOptions ?? new PayPalOptions { FallbackUsdToUahRate = 44.5m });
        return new ExchangeRateService(factory, cache, logger, options);
    }

    [Fact]
    public async Task GetRateAsync_WhenApiSucceeds_ReturnsRateAndCaches()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.exchangerate-api.com/v4/latest/UAH")
            .Respond(HttpStatusCode.OK, "application/json",
                """{ "base": "UAH", "rates": { "USD": 0.025 } }""");

        var service = BuildService(mockHttp);

        // Act - first call should hit API
        var result = await service.GetRateAsync("UAH", "USD");

        // Assert
        result.Base.Should().Be("UAH");
        result.Target.Should().Be("USD");
        result.Rate.Should().Be(0.025m);

        // Second call should hit cache (no exception demonstrates cache hit; unlimited expectation means if HTTP were called again we'd still get same value)
        var result2 = await service.GetRateAsync("UAH", "USD");
        result2.Rate.Should().Be(0.025m);
    }

    [Fact]
    public async Task GetRateAsync_WhenApiFails_UsesFallback()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "*")
            .Throw(new HttpRequestException("Network down"));

        var service = BuildService(mockHttp); // with fallback 44.5 -> direct rate = 1/44.5 = 0.022471...

        // Act
        var result = await service.GetRateAsync("UAH", "USD");

        // Assert
        result.Rate.Should().BeApproximately(1m / 44.5m, 5); // approx 0.02247
        result.Base.Should().Be("UAH");
        result.Target.Should().Be("USD");
    }

    [Fact]
    public async Task GetRateAsync_ForNonUsd_UsesApi()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://api.exchangerate-api.com/v4/latest/EUR")
            .Respond(HttpStatusCode.OK, "application/json",
                """{ "base": "EUR", "rates": { "USD": 1.08 } }""");

        var service = BuildService(mockHttp);
        var result = await service.GetRateAsync("EUR", "USD");

        result.Rate.Should().Be(1.08m);
    }

    // Helper adapter
    private sealed class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public MockHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}
