namespace Cinema.Application.Showtimes;

public sealed record ShowtimeFilters(
    int? MovieId = null,
    string? City = null,
    DateOnly? Date = null,
    string? Format = null
);
