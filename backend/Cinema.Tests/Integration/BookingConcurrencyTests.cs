using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Tickets;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cinema.Tests.Integration;

public sealed class TicketConcurrencyTests : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CinemaWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public TicketConcurrencyTests(CinemaWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Seat map ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSeatMap_ValidShowtime_ReturnsSeatMap()
    {
        var showtimeId = await GetFirstShowtimeIdAsync();
        var map = await _client.GetFromJsonAsync<SeatMapDto>($"/api/showtimes/{showtimeId}/seats", JsonOpts);

        map.Should().NotBeNull();
        map!.Rows.Should().BeGreaterThan(0);
        map.Cols.Should().BeGreaterThan(0);
        map.Layout.Length.Should().Be(map.Rows);
        map.Layout[0].Length.Should().Be(map.Cols);
    }

    [Fact]
    public async Task GetSeatMap_InvalidShowtime_ReturnsNotFound()
    {
        var resp = await _client.GetAsync("/api/showtimes/999999/seats");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Create tickets (instant buy) ─────────────────────────────────────────

    [Fact]
    public async Task CreateTickets_GuestWithEmail_ReturnsOk()
    {
        var showtimeId = await GetFirstShowtimeIdAsync();
        var map = await _client.GetFromJsonAsync<SeatMapDto>($"/api/showtimes/{showtimeId}/seats", JsonOpts);
        map.Should().NotBeNull();
        var freeSeat = FindFreeSeat(map);

        var req = new CreateTicketsRequest(
            showtimeId,
            [new SeatCoord(freeSeat.Row, freeSeat.Col)],
            GuestEmail: $"guest_{Guid.NewGuid():N}@test.com",
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        );

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        result.Should().NotBeNull();
        result!.Tickets.Should().ContainSingle();
        result.Tickets[0].QrToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTickets_SameSeatTwice_SecondGetsConflict()
    {
        var showtimeId = await GetFirstShowtimeIdAsync();
        var map = await _client.GetFromJsonAsync<SeatMapDto>($"/api/showtimes/{showtimeId}/seats", JsonOpts);
        map.Should().NotBeNull();
        var freeSeat = FindFreeSeat(map);

        var buildReq = () => new CreateTicketsRequest(
            showtimeId,
            [new SeatCoord(freeSeat.Row, freeSeat.Col)],
            $"g_{Guid.NewGuid():N}@t.com",
            null, null);

        var t1 = _client.PostAsJsonAsync("/api/tickets", buildReq());
        var t2 = _client.PostAsJsonAsync("/api/tickets", buildReq());
        var results = await Task.WhenAll(t1, t2);

        var statuses = results.Select(r => r.StatusCode).ToArray();
        statuses.Should().Contain(HttpStatusCode.OK);
        statuses.Should().Contain(HttpStatusCode.Conflict); // 409 or 400 depending on service; our service returns null -> 204? Actually null returns NoContent? In controller we didn't yet implement tickets endpoint, but TicketService returns null for conflict, controller should translate to Conflict. We'll handle later. For now, adjust expectation: we might return Conflict (409) or BadRequest. We'll check after controller is written. For now, just ensure not both OK.

        // Ensure at least one succeeded and at least one failed
        statuses.Count(s => s == HttpStatusCode.OK).Should().Be(1);
        statuses.Count(s => (int)s >= 400).Should().Be(1);
    }

    [Fact]
    public async Task CreateTickets_NoGuestEmail_ReturnsBadRequest()
    {
        var showtimeId = await GetFirstShowtimeIdAsync();
        var req = new CreateTicketsRequest(
            showtimeId,
            [new SeatCoord(1, 1)],
            GuestEmail: null,
            PromoCode:null,
            LoyaltyPointsToRedeem:null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTickets_WithoutSeats_ReturnsBadRequest()
    {
        var req = new CreateTicketsRequest(
            ShowtimeId: await GetFirstShowtimeIdAsync(),
            Seats: [],
            GuestEmail: $"guest_{Guid.NewGuid():N}@test.com",
            PromoCode: null,
            LoyaltyPointsToRedeem: null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTickets_WithDuplicateSeats_ReturnsBadRequest()
    {
        var seat = new SeatCoord(1, 1);
        var req = new CreateTicketsRequest(
            ShowtimeId: await GetFirstShowtimeIdAsync(),
            Seats: [seat, seat],
            GuestEmail: $"guest_{Guid.NewGuid():N}@test.com",
            PromoCode: null,
            LoyaltyPointsToRedeem: null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── AbandonedPaymentCleanupWorker ─────────────────────────────────────────

    [Fact]
    public async Task AbandonedPaymentWorker_CancelsOldPendingPayments()
    {
        // Create a pending payment with a ticket using TicketService directly
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ITicketService>();
        var showtimeId = await GetFirstShowtimeIdAsync();

        var req = new CreateTicketsRequest(
            showtimeId,
            [new SeatCoord(4, 6)],
            "abandoned@test.com", null, null);

        var result = await svc.CreateTicketsAsync(req, userId: null);
        result.Should().NotBeNull();

        // Manually set Payment.CreatedUtc to older than the payment hold timeout via raw SQL
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Payments SET CreatedUtc = DATEADD(MINUTE, -20, GETUTCDATE()) WHERE Id = {result!.PaymentId}");
        rowsAffected.Should().Be(1);

        // Run cleanup
        var worker = new AbandonedPaymentCleanupWorker(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AbandonedPaymentCleanupWorker>.Instance);
        await worker.CleanupAsync();

        // Reload from DB (clear tracker to avoid cached entities)
        db.ChangeTracker.Clear();
        var payment = await db.Payments.FindAsync(result.PaymentId);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);

        var ticket = await db.Tickets.FindAsync(result.Tickets[0].Id);
        ticket.Should().NotBeNull();
        ticket!.Status.Should().Be(TicketStatus.Cancelled);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<int> GetFirstShowtimeIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var s  = await db.Showtimes.OrderBy(s => s.StartUtc).FirstOrDefaultAsync();
        s.Should().NotBeNull();
        return s!.Id;
    }

    private static SeatCoord FindFreeSeat(SeatMapDto map)
    {
        for (var r = 1; r <= map.Rows; r++)
            for (var c = 1; c <= map.Cols; c++)
                if (!map.TakenSeats.Any(t => t.Row == r && t.Col == c))
                    return new SeatCoord(r, c);
        throw new InvalidOperationException("No free seats in showtime.");
    }
}
