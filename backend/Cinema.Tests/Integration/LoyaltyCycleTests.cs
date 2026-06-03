using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Payments;
using Cinema.Application.Tickets;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

/// <summary>
/// Integration test for loyalty cycle: buy ticket → earn points → use points on next purchase.
/// </summary>
[Collection("Integration")]
public sealed class LoyaltyCycleTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string StripeWebhookPayload(string piId) => $$"""
        {
          "id": "evt_test_{{piId}}",
          "type": "payment_intent.succeeded",
          "api_version": "2024-06-20",
          "object": "event",
          "data": {
            "object": {
              "id": "{{piId}}",
              "object": "payment_intent",
              "amount": 20000,
              "currency": "uah",
              "status": "succeeded",
              "livemode": false,
              "created": 1700000000,
              "metadata": {}
            }
          }
        }
        """;

    [Fact]
    public async Task FullCycle_Earn_Then_Redeem_Works()
    {
        // 1. Register user
        var email = $"loyalty_{Guid.NewGuid():N}@example.com";
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password  = "Test_1234!",
            firstName = "Loyal",
            lastName  = "User",
        });
        regResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regResp.Content.ReadFromJsonAsync<AuthBody>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var showtime = db.Showtimes.First();

        // 2. Buy ticket (payment will be processed)
        var createTickets = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(2, 2)],
            GuestEmail: null,
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        );

        var createResp = await _client.PostAsJsonAsync("/api/tickets", createTickets);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        var paymentId = createDto!.PaymentId;

        // 3. Create Stripe intent
        var intentResp = await _client.PostAsJsonAsync($"/api/payments/{paymentId}/intent/stripe", new { returnUrl = "http://localhost:4200/return" });
        intentResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var intentDto = await intentResp.Content.ReadFromJsonAsync<CreateIntentResponse>();
        var piId = intentDto!.ExternalId;

        // 4. Simulate Stripe webhook → tickets Paired + loyalty earned
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook/stripe")
        {
            Content = new StringContent(
                StripeWebhookPayload(piId),
                System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Stripe-Signature", "t=0,v1=fake");
        var webhookResp = await _client.SendAsync(req);
        webhookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Check balance > 0
        var balanceResp = await _client.GetFromJsonAsync<BalanceDto>("/api/account/loyalty/balance");
        balanceResp.Should().NotBeNull();
        var earnedBalance = balanceResp!.Balance;
        earnedBalance.Should().BeGreaterThan(0);

        // 6. Buy another ticket, this time using loyalty points
        var createTickets2 = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(2, 3)],
            GuestEmail: null,
            PromoCode:null,
            LoyaltyPointsToRedeem:earnedBalance // use all earned points
        );

        var createResp2 = await _client.PostAsJsonAsync("/api/tickets", createTickets2);
        createResp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto2 = await createResp2.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        var paymentId2 = createDto2!.PaymentId;

        // Create intent for second payment
        var intentResp2 = await _client.PostAsJsonAsync($"/api/payments/{paymentId2}/intent/stripe", new { returnUrl = "http://localhost:4200/return" });
        intentResp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var intentDto2 = await intentResp2.Content.ReadFromJsonAsync<CreateIntentResponse>();
        var piId2 = intentDto2!.ExternalId;

        // Webhook for second payment
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook/stripe")
        {
            Content = new StringContent(
                StripeWebhookPayload(piId2),
                System.Text.Encoding.UTF8, "application/json")
        };
        req2.Headers.Add("Stripe-Signature", "t=0,v1=fake");
        var webhookResp2 = await _client.SendAsync(req2);
        webhookResp2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. After paying ticket 2: redeemed points were consumed, and new points earned from ticket 2.
        // Balance = (earned from ticket1 - redeemed) + earned from ticket2 = 0 + earnedFromTicket2.
        // The net result: balance decreased by earnedBalance (all redeemed points were spent).
        var finalBalance = await _client.GetFromJsonAsync<BalanceDto>("/api/account/loyalty/balance");
        finalBalance!.TotalEarned.Should().BeGreaterThan(earnedBalance); // earned from both tickets
        finalBalance.Balance.Should().BeLessThan(finalBalance.TotalEarned); // redeemed some
    }

    [Fact]
    public async Task GetBalance_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.GetAsync("/api/account/loyalty/balance");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalance_NoTransactions_ReturnsZero()
    {
        var email = $"zero_{Guid.NewGuid():N}@example.com";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test_1234!",
            firstName = "Zero",
            lastName = "Balance",
        });
        reg.EnsureSuccessStatusCode();
        var auth = await reg.Content.ReadFromJsonAsync<AuthBody>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var balance = await _client.GetFromJsonAsync<BalanceDto>("/api/account/loyalty/balance");
        balance!.Balance.Should().Be(0);
        balance.TotalEarned.Should().Be(0);
    }

    private sealed record AuthBody(string AccessToken, string RefreshToken, string Role, string Email);
    private sealed record BalanceDto(int Balance, int TotalEarned);
}
