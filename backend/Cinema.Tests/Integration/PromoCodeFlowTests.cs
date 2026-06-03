using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Tickets;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

/// <summary>
/// Інтеграційний тест застосування промокоду при instant-buy:
/// реєстрація → купівля квитка з промокодом → перевірка знижки.
/// </summary>
[Collection("Integration")]
public sealed class PromoCodeFlowTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task ApplyPromo_PercentDiscount_ReducesTotalAmount()
    {
        // 1. Реєструємо користувача
        var email   = $"promo_{Guid.NewGuid():N}@example.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password  = "Test_1234!",
            firstName = "Promo",
            lastName  = "Tester",
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regResp.Content.ReadFromJsonAsync<PromoAuthBody>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        // 2. Отримуємо сеанс і базову ціну
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var showtime = db.Showtimes.First();
        var basePrice = showtime.BasePrice;

        // 3. Додаємо промокод у БД
        var promoCode = $"TEST10_{Guid.NewGuid():N}"[..12];
        var promo = new PromoCode(
            promoCode, DiscountType.Percent, 10,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            usageLimit: 0, perUserLimit: 10);
        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync();

        // 4. Купуємо квиток з промокодом
        var req = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(1, 2)],
            GuestEmail: null,
            PromoCode: promoCode,
            LoyaltyPointsToRedeem: null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        body.Should().NotBeNull();

        // Знижка 10% від базової ціни
        var expectedDiscount = Math.Round(basePrice * 0.10m, 2);
        var expectedTotal    = Math.Round(basePrice - expectedDiscount, 2);
        body!.TotalAmount.Should().BeApproximately(expectedTotal, 0.02m);
    }

    [Fact]
    public async Task ApplyPromo_InvalidCode_ReturnsBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var showtime = db.Showtimes.First();

        var req = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(3, 2)],
            GuestEmail: $"guest_{Guid.NewGuid():N}@test.com",
            PromoCode: "DOESNOTEXIST",
            LoyaltyPointsToRedeem: null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApplyPromo_ExpiredCode_ReturnsBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var showtime = db.Showtimes.First();

        var expiredCode = $"EXP_{Guid.NewGuid():N}"[..10];
        var expired = new PromoCode(
            expiredCode, DiscountType.Fixed, 50,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            0, 1);
        db.PromoCodes.Add(expired);
        await db.SaveChangesAsync();

        var req = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(4, 2)],
            GuestEmail: $"guest_{Guid.NewGuid():N}@test.com",
            PromoCode: expiredCode,
            LoyaltyPointsToRedeem: null);

        var resp = await _client.PostAsJsonAsync("/api/tickets", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record PromoAuthBody(string AccessToken, string RefreshToken, string Role, string Email);
}
