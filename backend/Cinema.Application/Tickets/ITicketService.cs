using Cinema.Domain.Enums;

namespace Cinema.Application.Tickets;

public interface ITicketService
{
    Task<SeatMapDto?> GetSeatMapAsync(int showtimeId, CancellationToken ct = default);
    Task<CreateTicketsResponse?> CreateTicketsAsync(CreateTicketsRequest request, string? userId, CancellationToken ct = default);
    Task<TicketDetailDto?> GetTicketDetailAsync(int ticketId, CancellationToken ct = default);
    Task<IReadOnlyCollection<TicketSummaryDto>> GetUserTicketsAsync(string userId, CancellationToken ct = default);
}
