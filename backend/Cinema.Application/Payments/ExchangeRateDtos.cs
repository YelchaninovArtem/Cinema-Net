using System.Text.Json.Serialization;

namespace Cinema.Application.Payments;

/// <summary>
/// Request for getting an exchange rate (optional filters).
/// </summary>
public sealed record GetExchangeRateRequest(
    string BaseCurrency = "UAH",
    string TargetCurrency = "USD");

/// <summary>
/// Response containing exchange rate information.
/// </summary>
public sealed record ExchangeRateDto(
    string Base,
    string Target,
    decimal Rate,
    [property: JsonPropertyName("fetched_at")] DateTime FetchedAt);
