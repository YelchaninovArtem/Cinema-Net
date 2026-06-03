using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Infrastructure.Payments;

/// <summary>
/// Verifies PayPal webhook signatures by calling PayPal's verify-webhook-signature endpoint.
/// </summary>
public sealed class PayPalWebhookVerifier(
    IHttpClientFactory httpClientFactory,
    ILogger<PayPalWebhookVerifier> logger,
    IOptions<PayPalOptions> options) : IPayPalWebhookVerifier
{
    private readonly PayPalOptions _options = options.Value;

    public async Task<bool> VerifyAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        string clientId,
        string secret,
        CancellationToken ct = default)
    {
        // Extract required PayPal headers
        if (!headers.TryGetValue("PAYPAL-TRANSMISSION-ID", out var transmissionId) ||
            !headers.TryGetValue("PAYPAL-TRANSMISSION-TIME", out var transmissionTime) ||
            !headers.TryGetValue("PAYPAL-TRANSMISSION-SIG", out var transmissionSig) ||
            !headers.TryGetValue("PAYPAL-CERT-URL", out var certUrl) ||
            !headers.TryGetValue("PAYPAL-AUTH-ALGO", out var authAlgo))
        {
            logger.LogWarning("PayPal webhook verification failed: missing required headers");
            return false;
        }

        var webhookId = _options.WebhookId;
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            logger.LogError("PayPal webhook verification failed: WebhookId not configured (PayPal:WebhookId)");
            return false;
        }

        // Build verification request body
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var verificationRequest = new
        {
            auth_algo = authAlgo,
            cert_url = certUrl,
            transmission_id = transmissionId,
            transmission_sig = transmissionSig,
            transmission_time = transmissionTime,
            webhook_id = webhookId,
            webhook_event = root
        };

        var json = JsonSerializer.Serialize(verificationRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var verifyUrl = $"{_options.BaseUrl.TrimEnd('/')}/v1/notifications/verify-webhook-signature";

        using var http = httpClientFactory.CreateClient();
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        try
        {
            using var response = await http.PostAsync(verifyUrl, content, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PayPal verification endpoint returned {Status}: {Content}", response.StatusCode, responseContent);
                return false;
            }

            using var responseDoc = JsonDocument.Parse(responseContent);
            if (responseDoc.RootElement.TryGetProperty("verification_status", out var statusProp) &&
                statusProp.GetString() == "SUCCESS")
            {
                return true;
            }

            logger.LogWarning("PayPal webhook verification failed: verification_status={Status}", statusProp.GetString());
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PayPal webhook verification call failed");
            return false;
        }
    }
}
