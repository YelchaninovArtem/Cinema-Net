using Cinema.Infrastructure.Payments;

namespace Cinema.Tests.Integration;

/// <summary>
/// Test double that always verifies successfully, used for integration tests.
/// </summary>
public sealed class NoVerificationPayPalWebhookVerifier : IPayPalWebhookVerifier
{
    public Task<bool> VerifyAsync(string payload, IReadOnlyDictionary<string, string> headers, string clientId, string secret, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
