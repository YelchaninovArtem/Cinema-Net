namespace Cinema.Application.Account;

public sealed record FavoriteSummaryDto(
    int MovieId,
    string Title,
    string? PosterUrl,
    double? AverageRating);