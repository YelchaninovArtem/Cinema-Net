namespace Cinema.Infrastructure.Payments;

public sealed class StripeOptions
{
    public string SecretKey     { get; set; } = "";
    public string PublicKey     { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}
