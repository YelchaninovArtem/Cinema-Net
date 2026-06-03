using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Reviews;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

/// <summary>
/// Integration tests for reviews:
/// - can only review after using a ticket for a finished showtime
/// - duplicate reviews forbidden
/// - submitted reviews are public immediately
/// - public GET returns all reviews
/// </summary>
[Collection("Integration")]
public sealed class ReviewFlowTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<(string Token, string UserId)> RegisterAsync(string prefix = "rv")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@example.com";
        var reg   = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Test_1234!", firstName = "Test", lastName = "User",
        });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<AuthBody>();
        // decode userId from JWT
        var payload = System.Text.Json.JsonDocument.Parse(
            System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(
                    body!.AccessToken.Split('.')[1]
                    .PadRight(body.AccessToken.Split('.')[1].Length + (4 - body.AccessToken.Split('.')[1].Length % 4) % 4, '='))));
        var userId = payload.RootElement.GetProperty("sub").GetString()!;
        return (body.AccessToken, userId);
    }

    private void SetBearer(string token) =>
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Submit_WithoutEligibleTicket_ReturnsBadRequest()
    {
        var (token, _) = await RegisterAsync("rv_noticket");
        SetBearer(token);

        var resp = await _client.PostAsJsonAsync("/api/reviews",
            new { movieId = 1, rating = 8, comment = "Great film!" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_WithOnlyPaidTicket_ReturnsBadRequest()
    {
        var (token, userId) = await RegisterAsync("rv_paid");
        SetBearer(token);

        var movieId = await AddTicketAsync(userId, used: false, showtimeEndedMoreThanMinuteAgo: true);

        var revResp = await _client.PostAsJsonAsync("/api/reviews",
            new { movieId, rating = 9, comment = "Excellent!" });
        revResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_WithUsedTicketBeforeReviewWindow_ReturnsBadRequest()
    {
        var (token, userId) = await RegisterAsync("rv_too_early");
        SetBearer(token);

        var movieId = await AddTicketAsync(userId, used: true, showtimeEndedMoreThanMinuteAgo: false);

        var revResp = await _client.PostAsJsonAsync("/api/reviews",
            new { movieId, rating = 9, comment = "Excellent!" });
        revResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_WithUsedTicketAfterShowtimeEnd_Returns200AndVisibleReview()
    {
        var (token, userId) = await RegisterAsync("rv_ok");
        SetBearer(token);

        var movieId = await AddTicketAsync(userId, used: true, showtimeEndedMoreThanMinuteAgo: true);

        // Now submit review
        var revResp = await _client.PostAsJsonAsync("/api/reviews",
            new { movieId, rating = 9, comment = "Excellent!" });
        revResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var rev = await revResp.Content.ReadFromJsonAsync<ReviewDtoBody>();
        rev!.IsApproved.Should().BeTrue();
        rev.Rating.Should().Be(9);
    }

    [Fact]
    public async Task Submit_Duplicate_Returns400()
    {
        var (token, userId) = await RegisterAsync("rv_dup");
        SetBearer(token);

        var movieId = await AddTicketAsync(userId, used: true, showtimeEndedMoreThanMinuteAgo: true);

        // First review — OK
        await _client.PostAsJsonAsync("/api/reviews",
            new { movieId, rating = 7, comment = "Good" });

        // Second review — 400
        var dup = await _client.PostAsJsonAsync("/api/reviews",
            new { movieId, rating = 8, comment = "Also good" });
        dup.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetForMovie_ReturnsAllReviews()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var movieId = db.Movies.First().Id;

        // Add two reviews directly
        var first = new Cinema.Domain.Entities.Review("sys_user_1", movieId, 8, "First review");
        var second = new Cinema.Domain.Entities.Review("sys_user_2", movieId, 5, "Second review");

        db.Reviews.AddRange(first, second);
        await db.SaveChangesAsync();

        // GET without auth
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.GetFromJsonAsync<MovieReviewsBody>($"/api/reviews/movies/{movieId}");
        resp.Should().NotBeNull();
        resp!.Reviews.Select(r => r.Comment).Should().Contain("First review")
            .And.Contain("Second review");
        resp.TotalReviews.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CanReview_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.GetAsync("/api/reviews/movies/1/can-review");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AuthBody(string AccessToken, string RefreshToken, string Role, string Email);
    private sealed record MovieReviewsBody(IReadOnlyList<ReviewDto> Reviews, double? AverageRating, int TotalReviews);
    private sealed record ReviewDtoBody(int Id, string UserId, string UserName, int Rating, string Comment, bool IsApproved, DateTime CreatedUtc);

    private async Task<int> AddTicketAsync(
        string userId,
        bool used,
        bool showtimeEndedMoreThanMinuteAgo)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var movie = await db.Movies.OrderBy(m => m.Id).FirstAsync();
        var hall = await db.Halls.OrderBy(h => h.Id).FirstAsync();
        var startUtc = showtimeEndedMoreThanMinuteAgo
            ? DateTime.UtcNow.AddMinutes(-(movie.DurationMinutes + 2))
            : DateTime.UtcNow.AddMinutes(-movie.DurationMinutes);

        var showtime = new Showtime(movie.Id, hall.Id, DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), MovieFormat.TwoD, 150m);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();

        var ticket = new Ticket(showtime.Id, row: 1, col: 1, SeatTypeCode.Standard, price: 150m, qrToken: Guid.NewGuid().ToString("N"));
        ticket.SetPurchaser(userId, null);
        ticket.SetFinalAmount(150m);
        ticket.MarkPaid();
        if (used)
            ticket.MarkUsed();

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        return movie.Id;
    }
}
