using System.Globalization;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Queries;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cinema.Infrastructure.Email;

internal static class TicketPdfGenerator
{
    private static readonly CultureInfo UkrainianCulture = CultureInfo.GetCultureInfo("uk-UA");

    public static byte[] Generate(Ticket ticket, byte[] qrCode)
    {
        var showtime = ticket.Showtime;
        var movie = showtime.Movie;
        var hall = showtime.Hall;
        var cinema = hall.CinemaBranch;
        var start = ToCinemaLocalTime(showtime.StartUtc, cinema.TimezoneId);
        var end = start.AddMinutes(movie.DurationMinutes);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Background(Colors.Grey.Darken4)
                    .Padding(22)
                    .Column(column =>
                    {
                        column.Spacing(8);
                        column.Item().Text("КВИТОК ПІДТВЕРДЖЕНО")
                            .FontSize(10)
                            .FontColor(Colors.Green.Lighten2)
                            .SemiBold();
                        column.Item().Text(movie.Title)
                            .FontSize(28)
                            .FontColor(Colors.White)
                            .Bold();
                    });

                page.Content()
                    .PaddingTop(22)
                    .Column(column =>
                    {
                        column.Spacing(18);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                            });

                            AddInfoRow(table, "Фільм", movie.Title);
                            AddInfoRow(table, "Дата", start.ToString("dd MMMM yyyy", UkrainianCulture));
                            AddInfoRow(table, "Час сеансу", $"{start:HH:mm} - {end:HH:mm}");
                            AddInfoRow(table, "Кінотеатр", cinema.Name);
                            AddInfoRow(table, "Місто", cinema.City);
                            AddInfoRow(table, "Адреса", cinema.Address);
                            AddInfoRow(table, "Зала", hall.Name);
                            AddInfoRow(table, "Формат", MovieQueryService.FormatToString(showtime.Format));
                        });

                        column.Item()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(16)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(details =>
                                {
                                    details.Spacing(8);
                                    details.Item().Text($"Квиток #{ticket.Id}")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken4);
                                    details.Item().Text($"Ряд {ticket.Row}, місце {ticket.Col}")
                                        .FontSize(14)
                                        .SemiBold();
                                    details.Item().Text(SeatType(ticket))
                                        .FontColor(Colors.Grey.Darken2);
                                    details.Item().Text($"Вартість: {Money(ticket.FinalAmount)}")
                                        .FontSize(14)
                                        .Bold();
                                });

                                row.ConstantItem(190)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(10)
                                    .Image(qrCode)
                                    .FitArea();
                            });

                        column.Item()
                            .PaddingTop(10)
                            .Background(Colors.Grey.Lighten4)
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(12)
                            .Text("Цей документ є підставою для відвідування сеансу без звернення до каси")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken4)
                            .AlignCenter();
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Cinema Network")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    private static void AddInfoRow(TableDescriptor table, string label, string value)
    {
        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(7)
            .Text(label)
            .FontColor(Colors.Grey.Darken1);

        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(7)
            .Text(value)
            .FontColor(Colors.Grey.Darken4)
            .SemiBold();
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
        Domain.Enums.SeatTypeCode.Love => "Розкладне місце",
        _ => "Класичне місце"
    };

    private static string Money(decimal amount) => $"{amount:0.00} грн";
}
