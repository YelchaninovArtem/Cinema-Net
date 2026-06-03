using Cinema.Application.Showtimes;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Admin;

public sealed class ShowtimeAdminService : IShowtimeAdminService
{
    private readonly CinemaDbContext _db;

    public ShowtimeAdminService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<ShowtimeAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var showtimes = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h!.CinemaBranch)
            .OrderBy(s => s.StartUtc)
            .ToListAsync(ct);

        return showtimes.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ShowtimeAdminDto>> GetByCinemaAsync(int cinemaBranchId, CancellationToken ct = default)
    {
        var showtimes = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h!.CinemaBranch)
            .Where(s => s.Hall!.CinemaBranchId == cinemaBranchId)
            .OrderBy(s => s.StartUtc)
            .ToListAsync(ct);

        return showtimes.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ShowtimeAdminDto>> GetByHallAsync(int hallId, CancellationToken ct = default)
    {
        var showtimes = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h!.CinemaBranch)
            .Where(s => s.HallId == hallId)
            .OrderBy(s => s.StartUtc)
            .ToListAsync(ct);

        return showtimes.Select(ToDto).ToList();
    }

    public async Task<ShowtimeAdminDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall)
                .ThenInclude(h => h!.CinemaBranch)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return showtime is null ? null : ToDto(showtime);
    }

    public async Task<int> CreateAsync(CreateShowtimeRequest request, CancellationToken ct = default)
    {
        var movieDuration = await GetMovieDuration(request.MovieId, ct);
        if (movieDuration <= 0)
        {
            throw new DomainException("Invalid movie ID or movie has no duration set.");
        }
        var endTime = request.StartUtc.AddMinutes(movieDuration);
        var conflict = await CheckConflictAsync(null, request.HallId, request.StartUtc, endTime, ct);
        if (conflict.HasConflict)
        {
            throw new DomainException($"Hall is already occupied by \"{conflict.ConflictingMovieTitle}\" at {conflict.ConflictingStartUtc:HH:mm}.");
        }

        var showtime = new Showtime(request.MovieId, request.HallId, request.StartUtc, request.Format, request.BasePrice);
        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync(ct);
        return showtime.Id;
    }

    public async Task UpdateAsync(int id, UpdateShowtimeRequest request, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new DomainException($"Showtime with ID {id} not found.");

        var movieDuration = showtime.Movie?.DurationMinutes ?? 120;
        var endTime = request.StartUtc.AddMinutes(movieDuration);
        var conflict = await CheckConflictAsync(id, showtime.HallId, request.StartUtc, endTime, ct);
        if (conflict.HasConflict)
        {
            throw new DomainException($"Hall is already occupied by \"{conflict.ConflictingMovieTitle}\" at {conflict.ConflictingStartUtc:HH:mm}.");
        }

        showtime.Reschedule(request.StartUtc);
        showtime.ChangeFormat(request.Format);
        showtime.Reprice(request.BasePrice);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var showtime = await _db.Showtimes.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Showtime with ID {id} not found.");

        var hasPurchasedTickets = await _db.Tickets
            .AnyAsync(t => t.ShowtimeId == id &&
                (t.Status == TicketStatus.Paid ||
                 t.Status == TicketStatus.Used ||
                 t.Status == TicketStatus.NotUsed ||
                 t.Status == TicketStatus.Refunded), ct);
        if (hasPurchasedTickets)
        {
            throw new DomainException("Cannot delete showtime with tickets already sold.");
        }

        var removableTickets = await _db.Tickets
            .Where(t => t.ShowtimeId == id)
            .ToListAsync(ct);

        if (removableTickets.Count > 0)
        {
            _db.Tickets.RemoveRange(removableTickets);
        }

        _db.Showtimes.Remove(showtime);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ShowtimeConflictResult> CheckConflictAsync(
        int? excludeShowtimeId,
        int hallId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct = default)
    {
        var query = _db.Showtimes
            .Include(s => s.Movie)
            .Where(s => s.HallId == hallId && s.Id != (excludeShowtimeId ?? 0));

        var conflicts = await query
            .Where(s => s.StartUtc < endUtc && s.StartUtc.AddMinutes(s.Movie!.DurationMinutes) > startUtc)
            .Select(s => new
            {
                s.Id,
                s.Movie!.Title,
                s.StartUtc,
                EndUtc = s.StartUtc.AddMinutes(s.Movie.DurationMinutes)
            })
            .ToListAsync(ct);

        if (conflicts.Count > 0)
        {
            var c = conflicts[0];
            return new ShowtimeConflictResult(true, c.Id, c.Title, c.StartUtc, c.EndUtc);
        }

        return new ShowtimeConflictResult(false, null, null, null, null);
    }

    private async Task<int> GetMovieDuration(int movieId, CancellationToken ct)
    {
        return await _db.Movies
            .Where(m => m.Id == movieId)
            .Select(m => m.DurationMinutes)
            .FirstOrDefaultAsync(ct);
    }

    private static ShowtimeAdminDto ToDto(Showtime s) => new(
        s.Id,
        s.MovieId,
        s.Movie!.Title,
        s.HallId,
        s.Hall!.Name,
        s.Hall.CinemaBranchId,
        s.Hall.CinemaBranch!.Name,
        DateTimeUtc.Mark(s.StartUtc),
        s.Format,
        s.BasePrice);
}
