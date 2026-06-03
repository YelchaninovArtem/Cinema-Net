using Cinema.Application.Email;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cinema.Infrastructure.Workers;

/// <summary>
/// Фоновий сервіс: раз на годину шукає оплачені квитки на сеанси,
/// що почнуться через 20-24 години, і надсилає нагадування на email.
/// </summary>
public sealed class ShowtimeReminderWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShowtimeReminderWorker> _logger;

    public ShowtimeReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ShowtimeReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SendRemindersAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    // Публічний для тестування
    public async Task<int> SendRemindersAsync(CancellationToken ct = default)
    {
        using var scope  = _scopeFactory.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var emailSender  = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now      = DateTime.UtcNow;
        var from     = now.AddHours(20);
        var to       = now.AddHours(24);

        // Вибираємо оплачені квитки на сеанси у вікні [+20h, +24h]
        // та які ще не отримали нагадування (ReminderSentUtc is null)
        var tickets = await db.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Showtime).ThenInclude(s => s.Hall).ThenInclude(h => h.CinemaBranch)
            .Where(t =>
                t.Status == TicketStatus.Paid &&
                !t.ReminderSentUtc.HasValue &&
                t.Showtime.StartUtc >= from &&
                t.Showtime.StartUtc <= to)
            .ToListAsync(ct);

        if (tickets.Count == 0) return 0;

        var sent = 0;
        foreach (var ticket in tickets)
        {
            var email = !string.IsNullOrEmpty(ticket.UserId)
                ? await db.Users.Where(u => u.Id == ticket.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct)
                : ticket.GuestEmail;

            if (string.IsNullOrWhiteSpace(email))
            {
                ticket.MarkReminderSent();
                continue;
            }

            try
            {
                var movie    = ticket.Showtime.Movie;
                var hall     = ticket.Showtime.Hall;
                var cinema   = hall.CinemaBranch;
                var startUtc = ticket.Showtime.StartUtc;

                var subject  = $"Нагадування: сеанс \"{movie.Title}\" завтра";
                var html     = BuildReminderHtml(
                    movie.Title, cinema.Name, cinema.Address,
                    hall.Name, startUtc, 1); // one ticket per email

                await emailSender.SendAsync(email, subject, html, ct: ct);
                ticket.MarkReminderSent();
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Не вдалося надіслати нагадування для квитка {TicketId}", ticket.Id);
            }
        }

        if (sent > 0)
            await db.SaveChangesAsync(ct);

        return sent;
    }

    private static string BuildReminderHtml(
        string movieTitle, string cinemaName, string cinemaAddress,
        string hallName, DateTime startUtc, int seatCount)
    {
        var time = startUtc.ToString("dd MMM yyyy, HH:mm") + " UTC";
        return $"""
            <div style="font-family: Arial, sans-serif; max-width: 560px; margin: auto;
                        background: #0d1422; color: #e2e8f0; border-radius: 12px; padding: 32px;">
              <h2 style="color: #d4a853; margin: 0 0 16px;">🎬 Нагадування про сеанс</h2>
              <p>Завтра вас чекає:</p>
              <table style="width:100%; border-collapse: collapse; margin: 16px 0;">
                <tr><td style="padding: 8px; color:#94a3b8;">Фільм</td>
                    <td style="padding: 8px; font-weight:700;">{movieTitle}</td></tr>
                <tr><td style="padding: 8px; color:#94a3b8;">Кінотеатр</td>
                    <td style="padding: 8px;">{cinemaName}, {cinemaAddress}</td></tr>
                <tr><td style="padding: 8px; color:#94a3b8;">Зал</td>
                    <td style="padding: 8px;">{hallName}</td></tr>
                <tr><td style="padding: 8px; color:#94a3b8;">Час початку</td>
                    <td style="padding: 8px;">{time}</td></tr>
                <tr><td style="padding: 8px; color:#94a3b8;">Кількість квитків</td>
                    <td style="padding: 8px;">{seatCount}</td></tr>
              </table>
              <p style="color:#64748b; font-size:12px;">QR-коди квитків можна знайти у своєму листі з підтвердженням.</p>
            </div>
            """;
    }
}