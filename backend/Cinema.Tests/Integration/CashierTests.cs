using System.Net;
using System.Net.Http.Json;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

[Collection("Integration")]
public sealed class CashierTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // --- локальні DTO для десеріалізації відповідей cashier API ---
    private sealed record AuthTokenDto(string AccessToken, string RefreshToken);
    private sealed record OfflineSaleResultDto(int PaymentId, decimal TotalAmount, IReadOnlyList<int> TicketIds);
    private sealed record VerifyTicketResultDto(int TicketId, string MovieTitle, string HallName,
        string CinemaBranchName, DateTime ShowtimeUtc, string Format, int Row, int Col,
        string SeatType, string Status, string? GuestEmail, decimal FinalAmount);
    private sealed record RefundResultDto(int TicketId, decimal RefundedAmount, string Status);
    private sealed record TicketSummaryDto(int Id, int MovieId, string MovieTitle, DateTime ShowtimeUtc,
        string HallName, string Format, int Row, int Col, string Status, decimal FinalAmount, DateTime CreatedUtc);

    private async Task LoginAsCashierAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "cashier@cinema.local", password = "Cashier_123!" });
        r.IsSuccessStatusCode.Should().BeTrue($"cashier login must succeed, got {r.StatusCode}");
        var auth = await r.Content.ReadFromJsonAsync<AuthTokenDto>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.AccessToken);
    }

    private async Task LoginAsClientAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "client@cinema.local", password = "Client_123!" });
        r.IsSuccessStatusCode.Should().BeTrue();
        var auth = await r.Content.ReadFromJsonAsync<AuthTokenDto>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.AccessToken);
    }

    /// <summary>Повертає перший showtime із БД для тестів.</summary>
    private int GetFirstShowtimeId()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        return db.Showtimes.OrderBy(s => s.Id).First().Id;
    }

    private async Task<OfflineSaleResultDto> CreateOfflineSaleAsync(int showtimeId, int row, int col)
    {
        var resp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row, col } },
            guestEmail = $"cash-sale-{row}-{col}@example.com"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.OK, $"offline sale for ({row},{col}) must succeed");
        var result = await resp.Content.ReadFromJsonAsync<OfflineSaleResultDto>();
        result.Should().NotBeNull();
        return result!;
    }

    // ─── 1. Verify — без авторизації → 401 ───────────────────────────────────

    [Fact]
    public async Task Verify_Returns401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/cashier/ticket/verify?qr=abc123");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── 2. Verify — клієнт → 403 ────────────────────────────────────────────

    [Fact]
    public async Task Verify_Returns403_WhenClient()
    {
        var client = factory.CreateClient();
        await LoginAsClientAsync(client);
        var resp = await client.GetAsync("/api/cashier/ticket/verify?qr=abc123");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── 3. Verify — неіснуючий QR → 404 ────────────────────────────────────

    [Fact]
    public async Task Verify_Returns404_WhenQrNotFound()
    {
        await LoginAsCashierAsync(_client);
        var resp = await _client.GetAsync("/api/cashier/ticket/verify?qr=nonexistent_qr_token_12345");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── 4. OfflineSale — успішний продаж ────────────────────────────────────

    [Fact]
    public async Task OfflineSale_Returns200_WithValidRequest()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();

        var result = await CreateOfflineSaleAsync(showtimeId, row: 1, col: 1);

        result.TotalAmount.Should().BeGreaterThan(0);
        result.TicketIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task OfflineSale_Returns400_WhenEmailMissing()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();

        var resp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row = 7, col = 7 } },
            guestEmail = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OfflineSale_Returns400_WhenSeatsAreEmpty()
    {
        await LoginAsCashierAsync(_client);

        var resp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId = GetFirstShowtimeId(),
            seats = Array.Empty<object>(),
            guestEmail = "empty-seats@example.com"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OfflineSale_Returns400_WhenSeatIsOutOfBounds()
    {
        await LoginAsCashierAsync(_client);

        var resp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId = GetFirstShowtimeId(),
            seats = new[] { new { row = 999, col = 999 } },
            guestEmail = "invalid-seat@example.com"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OfflineSale_WithRegisteredUserEmail_AppearsInUserAccount()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();
        var sentBefore = factory.EmailSender.Sent.Count;

        var saleResp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row = 6, col = 6 } },
            guestEmail = "client@cinema.local"
        });
        saleResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var sale = await saleResp.Content.ReadFromJsonAsync<OfflineSaleResultDto>();
        sale.Should().NotBeNull();

        var client = factory.CreateClient();
        await LoginAsClientAsync(client);

        var ticketsResp = await client.GetAsync("/api/account/tickets");
        ticketsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await ticketsResp.Content.ReadFromJsonAsync<IReadOnlyList<TicketSummaryDto>>();

        tickets.Should().NotBeNull();
        tickets!.Select(t => t.Id).Should().Contain(sale!.TicketIds[0]);
        factory.EmailSender.Sent
            .Skip(sentBefore)
            .Should()
            .Contain(e => e.To == "client@cinema.local"
                && e.Attachments == sale.TicketIds.Count
                && e.FileNames.All(n => n.EndsWith(".pdf"))
                && e.MimeTypes.All(m => m == "application/pdf"));
    }

    // ─── 5. OfflineSale — повторний продаж того ж місця → 409 ───────────────

    [Fact]
    public async Task OfflineSale_Returns409_WhenSeatTaken()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();

        // Перший продаж — повинен пройти
        var firstResp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row = 2, col = 2 } },
            guestEmail = "seat-taken-1@example.com"
        });
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Другий продаж того ж місця — конфлікт
        var secondResp = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row = 2, col = 2 } },
            guestEmail = "seat-taken-2@example.com"
        });
        secondResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─── 6. UseTicket — квиток позначається як використаний ─────────────────

    [Fact]
    public async Task UseTicket_MarksTicketAsUsed()
    {
        await LoginAsCashierAsync(_client);
        var ticketId = await AddPaidTicketForShowtimeStartingInAsync(minutesFromNow: 10);

        var useResp = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/use", new { });
        useResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await useResp.Content.ReadFromJsonAsync<VerifyTicketResultDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Used");
    }

    // ─── 7. UseTicket — повторне використання → 409 ──────────────────────────

    [Fact]
    public async Task UseTicket_Returns409_WhenAlreadyUsed()
    {
        await LoginAsCashierAsync(_client);
        var ticketId = await AddPaidTicketForShowtimeStartingInAsync(minutesFromNow: 10);

        // Перше використання
        var first = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/use", new { });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Друге використання — конфлікт
        var second = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/use", new { });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UseTicket_AfterCheckInWindow_Returns409AndMarksNotUsed()
    {
        await LoginAsCashierAsync(_client);
        var ticketId = await AddPaidTicketForEndedShowtimeAsync();

        var useResp = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/use", new { });
        useResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == ticketId);
        ticket.Status.Should().Be(TicketStatus.NotUsed);
    }

    [Fact]
    public async Task UseTicket_BeforeCheckInWindow_Returns409AndKeepsPaid()
    {
        await LoginAsCashierAsync(_client);
        var ticketId = await AddPaidTicketForShowtimeStartingInAsync(minutesFromNow: 21);

        var useResp = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/use", new { });
        useResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == ticketId);
        ticket.Status.Should().Be(TicketStatus.Paid);
    }

    // ─── 8. Refund — квиток повертається ─────────────────────────────────────

    [Fact]
    public async Task Refund_Returns200_WhenTicketIsPaid()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();

        var sale = await CreateOfflineSaleAsync(showtimeId, row: 5, col: 5);
        var ticketId = sale.TicketIds[0];

        var refundResp = await _client.PostAsJsonAsync($"/api/cashier/tickets/{ticketId}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await refundResp.Content.ReadFromJsonAsync<RefundResultDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Refunded");
        result.RefundedAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OfflineSale_AllowsResaleAfterRefund()
    {
        await LoginAsCashierAsync(_client);
        var showtimeId = GetFirstShowtimeId();
        var sale = await CreateOfflineSaleAsync(showtimeId, row: 5, col: 4);

        var refundResp = await _client.PostAsJsonAsync($"/api/cashier/tickets/{sale.TicketIds[0]}/refund", new { });
        refundResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var resale = await _client.PostAsJsonAsync("/api/cashier/offline-sale", new
        {
            showtimeId,
            seats = new[] { new { row = 5, col = 4 } },
            guestEmail = "resale-after-refund@example.com"
        });

        resale.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── 9. Refund — неіснуючий квиток → 404 ────────────────────────────────

    [Fact]
    public async Task Refund_Returns404_WhenTicketNotFound()
    {
        await LoginAsCashierAsync(_client);

        var resp = await _client.PostAsJsonAsync("/api/cashier/tickets/999999/refund", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<int> AddPaidTicketForEndedShowtimeAsync()
        => await AddPaidTicketForShowtimeStartingInAsync(minutesFromNow: null);

    private async Task<int> AddPaidTicketForShowtimeStartingInAsync(int? minutesFromNow)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var movie = await db.Movies.OrderBy(m => m.Id).FirstAsync();
        var hall = await db.Halls.OrderBy(h => h.Id).FirstAsync();
        var startUtc = minutesFromNow is int minutes
            ? DateTime.UtcNow.AddMinutes(minutes)
            : DateTime.UtcNow.AddMinutes(-(movie.DurationMinutes + 2));

        var showtime = new Showtime(movie.Id, hall.Id, DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), MovieFormat.TwoD, 150m);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();

        var ticket = new Ticket(showtime.Id, row: 1, col: 1, SeatTypeCode.Standard, price: 150m, qrToken: Guid.NewGuid().ToString("N"));
        ticket.SetFinalAmount(150m);
        ticket.MarkPaid();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return ticket.Id;
    }
}
