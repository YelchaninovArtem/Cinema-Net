using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cinema.Application.Payments;
using Cinema.Application.Tickets;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

/// <summary>
/// End-to-end: guest buys tickets → payment → tickets Paid + email sent with PDF ticket attachments.
/// </summary>
[Collection("Integration")]
public sealed class GuestPurchaseFlowTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string StripeWebhookPayload(string piId) => $$"""
        {
          "id": "evt_guest_{{piId}}",
          "type": "payment_intent.succeeded",
          "api_version": "2024-06-20",
          "object": "event",
          "data": { "object": { "id": "{{piId}}", "object": "payment_intent",
            "amount": 15000, "currency": "uah", "status": "succeeded",
            "livemode": false, "created": 1700000001, "metadata": {} } }
        }
        """;

    [Fact]
    public async Task GuestBuyTickets_AfterPayment_EmailSentWithPdfAttachments()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        // Get a showtime with free seats
        var showtime = db.Showtimes.First();

        const string guestEmail = "guest_flow@example.com";

        // 1. Create tickets (pending payment)
        var createTickets = new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats:[new SeatCoord(2, 2), new SeatCoord(2, 3)],
            GuestEmail:guestEmail,
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        );

        var createResp = await _client.PostAsJsonAsync("/api/tickets", createTickets);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var createDto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);
        createDto.Should().NotBeNull();
        var paymentId = createDto!.PaymentId;
        var ticketIds = createDto.Tickets.Select(t => t.Id).ToArray();

        // 2. Create Stripe payment intent
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

        // 5. Email sent to guest with attachments equal to number of tickets
        var emailSender = factory.EmailSender;
        emailSender.Sent.Should().ContainSingle(e =>
            e.To == guestEmail &&
            e.Attachments == ticketIds.Length &&
            e.FileNames.All(n => n.EndsWith(".pdf")) &&
            e.MimeTypes.All(m => m == "application/pdf"));
    }

    [Fact]
    public async Task GuestBuyTicket_QrTokens_AreNonEmptyGuids()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

        var showtime = db.Showtimes.OrderBy(s => s.Id).Last();

        var createResp = await _client.PostAsJsonAsync("/api/tickets", new CreateTicketsRequest(
            ShowtimeId: showtime.Id,
            Seats:[new SeatCoord(3, 3)],
            GuestEmail:"qr_check@example.com",
            PromoCode:null,
            LoyaltyPointsToRedeem:null
        ));
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await createResp.Content.ReadFromJsonAsync<CreateTicketsResponse>(JsonOpts);

        db.ChangeTracker.Clear();
        var ticket = await db.Tickets.FirstAsync(t => t.Id == dto!.Tickets[0].Id);
        ticket.QrToken.Should().NotBeNullOrEmpty();
        ticket.QrToken.Length.Should().Be(32); // hex GUID without dashes
    }
}
