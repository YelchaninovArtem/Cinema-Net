using Cinema.Domain.Enums;

namespace Cinema.Application.Payments;

public interface IPaymentService
{
    /// <summary>
    /// Creates a payment intent for an existing pending payment (which references tickets).
    /// </summary>
    Task<CreateIntentResponse> CreateIntentAsync(
        int paymentId, string provider, string returnUrl, CancellationToken ct = default);

    Task HandleWebhookAsync(
        string provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default);

    /// <summary>Захоплення (capture) PayPal-ордеру після повернення зі сторінки PayPal.</summary>
    Task<bool> CapturePayPalAsync(string orderId, CancellationToken ct = default);

    /// <summary>Створює і підтверджує Stripe PaymentIntent з Google Pay токеном (server-side).</summary>
    Task ConfirmWithGooglePayAsync(int paymentId, string googlePayToken, CancellationToken ct = default);

    /// <summary>Фіналізує платіж після успішного підтвердження Stripe на клієнті (без webhook).</summary>
    Task ConfirmStripeClientAsync(int paymentId, string paymentIntentId, CancellationToken ct = default);
}

public sealed record CreateIntentResponse(
    string? ClientSecret,
    string? ApprovalUrl,
    string  ExternalId);
