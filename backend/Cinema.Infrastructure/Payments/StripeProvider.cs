using Cinema.Application.Payments;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.Extensions.Options;
using Stripe;

namespace Cinema.Infrastructure.Payments;

public sealed class StripeProvider : IPaymentProvider
{
    public PaymentProvider ProviderType => PaymentProvider.Stripe;

    private readonly IStripeClient _stripeClient;
    private readonly IStripeWebhookVerifier _verifier;
    private readonly string _webhookSecret;

    public StripeProvider(
        IStripeClient stripeClient,
        IStripeWebhookVerifier verifier,
        IOptions<StripeOptions> options)
    {
        _stripeClient   = stripeClient;
        _verifier       = verifier;
        _webhookSecret  = options.Value.WebhookSecret;
    }

    public async Task<PaymentIntentResult> CreateIntentAsync(
        IEnumerable<Ticket> tickets, decimal amount, string returnUrl, CancellationToken ct = default)
    {
        var ticketIds = tickets.Select(t => t.Id).ToArray();
        var svc = new PaymentIntentService(_stripeClient);
        var intent = await svc.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount              = (long)(amount * 100), // копійки
            Currency            = "uah",
            PaymentMethodTypes  = ["card"],
            Metadata            = new Dictionary<string, string>
            {
                ["ticketIds"] = System.Text.Json.JsonSerializer.Serialize(ticketIds),
            },
        }, cancellationToken: ct);

        return new PaymentIntentResult(intent.Id, intent.ClientSecret, null);
    }

    /// <summary>
    /// Створює PaymentMethod із Google Pay токена, потім підтверджує PaymentIntent.
    /// </summary>
    public async Task<string> CreateAndConfirmWithTokenAsync(
        IEnumerable<Ticket> tickets, decimal amount, string googlePayToken, CancellationToken ct = default)
    {
        var ticketIds = tickets.Select(t => t.Id).ToArray();

        // 1. Конвертуємо токен → PaymentMethod
        var pmSvc = new PaymentMethodService(_stripeClient);
        var pm = await pmSvc.CreateAsync(new PaymentMethodCreateOptions
        {
            Type = "card",
            Card = new PaymentMethodCardOptions { Token = googlePayToken },
        }, cancellationToken: ct);

        // 2. Створюємо та підтверджуємо PaymentIntent
        var piSvc = new PaymentIntentService(_stripeClient);
        var intent = await piSvc.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount             = (long)(amount * 100),
            Currency           = "uah",
            PaymentMethodTypes = ["card"],
            PaymentMethod      = pm.Id,
            Confirm            = true,
            Metadata           = new Dictionary<string, string>
            {
                ["ticketIds"] = System.Text.Json.JsonSerializer.Serialize(ticketIds),
            },
        }, cancellationToken: ct);

        if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Google Pay payment was not completed.");

        return intent.Id;
    }

    public async Task EnsureSucceededAsync(string paymentIntentId, decimal expectedAmount, CancellationToken ct = default)
    {
        var svc = new PaymentIntentService(_stripeClient);
        var intent = await svc.GetAsync(paymentIntentId, cancellationToken: ct);
        var expectedMinorAmount = (long)(expectedAmount * 100);

        if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            || intent.Amount != expectedMinorAmount
            || !string.Equals(intent.Currency, "uah", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stripe payment was not completed or has an invalid amount.");
        }
    }

    public Task<WebhookResult> HandleWebhookAsync(
        string payload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        headers.TryGetValue("Stripe-Signature", out var signature);

        Event stripeEvent;
        try
        {
            stripeEvent = _verifier.ConstructEvent(payload, signature ?? string.Empty, _webhookSecret);
        }
        catch (StripeException)
        {
            return Task.FromResult(new WebhookResult(false, null, false));
        }

        if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
        {
            var intent = stripeEvent.Data.Object as PaymentIntent;
            return Task.FromResult(new WebhookResult(true, intent?.Id, true));
        }

        return Task.FromResult(new WebhookResult(true, null, false));
    }
}
