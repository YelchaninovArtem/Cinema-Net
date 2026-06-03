namespace Cinema.Domain.Entities;

public sealed class LoyaltyTransaction
{
    private LoyaltyTransaction() { }

    /// <summary>
    /// Creates a new loyalty transaction.
    /// Use TicketId for purchase-related transactions; leave null for other events.
    /// </summary>
    public LoyaltyTransaction(string userId, int? ticketId, int delta, string reason)
    {
        UserId    = userId;
        TicketId  = ticketId;
        Delta     = delta;
        Reason    = reason;
        CreatedUtc = DateTime.UtcNow;
    }

    public int     Id         { get; private set; }
    public string  UserId     { get; private set; } = default!;
    public int?    TicketId  { get; private set; }
    public Ticket? Ticket { get; private set; }
    public int     Delta      { get; private set; }  // >0 нарахування, <0 списання
    public string  Reason     { get; private set; } = default!;
    public DateTime CreatedUtc { get; private set; }
}
