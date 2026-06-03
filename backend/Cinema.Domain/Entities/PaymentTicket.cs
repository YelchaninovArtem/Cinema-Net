using Cinema.Domain.Entities;

namespace Cinema.Domain.Entities;

public sealed class PaymentTicket
{
    public PaymentTicket() { } // EF Core

    public PaymentTicket(int paymentId, int ticketId)
    {
        PaymentId = paymentId;
        TicketId  = ticketId;
    }

    public int PaymentId { get; private set; }
    public Payment Payment { get; private set; } = default!;

    public int TicketId { get; private set; }
    public Ticket Ticket { get; private set; } = default!;
}
