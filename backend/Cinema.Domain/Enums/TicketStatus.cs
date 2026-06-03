namespace Cinema.Domain.Enums;

public enum TicketStatus
{
    PendingPayment = 0,
    Paid           = 1,
    Cancelled      = 2,
    Used           = 3,
    Refunded       = 4,
    NotUsed        = 5,
}
