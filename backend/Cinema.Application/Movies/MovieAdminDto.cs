using Cinema.Domain.Enums;

namespace Cinema.Application.Movies;

public sealed record MovieAdminDto(
    int Id,
    string Title,
    string Description,
    int DurationMinutes,
    AgeRating AgeRating,
    DateTime ReleaseDateUtc,
    string? PosterUrl,
    string? TrailerUrl,
    IReadOnlyList<int> GenreIds);

public sealed record CreateMovieRequest(
    string Title,
    string Description,
    int DurationMinutes,
    AgeRating AgeRating,
    DateTime ReleaseDateUtc,
    string? PosterUrl = null,
    string? TrailerUrl = null,
    int[]? GenreIds = null);

public sealed record UpdateMovieRequest(
    string Title,
    string Description,
    int DurationMinutes,
    AgeRating AgeRating,
    DateTime ReleaseDateUtc,
    string? PosterUrl = null,
    string? TrailerUrl = null,
    int[]? GenreIds = null);
