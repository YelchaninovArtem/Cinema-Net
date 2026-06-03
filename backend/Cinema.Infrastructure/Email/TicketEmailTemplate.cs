using System.Globalization;
using System.Net;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Queries;

namespace Cinema.Infrastructure.Email;

internal static class TicketEmailTemplate
{
    private static readonly CultureInfo UkrainianCulture = CultureInfo.GetCultureInfo("uk-UA");

    public static string BuildSubject(IReadOnlyCollection<Ticket> tickets)
    {
        var movieTitles = tickets
            .Select(t => t.Showtime.Movie.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var showtimes = tickets
            .Select(t => t.Showtime)
            .DistinctBy(s => s.Id)
            .ToList();

        if (movieTitles.Count == 1 && showtimes.Count == 1)
        {
            var showtime = showtimes[0];
            var start = ToCinemaLocalTime(showtime.StartUtc, showtime.Hall.CinemaBranch.TimezoneId);
            var ticketWord = tickets.Count == 1 ? "квитка" : "квитків";
            return $"Підтвердження {ticketWord} на фільм \"{movieTitles[0]}\" – {start:dd.MM.yyyy HH:mm}";
        }

        return tickets.Count == 1
            ? "Підтвердження квитка у кіно"
            : "Підтвердження квитків у кіно";
    }

    public static string BuildHtml(
        IReadOnlyCollection<Ticket> tickets,
        bool paidAtCashDesk,
        Func<Ticket, byte[]> qrCodeFactory)
    {
        var first = tickets.First();
        var showtime = first.Showtime;
        var movie = showtime.Movie;
        var hall = showtime.Hall;
        var cinema = hall.CinemaBranch;
        var start = ToCinemaLocalTime(showtime.StartUtc, cinema.TimezoneId);
        var end = start.AddMinutes(movie.DurationMinutes);
        var total = tickets.Sum(t => t.FinalAmount);
        var seats = string.Join("", tickets.Select(t => BuildSeatRow(t, qrCodeFactory)));
        var seatsHeader = tickets.Count > 1
            ? """<div style="background:#f9fafb;padding:12px 16px;font-weight:700;color:#111827;">Ваші місця</div>"""
            : "";
        var confirmationText = tickets.Count == 1
            ? "Квиток підтверджено"
            : "Квитки підтверджено";
        var paymentText = paidAtCashDesk
            ? "Оплату прийнято в касі."
            : "Оплату успішно підтверджено.";
        var ticketDeliveryText = tickets.Count == 1
            ? "QR-код наведений нижче, а PDF-квиток прикріплений до цього листа. Покажіть його касиру при вході."
            : "QR-коди наведені нижче, а PDF-квитки прикріплені до цього листа. Покажіть їх касиру при вході.";

        return $"""
            <!DOCTYPE html>
            <html lang="uk">
            <body style="margin:0;background:#f4f6fb;font-family:Arial,Helvetica,sans-serif;color:#101828;">
              <div style="max-width:680px;margin:0 auto;padding:28px 16px;">
                <div style="background:#111827;border-radius:14px 14px 0 0;padding:24px 28px;color:#ffffff;">
                  <div style="font-size:13px;letter-spacing:.08em;text-transform:uppercase;color:#a7f3d0;">{confirmationText}</div>
                  <h1 style="margin:10px 0 0;font-size:28px;line-height:1.2;color:#f8fafc;">{Html(movie.Title)}</h1>
                </div>
                <div style="background:#ffffff;border:1px solid #e5e7eb;border-top:0;border-radius:0 0 14px 14px;padding:28px;">
                  <p style="margin:0 0 22px;font-size:16px;line-height:1.55;color:#344054;">
                    {paymentText} {ticketDeliveryText}
                  </p>

                  <table role="presentation" style="width:100%;border-collapse:collapse;margin:0 0 24px;">
                    {InfoRow("Фільм", movie.Title)}
                    {InfoRow("Дата", start.ToString("dd MMMM yyyy", UkrainianCulture))}
                    {InfoRow("Час сеансу", $"{start:HH:mm} - {end:HH:mm}")}
                    {InfoRow("Кінотеатр", cinema.Name)}
                    {InfoRow("Місто", cinema.City)}
                    {InfoRow("Адреса", cinema.Address)}
                    {InfoRow("Зала", hall.Name)}
                    {InfoRow("Формат", MovieQueryService.FormatToString(showtime.Format))}
                  </table>

                  <div style="border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;margin-bottom:22px;">
                    {seatsHeader}
                    {seats}
                  </div>

                  <div style="display:flex;justify-content:space-between;gap:16px;align-items:center;padding:16px;border-radius:12px;background:#f3f4f6;">
                    <span style="font-weight:700;color:#344054;">Разом</span>
                    <span style="font-size:20px;font-weight:800;color:#111827;">{Money(total)}</span>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildSeatRow(Ticket ticket, Func<Ticket, byte[]> qrCodeFactory)
    {
        var qrBase64 = Convert.ToBase64String(qrCodeFactory(ticket));
        return $"""
            <div style="display:flex;justify-content:space-between;gap:16px;padding:16px;border-top:1px solid #e5e7eb;">
              <div>
                <div style="font-weight:700;color:#111827;">Квиток #{ticket.Id}</div>
                <div style="font-size:13px;color:#667085;margin-top:3px;">Ряд {ticket.Row}, місце {ticket.Col} · {SeatType(ticket)}</div>
                <div style="font-size:13px;color:#667085;margin-top:3px;">PDF-квиток також є у вкладенні ticket-{ticket.Id}.pdf</div>
              </div>
              <div style="text-align:right;">
                <img src="data:image/png;base64,{qrBase64}" alt="QR-код квитка #{ticket.Id}" width="198" height="198" style="display:block;border:1px solid #e5e7eb;border-radius:10px;padding:8px;background:#ffffff;margin-bottom:8px;">
                <div style="font-weight:700;color:#111827;white-space:nowrap;">{Money(ticket.FinalAmount)}</div>
              </div>
            </div>
            """;
    }

    private static string InfoRow(string label, string value)
    {
        return $"""
            <tr>
              <td style="padding:9px 0;color:#667085;width:140px;border-bottom:1px solid #eef2f7;">{Html(label)}</td>
              <td style="padding:9px 0;color:#111827;font-weight:700;border-bottom:1px solid #eef2f7;">{Html(value)}</td>
            </tr>
            """;
    }

    private static DateTime ToCinemaLocalTime(DateTime utc, string timezoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);
        }
        catch
        {
            return DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        }
    }

    private static string SeatType(Ticket ticket) => ticket.SeatType switch
    {
        Domain.Enums.SeatTypeCode.Vip => "VIP",
        Domain.Enums.SeatTypeCode.Love => "Розкладне",
        _ => "Класичне"
    };

    private static string Money(decimal amount) => $"{amount:0.00} грн";

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
