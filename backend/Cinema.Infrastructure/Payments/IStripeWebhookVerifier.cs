using Stripe;

namespace Cinema.Infrastructure.Payments;

/// <summary>Абстракція для верифікації підпису Stripe webhook, яку можна замінити у тестах.</summary>
public interface IStripeWebhookVerifier
{
    Event ConstructEvent(string payload, string signature, string secret);
}

public sealed class StripeWebhookVerifier : IStripeWebhookVerifier
{
    public Event ConstructEvent(string payload, string signature, string secret)
        => EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false);
}

/// <summary>Версія без перевірки підпису — лише для інтеграційних тестів.</summary>
public sealed class NoVerificationStripeWebhookVerifier : IStripeWebhookVerifier
{
    public Event ConstructEvent(string payload, string signature, string secret)
        => EventUtility.ParseEvent(payload, throwOnApiVersionMismatch: false);
}
