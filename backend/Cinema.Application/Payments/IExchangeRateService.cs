using System.Threading;
using System.Threading.Tasks;
using Cinema.Application.Payments;

namespace Cinema.Application.Payments;

/// <summary>
/// Provides exchange rate information between currencies.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate from baseCurrency to targetCurrency.
    /// </summary>
    /// <remarks>
    /// Returns how many units of targetCurrency equal one unit of baseCurrency.
    /// For example, UAH→USD typically returns ~0.025 (1 UAH = 0.025 USD).
    /// </remarks>
    Task<ExchangeRateDto> GetRateAsync(string baseCurrency, string targetCurrency, CancellationToken ct = default);
}
