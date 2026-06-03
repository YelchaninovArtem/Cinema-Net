using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Payments;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Infrastructure.Payments;

/// <summary>
/// Fetches exchange rates from external API with in-memory caching.
/// Currently supports free exchangerate-api.com (no API key required).
/// </summary>
public sealed class ExchangeRateService : IExchangeRateService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly PayPalOptions _paypalOptions;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public ExchangeRateService(
        IHttpClientFactory clientFactory,
        IMemoryCache cache,
        ILogger<ExchangeRateService> logger,
        IOptions<PayPalOptions> paypalOptions)
    {
        _clientFactory = clientFactory;
        _cache = cache;
        _logger = logger;
        _paypalOptions = paypalOptions.Value;
    }

    public async Task<ExchangeRateDto> GetRateAsync(string baseCurrency, string targetCurrency, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(targetCurrency))
            throw new ArgumentException("Currency codes must be provided", nameof(baseCurrency));

        var cacheKey = $"exch_{baseCurrency.ToUpperInvariant()}_{targetCurrency.ToUpperInvariant()}";
        if (_cache.TryGetValue<ExchangeRateDto>(cacheKey, out var cached))
            return cached!;

        // Fetch from free API: https://api.exchangerate-api.com/v4/latest/UAH
        var client = _clientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.exchangerate-api.com/");

        try
        {
            var response = await client.GetFromJsonAsync<ExchangeRateApiResponse>($"/v4/latest/{baseCurrency}", ct);
            if (response?.Rates != null && response.Rates.TryGetValue(targetCurrency, out var rate) && rate > 0)
            {
                var result = new ExchangeRateDto(baseCurrency.ToUpperInvariant(), targetCurrency.ToUpperInvariant(), rate, DateTime.UtcNow);
                _cache.Set(cacheKey, result, CacheDuration);
                _logger.LogInformation("Fetched exchange rate {Base}→{Target}: {Rate}", baseCurrency, targetCurrency, rate);
                return result;
            }

            _logger.LogWarning("Exchange rate for {Base}→{Target} not available in API response", baseCurrency, targetCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rate from external API for {Base}→{Target}", baseCurrency, targetCurrency);
        }

        // Fallback: only for UAH→USD
        if (baseCurrency.Equals("UAH", StringComparison.OrdinalIgnoreCase) &&
            targetCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackRate = 1m / _paypalOptions.FallbackUsdToUahRate;
            var fallback = new ExchangeRateDto("UAH", "USD", fallbackRate, DateTime.UtcNow);
            _cache.Set(cacheKey, fallback, TimeSpan.FromHours(1)); // short cache for fallback
            _logger.LogWarning("Using fallback exchange rate UAH→USD: {Rate}", fallbackRate);
            return fallback;
        }

        throw new InvalidOperationException($"Unable to obtain exchange rate for {baseCurrency}→{targetCurrency}");
    }

    // Helper class matching API response
    private class ExchangeRateApiResponse
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = default!;

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = default!;

        [JsonPropertyName("date")]
        public string Date { get; set; } = default!;
    }
}
