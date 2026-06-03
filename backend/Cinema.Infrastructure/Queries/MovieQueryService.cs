using Cinema.Application.Movies;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Queries;

public sealed class MovieQueryService : IMovieQueryService
{
    private readonly CinemaDbContext _db;

    public MovieQueryService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<MovieSummaryDto>> GetMoviesAsync(
        MovieFilters filters, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Визначаємо набір MovieId, що мають майбутні сеанси з урахуванням фільтрів
        var showtimeQuery = _db.Showtimes
            .Where(s => s.StartUtc.AddMinutes(1) > now)
            .AsQueryable();

        if (filters.City is not null)
            showtimeQuery = showtimeQuery.Where(s => s.Hall.CinemaBranch.City == filters.City);

        if (filters.Date.HasValue)
        {
            var from = filters.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to   = from.AddDays(1);
            showtimeQuery = showtimeQuery.Where(s => s.StartUtc >= from && s.StartUtc < to);
        }

        if (filters.Format is not null)
        {
            var fmt = ParseFormat(filters.Format);
            if (fmt.HasValue)
                showtimeQuery = showtimeQuery.Where(s => s.Format == fmt.Value);
        }

        var eligibleMovieIds = showtimeQuery.Select(s => s.MovieId).Distinct();

        var query = _db.Movies
            .Include(m => m.Genres)
            .Where(m => eligibleMovieIds.Contains(m.Id))
            .AsQueryable();

        if (filters.GenreId.HasValue)
            query = query.Where(m => m.Genres.Any(g => g.Id == filters.GenreId.Value));

        if (!string.IsNullOrWhiteSpace(filters.Title))
        {
            var title = filters.Title.Trim();
            query = query.Where(m => m.Title.Contains(title));
        }

        var movies = await query
            .AsNoTracking()
            .OrderBy(m => m.Title)
            .ToListAsync(ct);

        // Формати, доступні для кожного з відібраних фільмів
        var movieIds = movies.Select(m => m.Id).ToList();
        var formatGroups = await _db.Showtimes
            .Where(s => movieIds.Contains(s.MovieId) && s.StartUtc.AddMinutes(1) > now)
            .GroupBy(s => s.MovieId)
            .Select(g => new { MovieId = g.Key, Formats = g.Select(s => s.Format).Distinct().ToList() })
            .ToListAsync(ct);

        var formatsMap = formatGroups.ToDictionary(g => g.MovieId, g => g.Formats);

        return movies.Select(m => new MovieSummaryDto(
            m.Id,
            m.Title,
            m.PosterUrl,
            m.DurationMinutes,
            m.AgeRating.ToString(),
            m.Genres.Select(g => g.Name).OrderBy(n => n).ToList(),
            formatsMap.TryGetValue(m.Id, out var fmts)
                ? fmts.Select(FormatToString).Order().ToList()
                : []
        )).ToList();
    }

    public async Task<MovieDetailDto?> GetMovieByIdAsync(int id, CancellationToken ct = default)
    {
        var m = await _db.Movies
            .Include(m => m.Genres)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (m is null) return null;

        return new MovieDetailDto(
            m.Id,
            m.Title,
            m.Description,
            m.PosterUrl,
            m.TrailerUrl,
            m.DurationMinutes,
            m.AgeRating.ToString(),
            m.ReleaseDateUtc,
            m.Genres.Select(g => g.Name).OrderBy(n => n).ToList()
        );
    }

    private static MovieFormat? ParseFormat(string format) =>
        format.ToUpperInvariant() switch
        {
            "2D"   => MovieFormat.TwoD,
            "3D"   => MovieFormat.ThreeD,
            "IMAX" => MovieFormat.Imax,
            _      => null
        };

    internal static string FormatToString(MovieFormat f) => f switch
    {
        MovieFormat.TwoD   => "2D",
        MovieFormat.ThreeD => "3D",
        MovieFormat.Imax   => "IMAX",
        _                  => f.ToString()
    };
}
