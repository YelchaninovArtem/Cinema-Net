using Cinema.Application.Email;
using Cinema.Application.Loyalty;
using Cinema.Application.Payments;
using Cinema.Application.QrCode;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Email;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly CinemaDbContext _db;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IEmailSender _email;
    private readonly IQrCodeGenerator _qr;
    private readonly ILoyaltyService _loyalty;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        CinemaDbContext db,
        IEnumerable<IPaymentProvider> providers,
        IEmailSender email,
        IQrCodeGenerator qr,
        ILoyaltyService loyalty,
        ILogger<PaymentService> logger)
    {
        _db        = db;
        _providers = providers;
        _email     = email;
        _qr        = qr;
        _loyalty   = loyalty;
        _logger    = logger;
    }

    public async Task<CreateIntentResponse> CreateIntentAsync(
        int paymentId, string providerName, string returnUrl, CancellationToken ct = default)
    {
        var payment = await _db.Payments
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be paid.");

        var provider = Resolve(providerName);

        var tickets = payment.TicketLinks.Select(tl => tl.Ticket).ToList();

        // Sanity check: amount should equal sum of ticket final amounts.
        var expectedAmount = tickets.Sum(t => t.FinalAmount);
        if (payment.Amount != expectedAmount)
            throw new InvalidOperationException($"Payment amount mismatch.");

        var result = await provider.CreateIntentAsync(tickets, payment.Amount, returnUrl, ct);

        payment.SetProviderAndExternalId(provider.ProviderType, result.ExternalId);
        // Note: payment status remains Pending until webhook

        await _db.SaveChangesAsync(ct);

        return new CreateIntentResponse(result.ClientSecret, result.ApprovalUrl, result.ExternalId);
    }

    public async Task HandleWebhookAsync(
        string providerName,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        var provider = Resolve(providerName);
        var wh = await provider.HandleWebhookAsync(payload, headers, ct);

        if (!wh.IsValid || !wh.IsCompleted || wh.ExternalId is null) return;

        var payment = await _db.Payments
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(p => p.ExternalId == wh.ExternalId, ct);

        if (payment is null) return;

        // Idempotent: if already completed, ignore
        if (payment.Status == PaymentStatus.Completed) return;

        await FinalizeAsync(payment, ct);
    }

    public async Task ConfirmWithGooglePayAsync(
        int paymentId, string googlePayToken, CancellationToken ct = default)
    {
        var payment = await _db.Payments
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Only pending payments can be paid.");

        var stripe = _providers.OfType<StripeProvider>().FirstOrDefault()
            ?? throw new InvalidOperationException("Stripe provider not configured.");

        var tickets = payment.TicketLinks.Select(tl => tl.Ticket).ToList();
        var intentId = await stripe.CreateAndConfirmWithTokenAsync(tickets, payment.Amount, googlePayToken, ct);

        payment.SetProviderAndExternalId(PaymentProvider.Stripe, intentId);
        await FinalizeAsync(payment, ct);
    }

    public async Task<bool> CapturePayPalAsync(string orderId, CancellationToken ct = default)
    {
        var paypal = _providers.OfType<PayPalProvider>().FirstOrDefault()
            ?? throw new InvalidOperationException("PayPal provider not configured.");

        var captured = await paypal.CaptureAsync(orderId, ct);
        if (!captured) return false;

        var payment = await _db.Payments
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(p => p.ExternalId == orderId, ct);

        if (payment is null) return false;

        await FinalizeAsync(payment, ct);
        return true;
    }

    public async Task ConfirmStripeClientAsync(
        int paymentId, string paymentIntentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(p => p.TicketLinks).ThenInclude(tl => tl.Ticket).ThenInclude(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status == PaymentStatus.Completed) return;
        if (payment.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Payment is not in pending state.");

        if (payment.Provider != PaymentProvider.Stripe || payment.ExternalId != paymentIntentId)
            throw new InvalidOperationException("Stripe payment intent does not match this payment.");

        var stripe = _providers.OfType<StripeProvider>().FirstOrDefault()
            ?? throw new InvalidOperationException("Stripe provider not configured.");
        await stripe.EnsureSucceededAsync(paymentIntentId, payment.Amount, ct);

        await FinalizeAsync(payment, ct);
    }

    // ── Shared finalization logic ────────────────────────────────────────────

    private async Task FinalizeAsync(Payment payment, CancellationToken ct)
    {
        payment.MarkCompleted();
        foreach (var ticket in payment.TicketLinks.Select(tl => tl.Ticket))
        {
            ticket.MarkPaid();
        }

        await _db.SaveChangesAsync(ct);

        // Award loyalty points for each ticket
        foreach (var ticket in payment.TicketLinks.Select(tl => tl.Ticket))
        {
            var userId = ticket.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var points = (int)Math.Floor(ticket.FinalAmount / 10m);
                    if (points > 0)
                        await _loyalty.EarnAsync(userId, ticket.Id, ticket.FinalAmount, ct);
                }
                catch { /* log but continue */ }
            }
        }

        // Send one confirmation email per recipient with all their QR codes as attachments
        var ticketsByRecipient = new Dictionary<string, List<Ticket>>();
        foreach (var ticket in payment.TicketLinks.Select(tl => tl.Ticket))
        {
            string? to;
            if (!string.IsNullOrEmpty(ticket.UserId))
                to = await _db.Users.Where(u => u.Id == ticket.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct);
            else
                to = ticket.GuestEmail;

            if (string.IsNullOrWhiteSpace(to)) continue;

            if (!ticketsByRecipient.TryGetValue(to, out var list))
                ticketsByRecipient[to] = list = [];
            list.Add(ticket);
        }

        foreach (var (to, tickets) in ticketsByRecipient)
        {
            try
            {
                var attachments = tickets.Select(t => new EmailAttachment(
                    FileName: $"ticket-{t.Id}.pdf",
                    Data: TicketPdfGenerator.Generate(t, _qr.Generate(t.QrToken)),
                    MimeType: "application/pdf")).ToList();
                var html = TicketEmailTemplate.BuildHtml(tickets, paidAtCashDesk: false, t => _qr.Generate(t.QrToken));
                var subject = TicketEmailTemplate.BuildSubject(tickets);
                await _email.SendAsync(to, subject, html, attachments, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send ticket confirmation email to {Recipient}", to);
            }
        }
    }

    private IPaymentProvider Resolve(string name) =>
        _providers.FirstOrDefault(p =>
            p.ProviderType.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown payment provider: '{name}'.");
}
