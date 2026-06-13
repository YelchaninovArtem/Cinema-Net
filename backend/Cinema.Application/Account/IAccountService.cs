using Cinema.Application.Tickets;

namespace Cinema.Application.Account;

public interface IAccountService
{
    Task<IReadOnlyCollection<TicketSummaryDto>> GetUserTicketsAsync(string userId, CancellationToken ct = default);
    Task<TicketDetailDto?> GetTicketDetailAsync(int ticketId, string userId, CancellationToken ct = default);
    Task<Stream> GetTicketQrAsync(int ticketId, string userId, CancellationToken ct = default);
    Task<byte[]> GetTicketPdfAsync(int ticketId, string userId, CancellationToken ct = default);
    Task<AccountRefundResult> RefundTicketAsync(int ticketId, string userId, CancellationToken ct = default);

    Task<IReadOnlyList<FavoriteSummaryDto>> GetFavoritesAsync(string userId, CancellationToken ct = default);
    Task AddFavoriteAsync(string userId, int movieId, CancellationToken ct = default);
    Task RemoveFavoriteAsync(string userId, int movieId, CancellationToken ct = default);
}

public sealed record AccountRefundResult(
    int TicketId,
    decimal RefundedAmount,
    string Status);
