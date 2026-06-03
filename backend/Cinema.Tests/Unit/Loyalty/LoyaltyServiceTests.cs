using Cinema.Application.Loyalty;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Loyalty;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Tests.Unit.Loyalty;

public sealed class LoyaltyServiceTests : IDisposable
{
    private readonly CinemaDbContext _db;
    private readonly LoyaltyService _svc;

    public LoyaltyServiceTests()
    {
        var opts = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new CinemaDbContext(opts);
        _svc = new LoyaltyService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetBalance_NoAccount_ReturnsZero()
    {
        var dto = await _svc.GetBalanceAsync("nouser");
        dto.Balance.Should().Be(0);
        dto.TotalEarned.Should().Be(0);
    }

    [Fact]
    public async Task EarnAsync_CreatesAccountAndAddsPoints()
    {
        // Create a pending ticket
        var ticket = new Ticket(1, 1, 1, SeatTypeCode.Standard, 100m, "qrtoken");
        ticket.SetPurchaser("u1", null);
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        await _svc.EarnAsync("u1", ticket.Id, 100m); // 100 * 0.1 = 10 points

        var dto = await _svc.GetBalanceAsync("u1");
        dto.Balance.Should().Be(10);
        dto.TotalEarned.Should().Be(10);

        var tx = await _db.LoyaltyTransactions.FirstAsync(t => t.UserId == "u1");
        tx.Delta.Should().Be(10);
        tx.TicketId.Should().Be(ticket.Id);
    }

    [Fact]
    public async Task RedeemForTicketAsync_ReducesBalanceAndAppliesToTicket()
    {
        // Setup: user with 50 points
        var ticket = new Ticket(1, 2, 2, SeatTypeCode.Standard, 200m, "qrtoken2");
        ticket.SetPurchaser("u2", null);
        ticket.SetFinalAmount(200m);
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        await _svc.EarnAsync("u2", ticket.Id, 500m); // 50 points

        // Redeem 30 points on ticket
        await _svc.RedeemForTicketAsync("u2", ticket.Id, 30);

        var dto = await _svc.GetBalanceAsync("u2");
        dto.Balance.Should().Be(20); // 50 - 30

        var tx = await _db.LoyaltyTransactions.FirstAsync(t => t.UserId == "u2" && t.Delta < 0);
        tx.Delta.Should().Be(-30);
        tx.TicketId.Should().Be(ticket.Id);

        // Ticket fields updated via ApplyLoyalty
        // Reload ticket
        var reloaded = await _db.Tickets.FindAsync(ticket.Id);
        reloaded!.LoyaltyPointsRedeemed.Should().Be(30);
        reloaded.LoyaltyDiscount.Should().Be(30m);
    }

    [Fact]
    public async Task RedeemForTicketAsync_CapsAtMaxFraction()
    {
        // Ticket with final amount 100, max redeem 50 points (50%)
        // User has 100 points
        var ticket = new Ticket(1, 3, 3, SeatTypeCode.Standard, 100m, "qrtoken3");
        ticket.SetPurchaser("u3", null);
        ticket.SetFinalAmount(100m);
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        await _svc.EarnAsync("u3", ticket.Id, 1000m); // 100 points

        await _svc.RedeemForTicketAsync("u3", ticket.Id, 100); // request 100

        var dto = await _svc.GetBalanceAsync("u3");
        // max 50% -> 50 points used
        dto.Balance.Should().Be(50); // 100 - 50

        var ticketReloaded = await _db.Tickets.FindAsync(ticket.Id);
        ticketReloaded!.LoyaltyPointsRedeemed.Should().Be(50);
        ticketReloaded.LoyaltyDiscount.Should().Be(50m);
    }

    [Fact]
    public async Task CancelRedeemAsync_RestoresPointsAndClearsTicket()
    {
        // Setup: ticket with redemption
        var ticket = new Ticket(1, 4, 4, SeatTypeCode.Standard, 200m, "qrtoken4");
        ticket.SetPurchaser("u4", null);
        ticket.SetFinalAmount(200m);
        ticket.ApplyLoyalty(30, 30m);
        // Create account and redeem manually? Actually we need to set up via service to have transaction.
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        // Set up account manually: Earn then Redeem via service
        await _svc.EarnAsync("u4", ticket.Id, 500m); // 50 points
        await _svc.RedeemForTicketAsync("u4", ticket.Id, 30);

        // Now cancel
        await _svc.CancelRedeemAsync("u4", ticket.Id);

        var dto = await _svc.GetBalanceAsync("u4");
        dto.Balance.Should().Be(50); // restored 30 -> 80? Actually initial 50 - 30 = 20, after restore -> 50. Good.

        var txCount = await _db.LoyaltyTransactions.CountAsync(t => t.UserId == "u4");
        txCount.Should().Be(1); // earn tx remains; redeem tx removed by cancel

        var ticketReloaded = await _db.Tickets.FindAsync(ticket.Id);
        ticketReloaded!.LoyaltyPointsRedeemed.Should().Be(0);
        ticketReloaded.LoyaltyDiscount.Should().Be(0m);
    }
}
