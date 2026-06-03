using Cinema.Application.Loyalty;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Loyalty;

public sealed class LoyaltyService : ILoyaltyService
{
    // 1 бал на кожні 10 UAH (0.1 points per UAH)
    private const decimal PointsPerUah = 0.1m;
    // максимум 50% суми можна покрити балами
    private const decimal MaxRedeemFraction = 0.5m;

    private readonly CinemaDbContext _db;

    public LoyaltyService(CinemaDbContext db) => _db = db;

    public async Task<LoyaltyBalanceDto> GetBalanceAsync(string userId, CancellationToken ct = default)
    {
        var account = await _db.LoyaltyAccounts.FindAsync([userId], ct);
        return account is null
            ? new LoyaltyBalanceDto(0, 0)
            : new LoyaltyBalanceDto(account.Balance, account.TotalEarned);
    }

    public async Task EarnAsync(string userId, int ticketId, decimal amount, CancellationToken ct = default)
    {
        var points = (int)Math.Floor(amount * PointsPerUah);
        if (points <= 0) return;

        var account = await GetOrCreateAccountAsync(userId, ct);
        account.Earn(points);

        _db.LoyaltyTransactions.Add(
            new LoyaltyTransaction(userId, ticketId, points, $"Earned from ticket #{ticketId}"));

        await _db.SaveChangesAsync(ct);
    }

    public async Task RedeemForTicketAsync(string userId, int ticketId, int points, CancellationToken ct = default)
    {
        if (points <= 0) throw new ArgumentException("Points must be positive.");

        var account = await _db.LoyaltyAccounts.FindAsync([userId], ct)
            ?? throw new DomainException("No loyalty account found.");

        // Need to know ticket's final amount to compute max points
        var ticket = await _db.Tickets.FindAsync([ticketId], ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.PendingPayment)
            throw new InvalidOperationException("Loyalty points can only be applied to pending tickets.");

        var maxDiscount = Math.Floor(ticket.FinalAmount * MaxRedeemFraction);
        var maxPoints = (int)maxDiscount; // 1 point = 1 UAH
        var toRedeem = Math.Min(points, Math.Min(account.Balance, maxPoints));

        if (toRedeem <= 0)
            throw new DomainException("No points available to redeem.");

        account.Redeem(toRedeem);
        ticket.ApplyLoyalty(toRedeem, toRedeem);

        _db.LoyaltyTransactions.Add(
            new LoyaltyTransaction(userId, ticketId, -toRedeem, $"Redeemed for ticket #{ticketId}"));

        await _db.SaveChangesAsync(ct);
    }

    public async Task<LoyaltyBalanceDto> CancelRedeemAsync(string userId, int ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status != TicketStatus.PendingPayment)
            throw new InvalidOperationException("Can only cancel loyalty redemption for pending tickets.");

        var redeemTx = await _db.LoyaltyTransactions
            .Where(t => t.UserId == userId && t.TicketId == ticketId && t.Delta < 0)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No loyalty redemption found for this ticket.");

        var restoredPoints = -redeemTx.Delta;

        var account = await _db.LoyaltyAccounts.FindAsync([userId], ct)
            ?? throw new DomainException("No loyalty account found.");

        account.Restore(restoredPoints);
        ticket.ClearLoyalty();

        _db.LoyaltyTransactions.Remove(redeemTx);
        await _db.SaveChangesAsync(ct);

        return new LoyaltyBalanceDto(account.Balance, account.TotalEarned);
    }

    private async Task<LoyaltyAccount> GetOrCreateAccountAsync(string userId, CancellationToken ct)
    {
        var account = await _db.LoyaltyAccounts.FindAsync([userId], ct);
        if (account is null)
        {
            account = new LoyaltyAccount(userId);
            _db.LoyaltyAccounts.Add(account);
        }
        return account;
    }
}