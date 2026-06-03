using Cinema.Domain.Enums;

namespace Cinema.Application.Showtimes;

public sealed record ShowtimeAdminDto(
    int Id,
    int MovieId,
    string MovieTitle,
    int HallId,
    string HallName,
    int CinemaBranchId,
    string CinemaName,
    DateTime StartUtc,
    MovieFormat Format,
    decimal BasePrice
);

public sealed record CreateShowtimeRequest(
    int MovieId,
    int HallId,
    DateTime StartUtc,
    MovieFormat Format,
    decimal BasePrice
);

public sealed record UpdateShowtimeRequest(
    DateTime StartUtc,
    MovieFormat Format,
    decimal BasePrice
);

public sealed record ShowtimeConflictResult(
    bool HasConflict,
    int? ConflictingShowtimeId,
    string? ConflictingMovieTitle,
    DateTime? ConflictingStartUtc,
    DateTime? ConflictingEndUtc
);