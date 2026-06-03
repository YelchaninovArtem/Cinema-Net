using Cinema.Application.Movies;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Admin;

public sealed class MovieAdminService : IMovieAdminService
{
    private readonly CinemaDbContext _db;

    public MovieAdminService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<MovieAdminDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Movies
            .Include(m => m.Genres)
            .OrderBy(m => m.Title)
            .Select(m => new MovieAdminDto(
                m.Id,
                m.Title,
                m.Description,
                m.DurationMinutes,
                m.AgeRating,
                m.ReleaseDateUtc,
                m.PosterUrl,
                m.TrailerUrl,
                m.Genres.Select(g => g.Id).ToList()))
            .ToListAsync(ct);

    public async Task<MovieAdminDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Movies
            .Include(m => m.Genres)
            .Where(m => m.Id == id)
            .Select(m => new MovieAdminDto(
                m.Id,
                m.Title,
                m.Description,
                m.DurationMinutes,
                m.AgeRating,
                m.ReleaseDateUtc,
                m.PosterUrl,
                m.TrailerUrl,
                m.Genres.Select(g => g.Id).ToList()))
            .FirstOrDefaultAsync(ct);

    public async Task<int> CreateAsync(CreateMovieRequest request, CancellationToken ct = default)
    {
        var movie = new Movie(
            request.Title,
            request.Description,
            request.DurationMinutes,
            request.AgeRating,
            request.ReleaseDateUtc,
            request.PosterUrl,
            request.TrailerUrl);

        if (request.GenreIds is { Length: > 0 })
        {
            var genres = await _db.Genres
                .Where(g => request.GenreIds.Contains(g.Id))
                .ToListAsync(ct);

            foreach (var genre in genres)
                movie.AddGenre(genre);
        }

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(ct);
        return movie.Id;
    }

    public async Task UpdateAsync(int id, UpdateMovieRequest request, CancellationToken ct = default)
    {
        var movie = await _db.Movies
            .Include(m => m.Genres)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new DomainException($"Movie with ID {id} not found.");

        movie.Rename(request.Title);
        movie.UpdateDescription(request.Description);
        movie.SetDuration(request.DurationMinutes);
        movie.SetReleaseDate(request.ReleaseDateUtc);
        movie.SetPosterUrl(request.PosterUrl);
        movie.SetTrailerUrl(request.TrailerUrl);

        // Оновлюємо жанри — видаляємо старі, додаємо нові
        if (request.GenreIds is not null)
        {
            var newGenreIds = request.GenreIds.ToHashSet();
            var genresToRemove = movie.Genres.Where(g => !newGenreIds.Contains(g.Id)).ToList();
            
            foreach (var genre in genresToRemove)
                movie.Genres.ToList().Remove(genre);

            var currentGenreIds = movie.Genres.Select(g => g.Id).ToHashSet();
            var genresToAdd = await _db.Genres
                .Where(g => newGenreIds.Contains(g.Id) && !currentGenreIds.Contains(g.Id))
                .ToListAsync(ct);

            foreach (var genre in genresToAdd)
                movie.AddGenre(genre);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var movie = await _db.Movies.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Movie with ID {id} not found.");

        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync(ct);
    }
}
