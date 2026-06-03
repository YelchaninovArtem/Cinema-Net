using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Workers;

/// <summary>
/// Фоновий сервіс: раз на хвилину шукає PendingPayment платежі, старші за 7 хвилин,
/// і скасовує їх разом із квитками. 7 хвилин відповідає повідомленню в UI.
/// </summary>
public sealed class AbandonedPaymentCleanupWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PaymentHoldTimeout = TimeSpan.FromMinutes(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedPaymentCleanupWorker> _logger;

    public AbandonedPaymentCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedPaymentCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up abandoned payments");
            }
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task CleanupAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var cutoff = DateTime.UtcNow - PaymentHoldTimeout;
        var abandoned = await db.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.CreatedUtc < cutoff)
            .ToListAsync(ct);

        var abandonedIds = abandoned.Select(p => p.Id).ToList();
        var ticketIds = await db.PaymentTickets
            .Where(pt => abandonedIds.Contains(pt.PaymentId))
            .Select(pt => pt.TicketId)
            .ToListAsync(ct);

        var tickets = await db.Tickets
            .Where(t => ticketIds.Contains(t.Id) && t.Status == TicketStatus.PendingPayment)
            .ToListAsync(ct);

        foreach (var payment in abandoned)
            payment.MarkFailed();

        foreach (var ticket in tickets)
            ticket.Cancel();

        // Відновлюємо списані loyalty-бали для відмінених квитків
        var cancelledIds = tickets.Select(t => t.Id).ToList();
        var redeemTxs = await db.LoyaltyTransactions
            .Where(tx => tx.TicketId.HasValue && cancelledIds.Contains(tx.TicketId!.Value) && tx.Delta < 0)
            .ToListAsync(ct);

        foreach (var tx in redeemTxs)
        {
            var account = await db.LoyaltyAccounts.FindAsync([tx.UserId], ct);
            account?.Restore(-tx.Delta);
        }

        db.LoyaltyTransactions.RemoveRange(redeemTxs);

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Canceled {Count} abandoned payments.", abandoned.Count);
    }
}
