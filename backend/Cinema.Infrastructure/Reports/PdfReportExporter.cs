using Cinema.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cinema.Infrastructure.Reports;

public sealed class PdfReportExporter : IReportExporter
{
    public byte[] ExportSalesPdf(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text($"Sales Report: {from:yyyy-MM-dd} – {to:yyyy-MM-dd}")
                    .Bold().FontSize(14).AlignCenter();

                page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                    });

                    foreach (var header in new[] { "Date", "Bookings", "Revenue (UAH)" })
                    {
                        table.Cell().Background(Colors.Blue.Lighten3)
                            .Padding(5).Text(header).Bold();
                    }

                    foreach (var row in data)
                    {
                        table.Cell().Padding(5).Text(row.Date);
                        table.Cell().Padding(5).Text(row.TotalBookings.ToString());
                        table.Cell().Padding(5).Text(row.TotalRevenue.ToString("N2"));
                    }
                });

                page.Footer().AlignCenter()
                    .Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); });
            });
        }).GeneratePdf();
    }

    public byte[] ExportOccupancyPdf(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text($"Occupancy Report: {from:yyyy-MM-dd} – {to:yyyy-MM-dd}")
                    .Bold().FontSize(14).AlignCenter();

                page.Content().PaddingVertical(0.5f, Unit.Centimetre).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(1);
                    });

                    foreach (var header in new[] { "Hall", "Movie", "Date", "Occupied", "Total", "%" })
                    {
                        table.Cell().Background(Colors.Blue.Lighten3)
                            .Padding(5).Text(header).Bold();
                    }

                    foreach (var row in data)
                    {
                        table.Cell().Padding(5).Text(row.HallName);
                        table.Cell().Padding(5).Text(row.MovieTitle);
                        table.Cell().Padding(5).Text(row.Date);
                        table.Cell().Padding(5).Text(row.OccupiedSeats.ToString());
                        table.Cell().Padding(5).Text(row.TotalSeats.ToString());
                        table.Cell().Padding(5).Text($"{row.OccupancyPercent:0.#}%");
                    }
                });

                page.Footer().AlignCenter()
                    .Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); });
            });
        }).GeneratePdf();
    }

    public byte[] ExportSalesXlsx(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to)
        => throw new NotSupportedException("Use ExcelReportExporter for XLSX.");

    public byte[] ExportOccupancyXlsx(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to)
        => throw new NotSupportedException("Use ExcelReportExporter for XLSX.");
}
