using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Payments;
using Cinema.Application.Tickets;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

/// <summary>
/// Verifies full flow: create tickets → create intent → webhook → Tickets.Paid.
/// Stripe API is stubbed; uses NoVerificationStripeWebhookVerifier.
/// </summary>
[Collection("Integration")]
public sealed class PaymentFlowTests(CinemaWebApplicationFactory factory)
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
    public async Task StripeWebhook_MarksTicketsAsPaid()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var showtime = db.Showtimes.First();

        // 1. Create tickets (pending payment)
        var createTickets = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(1, 1)],
            GuestEmail: "pay_test@example.com",
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        );

        var createResp = await _client.PostAsJsonAsync("/api/tickets", createTickets);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        createDto.Should().NotBeNull();
        var paymentId = createDto!.PaymentId;
        var ticketIds = createDto.Tickets.Select(t => t.Id).ToArray();

        // 2. Create Stripe intent
        var intentResp = await _client.PostAsJsonAsync($"/api/payments/{paymentId}/intent/stripe", new { returnUrl = "http://localhost:4200/return" });
        intentResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var intentDto = await intentResp.Content.ReadFromJsonAsync<CreateIntentResponse>();
        var piId = intentDto!.ExternalId;

        // 3. Simulate Stripe webhook
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook/stripe")
        {
            Content = new StringContent(
                StripeWebhookPayload(piId),
                System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Stripe-Signature", "t=0,v1=fake");
        var webhookResp = await _client.SendAsync(req);
        webhookResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify tickets are now Paid
        db.ChangeTracker.Clear();
        var tickets = await db.Tickets.Where(t => ticketIds.Contains(t.Id)).ToListAsync();
        tickets.All(t => t.Status == TicketStatus.Paid).Should().BeTrue();
    }

    [Fact]
    public async Task GooglePayConfirm_MarksTicketsAsPaid()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var showtime = db.Showtimes.First();
        var createTickets = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(1, 2)],
            GuestEmail: "google_pay_test@example.com",
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        );

        var createResp = await _client.PostAsJsonAsync("/api/tickets", createTickets);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        createDto.Should().NotBeNull();

        var paymentId = createDto!.PaymentId;
        var ticketIds = createDto.Tickets.Select(t => t.Id).ToArray();

        var confirmResp = await _client.PostAsJsonAsync("/api/payments/stripe/google-pay", new
        {
            paymentId,
            googlePayToken = "tok_google_pay"
        });

        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);

        db.ChangeTracker.Clear();
        var tickets = await db.Tickets.Where(t => ticketIds.Contains(t.Id)).ToListAsync();
        tickets.All(t => t.Status == TicketStatus.Paid).Should().BeTrue();

        var payment = await db.Payments.SingleAsync(p => p.Id == paymentId);
        payment.Provider.Should().Be(PaymentProvider.Stripe);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ExternalId.Should().StartWith("pi_test_");
    }

    [Fact]
    public async Task StripeClientConfirm_VerifiesIntentAndMarksTicketsAsPaid()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var showtime = db.Showtimes.First();

        var createResp = await _client.PostAsJsonAsync("/api/tickets", new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(1, 3)],
            GuestEmail: "stripe_client_confirm@example.com",
            PromoCode: null,
            LoyaltyPointsToRedeem: null));
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);

        var intentResp = await _client.PostAsJsonAsync(
            $"/api/payments/{createDto!.PaymentId}/intent/stripe",
            new { returnUrl = "http://localhost:4200/return" });
        var intent = await intentResp.Content.ReadFromJsonAsync<CreateIntentResponse>();

        var confirmResp = await _client.PostAsJsonAsync(
            $"/api/payments/{createDto.PaymentId}/confirm-stripe",
            new { paymentIntentId = intent!.ExternalId });

        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);
        db.ChangeTracker.Clear();
        var tickets = await db.Tickets.Where(t => createDto.Tickets.Select(x => x.Id).Contains(t.Id)).ToListAsync();
        tickets.Should().OnlyContain(t => t.Status == TicketStatus.Paid);
    }

    [Fact]
    public async Task StripeClientConfirm_Returns400_WhenIntentDoesNotMatchPayment()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var showtime = db.Showtimes.First();

        var createResp = await _client.PostAsJsonAsync("/api/tickets", new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats: [new SeatCoord(1, 4)],
            GuestEmail: "stripe_mismatch@example.com",
            PromoCode: null,
            LoyaltyPointsToRedeem: null));
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        await _client.PostAsJsonAsync(
            $"/api/payments/{createDto!.PaymentId}/intent/stripe",
            new { returnUrl = "http://localhost:4200/return" });

        var confirmResp = await _client.PostAsJsonAsync(
            $"/api/payments/{createDto.PaymentId}/confirm-stripe",
            new { paymentIntentId = "pi_different" });

        confirmResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_UnknownProvider_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/payments/webhook/bitcoin", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record BookingDto(int Id, decimal TotalAmount);
}
