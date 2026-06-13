using Cinema.Application.Account;
using Cinema.Application.QrCode;
using Cinema.Application.Tickets;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Email;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Account;

public sealed class AccountService : IAccountService
{
    private readonly CinemaDbContext _db;
    private readonly IQrCodeGenerator _qr;

    public AccountService(CinemaDbContext db, IQrCodeGenerator qr)
    {
        _db = db;
        _qr = qr;
    }

    public async Task<IReadOnlyCollection<TicketSummaryDto>> GetUserTicketsAsync(
        string userId, CancellationToken ct = default)
    {
        var tickets = await _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .Where(t => t.UserId == userId
                        && (t.Status == TicketStatus.Paid
                            || t.Status == TicketStatus.Used
                            || t.Status == TicketStatus.NotUsed
                            || t.Status == TicketStatus.Refunded))
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
            DateTimeUtc.Mark(t.CreatedUtc)
        )).ToList();

        return result.AsReadOnly();
    }

    public async Task<TicketDetailDto?> GetTicketDetailAsync(
        int ticketId, string userId, CancellationToken ct = default)
    {
        var t = await _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, ct);

        if (t is null) return null;

        var seatInfo = new SeatInfo(t.Row, t.Col, t.SeatType, t.Price);
        var qrCodeUrl = $"/api/tickets/{t.Id}/qr";

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
            qrCodeUrl);
    }

    public async Task<Stream> GetTicketQrAsync(int ticketId, string userId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FindAsync([ticketId], ct);
        if (ticket is null) throw new KeyNotFoundException($"Ticket {ticketId} not found.");
        if (ticket.UserId != userId) throw new UnauthorizedAccessException("Access denied.");
        var png = _qr.Generate(ticket.QrToken);
        return new MemoryStream(png);
    }

    public async Task<byte[]> GetTicketPdfAsync(int ticketId, string userId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, ct);

        if (ticket is null)
            throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        var qrCode = _qr.Generate(ticket.QrToken);
        return TicketPdfGenerator.Generate(ticket, qrCode);
    }

    public async Task<AccountRefundResult> RefundTicketAsync(
        int ticketId, string userId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Showtime)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, ct);

        if (ticket is null)
            throw new KeyNotFoundException($"Ticket {ticketId} not found.");
        if (ticket.Status != TicketStatus.Paid)
            throw new InvalidOperationException("Only paid tickets can be refunded.");
        if (ticket.Showtime.StartUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Expired tickets cannot be refunded.");

        ticket.MarkRefunded();
        await _db.SaveChangesAsync(ct);

        return new AccountRefundResult(ticket.Id, ticket.FinalAmount, ticket.Status.ToString());
    }

    public async Task<IReadOnlyList<FavoriteSummaryDto>> GetFavoritesAsync(
        string userId, CancellationToken ct = default)
    {
        return await _db.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => new FavoriteSummaryDto(
                f.MovieId,
                f.Movie.Title,
                f.Movie.PosterUrl,
                null))
            .ToListAsync(ct);
    }

    public async Task AddFavoriteAsync(string userId, int movieId, CancellationToken ct = default)
    {
        var exists = await _db.Favorites
            .AnyAsync(f => f.UserId == userId && f.MovieId == movieId, ct);
        if (exists) return;

        _db.Favorites.Add(new Favorite { UserId = userId, MovieId = movieId });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveFavoriteAsync(string userId, int movieId, CancellationToken ct = default)
    {
        var fav = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId, ct);
        if (fav is null) return;

        _db.Favorites.Remove(fav);
        await _db.SaveChangesAsync(ct);
    }
}
