using Cinema.Application.Showtimes;
using Cinema.Domain.Enums;
using Cinema.Infrastructure;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Queries;

public sealed class ShowtimeQueryService : IShowtimeQueryService
{
    private readonly CinemaDbContext _db;

    public ShowtimeQueryService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<ShowtimeDto>> GetShowtimesAsync(
        ShowtimeFilters filters, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var query = _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .Where(s => s.StartUtc.AddMinutes(1) > now)
            .AsQueryable();

        if (filters.MovieId.HasValue)
            query = query.Where(s => s.MovieId == filters.MovieId.Value);

        if (filters.City is not null)
            query = query.Where(s => s.Hall.CinemaBranch.City == filters.City);

        if (filters.Date.HasValue)
        {
            var from = filters.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to   = from.AddDays(1);
            query = query.Where(s => s.StartUtc >= from && s.StartUtc < to);
        }

        if (filters.Format is not null)
        {
            var fmt = ParseFormat(filters.Format);
            if (fmt.HasValue)
                query = query.Where(s => s.Format == fmt.Value);
        }

        var showtimes = await query
            .AsNoTracking()
            .OrderBy(s => s.StartUtc)
            .ToListAsync(ct);

        return showtimes.Select(s => new ShowtimeDto(
            s.Id,
            s.MovieId,
            s.Movie.Title,
            s.Hall.Name,
            s.Hall.CinemaBranch.Name,
            s.Hall.CinemaBranch.City,
            DateTimeUtc.Mark(s.StartUtc),
            MovieQueryService.FormatToString(s.Format),
            s.BasePrice
        )).ToList();
    }

    private static MovieFormat? ParseFormat(string format) =>
        format.ToUpperInvariant() switch
        {
            "2D"   => MovieFormat.TwoD,
            "3D"   => MovieFormat.ThreeD,
            "IMAX" => MovieFormat.Imax,
            _      => null
        };
}
