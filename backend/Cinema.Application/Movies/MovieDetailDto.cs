namespace Cinema.Application.Movies;

public sealed record MovieDetailDto(
    int Id,
    string Title,
    string Description,
    string? PosterUrl,
    string? TrailerUrl,
    int DurationMinutes,
    string AgeRating,
    DateTime ReleaseDateUtc,
    IReadOnlyList<string> Genres
);
