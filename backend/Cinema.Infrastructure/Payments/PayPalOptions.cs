namespace Cinema.Infrastructure.Payments;

public sealed class PayPalOptions
{
    public string ClientId     { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    /// <summary>https://api-m.sandbox.paypal.com для sandbox, https://api-m.paypal.com для prod.</summary>
    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    /// <summary>PayPal Webhook ID from developer dashboard (used for signature verification).</summary>
    public string WebhookId { get; set; } = "";

    /// <summary>Fallback USD→UAH exchange rate used if live fetch fails.</summary>
    public decimal FallbackUsdToUahRate { get; set; } = 44.5m;
}
