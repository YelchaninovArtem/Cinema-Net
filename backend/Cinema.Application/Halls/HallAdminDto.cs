using Cinema.Domain.Enums;

namespace Cinema.Application.Halls;

public sealed record HallAdminDto(
    int Id,
    int CinemaBranchId,
    string CinemaName,
    string HallName,
    int Rows,
    int Cols,
    SeatTypeCode[][] Layout);

public sealed record CreateHallRequest(
    int CinemaBranchId,
    string Name,
    int Rows,
    int Cols,
    SeatTypeCode[][] Layout);

public sealed record UpdateHallRequest(
    string Name,
    int Rows,
    int Cols,
    SeatTypeCode[][] Layout);

public sealed record SeatTypeDto(int Row, int Col, SeatTypeCode Type);
