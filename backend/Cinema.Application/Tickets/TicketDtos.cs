namespace Cinema.Application.Tickets;

public sealed record SeatCoord(int Row, int Col);

public sealed record SeatMapDto(
    int ShowtimeId,
    string MovieTitle,
    string HallName,
    string CinemaBranchName,
    string City,
    DateTime StartUtc,
    string Format,
    decimal BasePrice,
    int Rows,
    int Cols,
    Cinema.Domain.Enums.SeatTypeCode[][] Layout,
    IReadOnlyList<SeatCoord> TakenSeats);

public sealed record SeatInfo(int Row, int Col, Cinema.Domain.Enums.SeatTypeCode Type, decimal Price);

public sealed record CreateTicketsRequest(
    int ShowtimeId,
    IReadOnlyList<SeatCoord> Seats,
    string? GuestEmail,
    string? PromoCode,
    int? LoyaltyPointsToRedeem);

public sealed record CreateTicketsResponse(
    int PaymentId,
    decimal TotalAmount,
    IReadOnlyList<TicketDto> Tickets);

public sealed record TicketDto(
    int Id,
    SeatInfo Seat,
    string QrToken,
    decimal FinalAmount);

public sealed record TicketDetailDto(
    int Id,
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    DateTime ShowtimeUtc,
    string HallName,
    string Format,
    SeatInfo Seat,
    string Status,
    decimal FinalAmount,
    string? GuestEmail,
    string? QrCodeUrl);

public sealed record TicketSummaryDto(
    int Id,
    int MovieId,
    string MovieTitle,
    DateTime ShowtimeUtc,
    string HallName,
    string Format,
    int Row,
    int Col,
    Cinema.Domain.Enums.TicketStatus Status,
    decimal FinalAmount,
    DateTime CreatedUtc);
