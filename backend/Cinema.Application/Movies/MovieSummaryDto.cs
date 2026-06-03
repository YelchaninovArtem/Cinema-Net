namespace Cinema.Application.Movies;

public sealed record MovieSummaryDto(
    int Id,
    string Title,
    string? PosterUrl,
    int DurationMinutes,
    string AgeRating,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> AvailableFormats
);
