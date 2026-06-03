using Cinema.Application.Cashier;
using Cinema.Application.Email;
using Cinema.Application.QrCode;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Email;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Cashier;

public sealed class CashierService : ICashierService
{
    private readonly CinemaDbContext _db;
    private readonly IEmailSender _email;
    private readonly IQrCodeGenerator _qr;
    private readonly ILogger<CashierService> _logger;

    public CashierService(
        CinemaDbContext db,
        IEmailSender email,
        IQrCodeGenerator qr,
        ILogger<CashierService> logger)
    {
        _db     = db;
        _email  = email;
        _qr     = qr;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static VerifyTicketResult MapToResult(Ticket t) => new(
        t.Id,
        t.Showtime.Movie.Title,
        t.Showtime.Hall.Name,
        t.Showtime.Hall.CinemaBranch.Name,
        DateTimeUtc.Mark(t.Showtime.StartUtc),
        MovieQueryService.FormatToString(t.Showtime.Format),
        t.Row,
        t.Col,
        t.SeatType,
        t.Status.ToString(),
        t.GuestEmail,
        t.FinalAmount);

    private IQueryable<Ticket> TicketsWithNavigations() =>
        _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch);

    // ── Verify ────────────────────────────────────────────────────────────────

    public async Task<VerifyTicketResult?> VerifyByQrAsync(string qrToken, CancellationToken ct = default)
    {
        var ticket = await TicketsWithNavigations()
            .FirstOrDefaultAsync(t => t.QrToken == qrToken, ct);

        return ticket is null ? null : MapToResult(ticket);
    }

    public async Task<VerifyTicketResult?> VerifyByIdAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await TicketsWithNavigations()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        return ticket is null ? null : MapToResult(ticket);
    }

    // ── Use ───────────────────────────────────────────────────────────────────

    public async Task<VerifyTicketResult> UseTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await TicketsWithNavigations()
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var nowUtc = DateTime.UtcNow;
        var useStartUtc = ticket.Showtime.StartUtc.AddMinutes(-20);
        if (nowUtc < useStartUtc)
            throw new InvalidOperationException("Ticket can only be marked as used within 20 minutes before the showtime starts.");

        var useDeadlineUtc = ticket.Showtime.StartUtc.AddMinutes(ticket.Showtime.Movie.DurationMinutes + 1);
        if (nowUtc > useDeadlineUtc)
        {
            ticket.MarkNotUsed();
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Ticket can no longer be marked as used after the showtime check-in window has closed.");
        }

        // Кидає InvalidOperationException якщо квиток не в статусі Paid
        ticket.MarkUsed();
        await _db.SaveChangesAsync(ct);

        return MapToResult(ticket);
    }

    // ── Offline sale ──────────────────────────────────────────────────────────

    public async Task<OfflineSaleResult?> CreateOfflineSaleAsync(
        OfflineSaleRequest request,
        CancellationToken ct = default)
    {
        ValidateSeats(request.Seats);

        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId, ct);

        if (showtime is null)
            throw new KeyNotFoundException($"Showtime {request.ShowtimeId} not found.");
        if (showtime.StartUtc.AddMinutes(1) <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot sell tickets for a past showtime.");

        var hall   = showtime.Hall;
        var layout = hall.GetLayout();
        foreach (var seat in request.Seats)
        {
            if (seat.Row < 1 || seat.Row > hall.Rows || seat.Col < 1 || seat.Col > hall.Cols)
                throw new DomainException($"Seat ({seat.Row},{seat.Col}) is out of bounds.");
        }
        var guestEmail = string.IsNullOrWhiteSpace(request.GuestEmail)
            ? null
            : request.GuestEmail.Trim();
        var purchaserUserId = guestEmail is null
            ? null
            : await _db.Users
                .Where(u => u.NormalizedEmail == guestEmail.ToUpperInvariant())
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct);

        var strategy = _db.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                try
                {
                // Перевірка зайнятості місць (виключаємо скасовані та повернені)
                var requestedRows = request.Seats.Select(s => s.Row).ToList();
                var requestedCols = request.Seats.Select(s => s.Col).ToList();

                var takenSeats = await _db.Tickets
                    .Where(t => t.ShowtimeId == request.ShowtimeId
                                && requestedRows.Contains(t.Row)
                                && requestedCols.Contains(t.Col)
                                && t.Status != TicketStatus.Cancelled
                                && t.Status != TicketStatus.Refunded)
                    .Select(t => new { t.Row, t.Col })
                    .ToListAsync(ct);

                var conflict = takenSeats.Any(taken =>
                    request.Seats.Any(s => s.Row == taken.Row && s.Col == taken.Col));

                if (conflict)
                {
                    await tx.RollbackAsync(ct);
                    return null;
                }

                // Підрахунок ціни для кожного місця
                decimal GetCoefficient(SeatTypeCode type) => type switch
                {
                    SeatTypeCode.Vip  => 1.5m,
                    SeatTypeCode.Love => 2.0m,
                    _                 => 1.0m
                };

                var tickets    = new List<Ticket>();
                decimal total  = 0;

                foreach (var seat in request.Seats)
                {
                    var seatType = layout[seat.Row - 1][seat.Col - 1];
                    var price    = showtime.BasePrice * GetCoefficient(seatType);
                    var qr       = Guid.NewGuid().ToString("N");

                    var ticket = new Ticket(request.ShowtimeId, seat.Row, seat.Col, seatType, price, qr);
                    ticket.SetPurchaser(purchaserUserId, guestEmail);
                    ticket.SetFinalAmount(price);
                    ticket.MarkPaid();

                    tickets.Add(ticket);
                    total += price;
                }

                _db.Tickets.AddRange(tickets);

                // Створюємо платіж готівкою
                var payment = new Payment(total);
                _db.Payments.Add(payment);

                await _db.SaveChangesAsync(ct);

                // Зв'язуємо платіж з квитками та позначаємо провайдера
                payment.SetProviderAndExternalId(PaymentProvider.Cash, $"CASH-{payment.Id}");
                payment.MarkCompleted();

                foreach (var t in tickets)
                    _db.PaymentTickets.Add(new PaymentTicket(payment.Id, t.Id));

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await SendOfflineSaleConfirmationAsync(guestEmail, tickets, ct);

                return new OfflineSaleResult(
                    payment.Id,
                    total,
                    tickets.Select(t => t.Id).ToList());
                }
                catch
                {
                    await tx.RollbackAsync(ct);
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

    private static void ValidateSeats(IReadOnlyList<SeatCoordRequest>? seats)
    {
        if (seats is null || seats.Count == 0)
            throw new DomainException("Select at least one seat.");
        if (seats.Count > 10)
            throw new DomainException("A maximum of 10 seats can be sold at once.");
        if (seats.Distinct().Count() != seats.Count)
            throw new DomainException("Duplicate seats are not allowed.");
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    public async Task<RefundResult> RefundTicketAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Showtime)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
            throw new KeyNotFoundException($"Ticket {ticketId} not found.");
        if (ticket.Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be refunded.");
        if (ticket.Showtime.StartUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Expired tickets cannot be refunded.");

        // Кидає InvalidOperationException якщо квиток не може бути повернений
        ticket.MarkRefunded();
        await _db.SaveChangesAsync(ct);

        return new RefundResult(ticket.Id, ticket.FinalAmount, ticket.Status.ToString());
    }

    private async Task SendOfflineSaleConfirmationAsync(string? to, IReadOnlyCollection<Ticket> tickets, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to) || tickets.Count == 0) return;

        try
        {
            var attachments = tickets.Select(t => new EmailAttachment(
                FileName: $"ticket-{t.Id}.pdf",
                Data: TicketPdfGenerator.Generate(t, _qr.Generate(t.QrToken)),
                MimeType: "application/pdf")).ToList();
            var html = TicketEmailTemplate.BuildHtml(tickets, paidAtCashDesk: true, t => _qr.Generate(t.QrToken));
            var subject = TicketEmailTemplate.BuildSubject(tickets);
            await _email.SendAsync(to, subject, html, attachments, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send offline sale ticket confirmation email to {Recipient}", to);
        }
    }

}
