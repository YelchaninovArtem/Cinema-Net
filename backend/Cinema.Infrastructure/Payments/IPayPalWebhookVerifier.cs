using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cinema.Infrastructure.Payments;

/// <summary>
/// Verifies the authenticity of a PayPal webhook event.
/// </summary>
public interface IPayPalWebhookVerifier
{
    /// <summary>
    /// Verifies that the webhook payload and headers originate from PayPal.
    /// </summary>
    /// <param name="payload">Raw request body (JSON string).</param>
    /// <param name="headers">HTTP headers from the webhook request.</param>
    /// <param name="clientId">PayPal client ID.</param>
    /// <param name="secret">PayPal client secret.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if verification succeeds; otherwise false.</returns>
    Task<bool> VerifyAsync(string payload, IReadOnlyDictionary<string, string> headers, string clientId, string secret, CancellationToken ct = default);
}
