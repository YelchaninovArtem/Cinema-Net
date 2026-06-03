namespace Cinema.Application.Showtimes;

public sealed record ShowtimeDto(
    int Id,
    int MovieId,
    string MovieTitle,
    string HallName,
    string CinemaBranchName,
    string City,
    DateTime StartUtc,
    string Format,
    decimal BasePrice
);
