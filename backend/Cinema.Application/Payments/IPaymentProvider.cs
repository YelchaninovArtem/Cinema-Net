using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.Payments;

public interface IPaymentProvider
{
    PaymentProvider ProviderType { get; }

    /// <summary>
    /// Creates a payment intent for the given tickets.
    /// </summary>
    /// <param name="tickets">The tickets to be paid for.</param>
    /// <param name="returnUrl">Where to return after provider approval.</param>
    Task<PaymentIntentResult> CreateIntentAsync(
        IEnumerable<Ticket> tickets, decimal amount, string returnUrl, CancellationToken ct = default);

    /// <param name="payload">Сирий текст тіла запиту webhook.</param>
    /// <param name="headers">HTTP-заголовки запиту для верифікації підпису.</param>
    Task<WebhookResult> HandleWebhookAsync(
        string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
}

public sealed record PaymentIntentResult(
    string  ExternalId,
    string? ClientSecret,
    string? ApprovalUrl);

public sealed record WebhookResult(bool IsValid, string? ExternalId, bool IsCompleted);
