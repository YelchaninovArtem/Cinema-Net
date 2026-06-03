using Cinema.Application.Loyalty;
using Cinema.Application.PromoCodes;
using Cinema.Application.Tickets;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Queries;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly CinemaDbContext _db;
    private readonly IPromoCodeService _promoService;
    private readonly ILoyaltyService _loyaltyService;

    public TicketService(CinemaDbContext db, IPromoCodeService promoService, ILoyaltyService loyaltyService)
    {
        _db           = db;
        _promoService = promoService;
        _loyaltyService = loyaltyService;
    }

    // ── Seat map ──────────────────────────────────────────────────────────────

    public async Task<SeatMapDto?> GetSeatMapAsync(int showtimeId, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(s => s.Id == showtimeId, ct);

        if (showtime is null) return null;
        if (showtime.StartUtc.AddMinutes(1) <= DateTime.UtcNow)
            return null;

        var hall   = showtime.Hall;
        var layout = hall.GetLayout(); // SeatTypeCode[][]

        var takenSeats = await _db.Tickets
            .Where(t => t.ShowtimeId == showtimeId &&
                        (t.Status == TicketStatus.PendingPayment || t.Status == TicketStatus.Paid))
            .Select(t => new SeatCoord(t.Row, t.Col))
            .ToListAsync(ct);

        return new SeatMapDto(
            showtime.Id,
            showtime.Movie.Title,
            hall.Name,
            hall.CinemaBranch.Name,
            hall.CinemaBranch.City,
            DateTimeUtc.Mark(showtime.StartUtc),
            MovieQueryService.FormatToString(showtime.Format),
            showtime.BasePrice,
            hall.Rows,
            hall.Cols,
            layout,
            takenSeats);
    }

    // ── Create tickets (instant buy) ─────────────────────────────────────────

    public async Task<CreateTicketsResponse?> CreateTicketsAsync(
        CreateTicketsRequest req,
        string? userId,
        CancellationToken ct = default)
    {
        ValidateSeats(req.Seats);

        var showtime = await _db.Showtimes
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.Id == req.ShowtimeId, ct);

        if (showtime is null) return null;
        if (showtime.StartUtc.AddMinutes(1) <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot purchase tickets for a past showtime.");

        var hall    = showtime.Hall;
        var layout  = hall.GetLayout();

        // Validate seat coordinates
        foreach (var seat in req.Seats)
        {
            if (seat.Row < 1 || seat.Row > hall.Rows || seat.Col < 1 || seat.Col > hall.Cols)
                throw new ArgumentOutOfRangeException(nameof(req.Seats), $"Seat ({seat.Row},{seat.Col}) is out of bounds.");
        }

        // Compute per-seat price
        decimal GetCoefficient(SeatTypeCode type) => type switch
        {
            SeatTypeCode.Vip   => 1.5m,
            SeatTypeCode.Love  => 2.0m,
            _                  => 1.0m
        };

        var seatsList   = req.Seats.ToList();
        var seatPrices  = new List<decimal>();
        var seatTypes   = new List<SeatTypeCode>();
        decimal subtotal = 0;

        foreach (var s in seatsList)
        {
            var seatType = layout[s.Row - 1][s.Col - 1];
            var price    = showtime.BasePrice * GetCoefficient(seatType);
            seatPrices.Add(price);
            seatTypes.Add(seatType);
            subtotal += price;
        }

        // Promo
        decimal promoDiscount = 0;
        PromoCode? promo = null;
        if (!string.IsNullOrWhiteSpace(req.PromoCode))
        {
            promo = await _promoService.ValidateAndGetAsync(req.PromoCode.Trim().ToUpperInvariant(), userId, ct);
            promoDiscount = promo.CalculateDiscount(subtotal);
            if (promoDiscount > subtotal) promoDiscount = subtotal;
        }

        // Loyalty
        int loyaltyPointsUsed = 0;
        decimal loyaltyDiscount = 0;
        if (req.LoyaltyPointsToRedeem.HasValue && req.LoyaltyPointsToRedeem > 0)
        {
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Loyalty points require a registered user.");

            var balance = await _loyaltyService.GetBalanceAsync(userId, ct);
            var maxDiscount = Math.Floor(subtotal * 0.5m);
            var maxPoints   = (int)maxDiscount;
            var toRedeem    = Math.Min(req.LoyaltyPointsToRedeem.Value, Math.Min(balance.Balance, maxPoints));

            if (toRedeem <= 0)
                throw new InvalidOperationException("No points available to redeem.");

            loyaltyPointsUsed = toRedeem;
            loyaltyDiscount   = toRedeem;
        }

        var finalAmount = subtotal - promoDiscount - loyaltyDiscount;
        if (finalAmount < 0) finalAmount = 0;

        var strategy = _db.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                    // Скасовуємо pending квитки цього користувача (re-create при зміні promo/loyalty)
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var pendingOwn = await _db.Tickets
                            .Where(t => t.ShowtimeId == req.ShowtimeId &&
                                        t.UserId == userId &&
                                        t.Status == TicketStatus.PendingPayment)
                            .ToListAsync(ct);

                        if (pendingOwn.Count > 0)
                        {
                            var pendingOwnIds = pendingOwn.Select(t => t.Id).ToList();
                            var redeemTxs = await _db.LoyaltyTransactions
                                .Where(tx => tx.UserId == userId &&
                                             tx.TicketId.HasValue &&
                                             pendingOwnIds.Contains(tx.TicketId.Value) &&
                                             tx.Delta < 0)
                                .ToListAsync(ct);

                            if (redeemTxs.Count > 0)
                            {
                                var account = await _db.LoyaltyAccounts.FindAsync([userId], ct);
                                if (account is not null)
                                    foreach (var tx in redeemTxs)
                                        account.Restore(-tx.Delta);

                                _db.LoyaltyTransactions.RemoveRange(redeemTxs);
                            }
                        }

                        foreach (var t in pendingOwn) t.Cancel();
                        if (pendingOwn.Count > 0)
                            await _db.SaveChangesAsync(ct);
                    }

                    // Перевірка зайнятості після скасування своїх
                    var rows = req.Seats.Select(s => s.Row).ToList();
                    var cols = req.Seats.Select(s => s.Col).ToList();
                    var existingSeats = await _db.Tickets
                        .Where(t => t.ShowtimeId == req.ShowtimeId &&
                                    rows.Contains(t.Row) &&
                                    cols.Contains(t.Col) &&
                                    t.Status != TicketStatus.Cancelled &&
                                    t.Status != TicketStatus.Refunded)
                        .Select(t => new { t.Row, t.Col })
                        .ToListAsync(ct);
                    if (existingSeats.Any(e => req.Seats.Any(s => s.Row == e.Row && s.Col == e.Col)))
                    {
                        await transaction.RollbackAsync(ct);
                        return null;
                    }

                    var payment = new Payment(finalAmount);
                    _db.Payments.Add(payment);

                    var tickets = new List<Ticket>();
                    decimal allocatedFinalAmount = 0;
                    decimal allocatedPromoDiscount = 0;
                    decimal allocatedLoyaltyDiscount = 0;
                    for (int i = 0; i < seatsList.Count; i++)
                    {
                        var row   = seatsList[i].Row;
                        var col   = seatsList[i].Col;
                        var type  = seatTypes[i];
                        var price = seatPrices[i];
                        var qr    = Guid.NewGuid().ToString("N");

                        var ticket = new Ticket(req.ShowtimeId, row, col, type, price, qr);
                        ticket.SetPurchaser(userId, req.GuestEmail);

                        var share       = price / subtotal;
                        var isLast      = i == seatsList.Count - 1;
                        var ticketFinal = isLast
                            ? finalAmount - allocatedFinalAmount
                            : Math.Round(finalAmount * share, 2);
                        allocatedFinalAmount += ticketFinal;
                        ticket.SetFinalAmount(ticketFinal);

                        if (promo != null && promoDiscount > 0)
                        {
                            var ticketPromoShare = isLast
                                ? promoDiscount - allocatedPromoDiscount
                                : Math.Round(promoDiscount * share, 2);
                            allocatedPromoDiscount += ticketPromoShare;
                            ticket.ApplyPromo(promo, ticketPromoShare);
                        }

                        if (loyaltyPointsUsed > 0)
                        {
                            var ticketLoyaltyShare = isLast
                                ? loyaltyDiscount - allocatedLoyaltyDiscount
                                : Math.Round(loyaltyDiscount * share, 2);
                            allocatedLoyaltyDiscount += ticketLoyaltyShare;
                            var pts = (int)Math.Round(loyaltyPointsUsed * share);
                            if (pts > 0)
                                ticket.ApplyLoyalty(pts, ticketLoyaltyShare);
                        }

                        tickets.Add(ticket);
                    }

                    _db.Tickets.AddRange(tickets);
                    await _db.SaveChangesAsync(ct);

                    foreach (var t in tickets)
                        _db.PaymentTickets.Add(new PaymentTicket(payment.Id, t.Id));

                    if (promo != null)
                        await _promoService.IncrementUsageAsync(promo.Id, ct);

                    if (loyaltyPointsUsed > 0 && !string.IsNullOrEmpty(userId))
                    {
                        var account = await _db.LoyaltyAccounts.FirstAsync(a => a.UserId == userId, ct);
                        account.Redeem(loyaltyPointsUsed);
                        _db.LoyaltyTransactions.Add(new LoyaltyTransaction(userId, tickets.First().Id, -loyaltyPointsUsed, "Redeemed for purchase"));
                    }

                    await _db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    var ticketDtos = tickets.Select(t => new TicketDto(
                        t.Id,
                        new SeatInfo(t.Row, t.Col, t.SeatType, t.Price),
                        t.QrToken,
                        t.FinalAmount)).ToList();

                    return new CreateTicketsResponse(payment.Id, finalAmount, ticketDtos);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
                  (sql.Number == 2601 || sql.Number == 2627))
        {
            return null;
        }
    }

    private static void ValidateSeats(IReadOnlyList<SeatCoord>? seats)
    {
        if (seats is null || seats.Count == 0)
            throw new DomainException("Select at least one seat.");
        if (seats.Count > 10)
            throw new DomainException("A maximum of 10 seats can be purchased at once.");
        if (seats.Distinct().Count() != seats.Count)
            throw new DomainException("Duplicate seats are not allowed.");
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<TicketDetailDto?> GetTicketDetailAsync(int ticketId, CancellationToken ct = default)
    {
        var t = await _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (t is null) return null;

        var seatInfo = new SeatInfo(t.Row, t.Col, t.SeatType, t.Price);
        var qrUrl    = $"/api/tickets/{t.Id}/qr";

        return new TicketDetailDto(
            t.Id,
            t.ShowtimeId,
            t.Showtime.MovieId,
            t.Showtime.Movie.Title,
            DateTimeUtc.Mark(t.Showtime.StartUtc),
            t.Showtime.Hall.Name,
            MovieQueryService.FormatToString(t.Showtime.Format),
            seatInfo,
            t.Status.ToString(),
            t.FinalAmount,
            t.GuestEmail,
            qrUrl);
    }

    public async Task<IReadOnlyCollection<TicketSummaryDto>> GetUserTicketsAsync(string userId, CancellationToken ct = default)
    {
        var tickets = await _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .Where(t => t.UserId == userId
                        && (t.Status == TicketStatus.Paid
                            || t.Status == TicketStatus.Used
                            || t.Status == TicketStatus.NotUsed))
            .OrderByDescending(t => t.CreatedUtc)
            .ToListAsync(ct);

        var result = tickets.Select(t => new TicketSummaryDto(
            t.Id,
            t.Showtime.MovieId,
            t.Showtime.Movie.Title,
            DateTimeUtc.Mark(t.Showtime.StartUtc),
            t.Showtime.Hall.CinemaBranch.Name,
            MovieQueryService.FormatToString(t.Showtime.Format),
            t.Row,
            t.Col,
            t.Status,
            t.FinalAmount,
            DateTimeUtc.Mark(t.CreatedUtc))).ToList();

        return result.AsReadOnly();
    }
}
