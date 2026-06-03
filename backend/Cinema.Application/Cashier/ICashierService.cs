using Cinema.Domain.Enums;

namespace Cinema.Application.Cashier;

public sealed record VerifyTicketResult(
    int TicketId,
    string MovieTitle,
    string HallName,
    string CinemaBranchName,
    DateTime ShowtimeUtc,
    string Format,
    int Row,
    int Col,
    SeatTypeCode SeatType,
    string Status,
    string? GuestEmail,
    decimal FinalAmount);

public sealed record OfflineSaleRequest(
    int ShowtimeId,
    IReadOnlyList<SeatCoordRequest> Seats,
    string? GuestEmail);

public sealed record SeatCoordRequest(int Row, int Col);

public sealed record OfflineSaleResult(
    int PaymentId,
    decimal TotalAmount,
    IReadOnlyList<int> TicketIds);

public sealed record RefundResult(
    int TicketId,
    decimal RefundedAmount,
    string Status);

public interface ICashierService
{
    Task<VerifyTicketResult?> VerifyByQrAsync(string qrToken, CancellationToken ct = default);
    Task<VerifyTicketResult?> VerifyByIdAsync(int ticketId, CancellationToken ct = default);
    Task<VerifyTicketResult> UseTicketAsync(int ticketId, CancellationToken ct = default);
    Task<OfflineSaleResult?> CreateOfflineSaleAsync(OfflineSaleRequest request, CancellationToken ct = default);
    Task<RefundResult> RefundTicketAsync(int ticketId, CancellationToken ct = default);
}
