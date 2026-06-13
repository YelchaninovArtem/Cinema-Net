using Cinema.Application.Genres;
using Cinema.Application.Localization;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Queries;

public sealed class GenreQueryService : IGenreQueryService
{
    private readonly CinemaDbContext _db;
    private readonly IContentLocalizationService _localizer;

    public GenreQueryService(CinemaDbContext db)
        : this(db, new PassthroughContentLocalizationService())
    {
    }

    public GenreQueryService(CinemaDbContext db, IContentLocalizationService localizer)
    {
        _db = db;
        _localizer = localizer;
    }

    public async Task<IReadOnlyList<GenreDto>> GetAllAsync(CancellationToken ct = default) =>
        await GetAllAsync(null, ct);

    public async Task<IReadOnlyList<GenreDto>> GetAllAsync(string? language, CancellationToken ct = default) =>
        await _db.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct)
            .ContinueWith(t => t.Result
                .Select(g => new GenreDto(g.Id, _localizer.LocalizeGenres([g.Name], language)[0]))
                .OrderBy(g => g.Name)
                .ToList(), ct);

    public async Task<IReadOnlyList<GenreDto>> EnsureAsync(string[] names, CancellationToken ct = default)
    {
        var trimmed = names.Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (trimmed.Length == 0) return [];

        var existing = await _db.Genres
            .Where(g => trimmed.Contains(g.Name))
            .ToListAsync(ct);

        var existingNames = existing.Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toCreate = trimmed.Where(n => !existingNames.Contains(n)).Select(n => new Genre(n)).ToList();

        if (toCreate.Count > 0)
        {
            _db.Genres.AddRange(toCreate);
            await _db.SaveChangesAsync(ct);
            existing.AddRange(toCreate);
        }

        return existing.Select(g => new GenreDto(g.Id, g.Name)).OrderBy(g => g.Name).ToList();
    }

    private sealed class PassthroughContentLocalizationService : IContentLocalizationService
    {
        public string NormalizeLanguage(string? language) => "en";
        public IReadOnlyList<string> LocalizeGenres(IEnumerable<string> genres, string? language) => genres.OrderBy(g => g).ToList();
        public Task<string> LocalizeMovieDescriptionAsync(string description, string? language, CancellationToken ct = default) => Task.FromResult(description);
    }
}
