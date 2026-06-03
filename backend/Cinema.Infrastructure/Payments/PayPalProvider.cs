using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Cinema.Application.Payments;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Cinema.Infrastructure.Payments;

public sealed class PayPalProvider : IPaymentProvider
{
    public PaymentProvider ProviderType => PaymentProvider.PayPal;

    private readonly HttpClient _http;
    private readonly PayPalOptions _options;
    private readonly IPayPalWebhookVerifier _verifier;
    private readonly IExchangeRateService _exchangeRate;

    public PayPalProvider(HttpClient http, IOptions<PayPalOptions> options, IPayPalWebhookVerifier verifier, IExchangeRateService exchangeRate)
    {
        _http        = http;
        _options     = options.Value;
        _verifier    = verifier;
        _exchangeRate = exchangeRate;
    }

    public async Task<PaymentIntentResult> CreateIntentAsync(
        IEnumerable<Ticket> tickets, decimal amount, string returnUrl, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);

        // PayPal does not support UAH — convert to USD using live exchange rate
        var rateDto = await _exchangeRate.GetRateAsync("UAH", "USD", ct);
        var usdAmount = Math.Round(amount * rateDto.Rate, 2);

        var ticketIds = tickets.Select(t => t.Id).ToArray();
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount    = new { currency_code = "USD", value = usdAmount.ToString("F2", CultureInfo.InvariantCulture) },
                    custom_id = JsonSerializer.Serialize(ticketIds), // store ticket IDs as JSON
                }
            },
            application_context = new
            {
                return_url  = returnUrl,
                cancel_url  = returnUrl + "?cancelled=true",
                brand_name  = "Cinema Network",
                user_action = "PAY_NOW",
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v2/checkout/orders");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(body);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var error = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"PayPal create order failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {error}");
        }

        var doc       = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var orderId   = doc.GetProperty("id").GetString()!;
        var approvalUrl = doc.GetProperty("links").EnumerateArray()
            .First(l => l.GetProperty("rel").GetString() == "approve")
            .GetProperty("href").GetString()!;

        return new PaymentIntentResult(orderId, null, approvalUrl);
    }

    public async Task<WebhookResult> HandleWebhookAsync(
        string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        // Verify webhook signature before processing
        var verified = await _verifier.VerifyAsync(payload, headers, _options.ClientId, _options.ClientSecret, ct);
        if (!verified)
        {
            return new WebhookResult(false, null, false);
        }

        JsonDocument doc;
        try { doc = JsonDocument.Parse(payload); }
        catch { return new WebhookResult(false, null, false); }

        if (!doc.RootElement.TryGetProperty("event_type", out var eventType))
            return new WebhookResult(false, null, false);

        // PAYMENT.CAPTURE.COMPLETED
        if (eventType.GetString() == "PAYMENT.CAPTURE.COMPLETED")
        {
            var orderId = doc.RootElement
                .GetProperty("resource")
                .GetProperty("supplementary_data")
                .GetProperty("related_ids")
                .GetProperty("order_id")
                .GetString();

            return new WebhookResult(true, orderId, true);
        }

        return new WebhookResult(true, null, false);
    }

    /// <summary>Захоплення PayPal-ордеру після повернення користувача зі сторінки PayPal.</summary>
    public async Task<bool> CaptureAsync(string orderId, CancellationToken ct = default)
    {
        var token = await GetAccessTokenAsync(ct);

        var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v2/checkout/orders/{orderId}/capture");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await _http.SendAsync(req, ct);

        if (!resp.IsSuccessStatusCode) return false;

        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("status").GetString() == "COMPLETED";
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v1/oauth2/token");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        req.Content = new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("grant_type", "client_credentials")]);

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var doc = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return doc.GetProperty("access_token").GetString()!;
    }

}
