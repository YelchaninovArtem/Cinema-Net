using Cinema.Domain.Enums;

namespace Cinema.Application.Loyalty;

public interface ILoyaltyService
{
    Task<LoyaltyBalanceDto> GetBalanceAsync(string userId, CancellationToken ct = default);

    /// <summary>Нараховує бали за покупку квитка (1 бал = 10 UAH).</summary>
    Task EarnAsync(string userId, int ticketId, decimal amount, CancellationToken ct = default);

    /// <summary>Списання балів для покупки квитка (1 бал = 1 UAH знижки, макс. 50%).</summary>
    Task RedeemForTicketAsync(string userId, int ticketId, int points, CancellationToken ct = default);

    /// <summary>Скасовує списання балів для квитка і повертає їх на баланс.</summary>
    Task<LoyaltyBalanceDto> CancelRedeemAsync(string userId, int ticketId, CancellationToken ct = default);
}
