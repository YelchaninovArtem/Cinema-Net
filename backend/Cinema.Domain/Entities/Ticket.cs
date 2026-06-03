using Cinema.Domain.Common;
using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class Ticket
{
    public Ticket()
    {
        _paymentLinks = new List<PaymentTicket>();
    }

    public Ticket(int showtimeId, int row, int col, SeatTypeCode seatType, decimal price, string qrToken)
    {
        ShowtimeId = showtimeId;
        Row        = row;
        Col        = col;
        SeatType   = seatType;
        Price      = price;
        QrToken    = qrToken;
        Status     = TicketStatus.PendingPayment;
        CreatedUtc = DateTime.UtcNow;
        _paymentLinks = new List<PaymentTicket>();
    }

    public int Id { get; private set; }

    // Who bought
    public string? UserId { get; private set; }
    public string? GuestEmail { get; private set; }

    // What/when
    public int ShowtimeId { get; private set; }
    public Showtime Showtime { get; private set; } = default!;

    // Seat
    public int Row { get; private set; }
    public int Col { get; private set; }
    public SeatTypeCode SeatType { get; private set; }
    public decimal Price { get; private set; }

    // Navigation: many-to-many via PaymentTicket
    private readonly List<PaymentTicket> _paymentLinks;
    public IReadOnlyCollection<PaymentTicket> PaymentLinks => _paymentLinks.AsReadOnly();

    // Promo code applied (nullable)
    public int? PromoCodeId { get; private set; }
    public PromoCode? PromoCode { get; private set; }

    // Status & time
    public TicketStatus Status { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? PaidUtc { get; private set; }

    // QR code
    public string QrToken { get; private set; } = string.Empty;

    // Discounts (denormalized snapshot at purchase time)
    public decimal PromoDiscount { get; private set; }
    public int LoyaltyPointsRedeemed { get; private set; }
    public decimal LoyaltyDiscount { get; private set; }

    // Total after discounts
    public decimal FinalAmount { get; private set; }

    // Reminder tracking
    public DateTime? ReminderSentUtc { get; private set; }

    // ── Domain methods ──────────────────────────────────────────────────────────

    public void SetPurchaser(string? userId, string? guestEmail)
    {
        UserId = userId;
        GuestEmail = guestEmail;
    }

    public void MarkPaid()
    {
        if (Status != TicketStatus.PendingPayment)
            throw new InvalidOperationException("Only pending tickets can be marked paid.");
        Status = TicketStatus.Paid;
        PaidUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == TicketStatus.Cancelled) return;
        Status = TicketStatus.Cancelled;
    }

    public void MarkUsed()
    {
        if (Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be marked as used.");
        Status = TicketStatus.Used;
    }

    public void MarkNotUsed()
    {
        if (Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be marked as not used.");
        Status = TicketStatus.NotUsed;
    }

    public void MarkRefunded()
    {
        if (Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be refunded.");
        Status = TicketStatus.Refunded;
    }

    public void MarkReminderSent()
    {
        ReminderSentUtc = DateTime.UtcNow;
    }

    public void ApplyPromo(PromoCode promo, decimal discountAmount)
    {
        if (discountAmount < 0) throw new ArgumentException("Discount cannot be negative.");
        PromoDiscount = discountAmount;
        PromoCodeId = promo.Id;
    }

    public void ApplyPromo(decimal discountAmount)
    {
        if (discountAmount < 0) throw new ArgumentException("Discount cannot be negative.");
        PromoDiscount = discountAmount;
    }

    public void ApplyLoyalty(int pointsRedeemed, decimal discountAmount)
    {
        if (pointsRedeemed <= 0) throw new ArgumentException("Points must be positive.");
        if (discountAmount < 0) throw new ArgumentException("Discount cannot be negative.");
        LoyaltyPointsRedeemed = pointsRedeemed;
        LoyaltyDiscount = discountAmount;
    }

    public void ClearLoyalty()
    {
        LoyaltyPointsRedeemed = 0;
        LoyaltyDiscount = 0;
    }

    public void SetFinalAmount(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Final amount cannot be negative.");
        FinalAmount = amount;
    }
}
