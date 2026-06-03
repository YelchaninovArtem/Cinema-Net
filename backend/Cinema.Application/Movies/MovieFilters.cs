namespace Cinema.Application.Movies;

public sealed record MovieFilters(
    string? Title = null,
    string? City = null,
    DateOnly? Date = null,
    string? Format = null,
    int? GenreId = null
);
