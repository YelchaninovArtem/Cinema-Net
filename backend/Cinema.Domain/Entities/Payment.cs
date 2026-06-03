using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class Payment
{
    private Payment() { } // EF Core

    /// <summary>
    /// Creates a new pending payment.
    /// Provider and ExternalId are set when the payment intent is created with the provider.
    /// </summary>
    public Payment(decimal amount)
    {
        Amount     = amount;
        Status     = PaymentStatus.Pending;
        CreatedUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    private readonly List<PaymentTicket> _ticketLinks = new();
    public IReadOnlyCollection<PaymentTicket> TicketLinks => _ticketLinks.AsReadOnly();

    public PaymentProvider Provider { get; private set; }
    public string ExternalId { get; private set; } = default!;
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? OriginalAmount { get; private set; }
    public int? LoyaltyPointsEarned { get; private set; }
    public DateTime? PaidUtc { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public void SetProviderAndExternalId(PaymentProvider provider, string externalId)
    {
        Provider   = provider;
        ExternalId = externalId;
    }

    public void MarkCompleted()
    {
        Status  = PaymentStatus.Completed;
        PaidUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = PaymentStatus.Failed;
    }
}
