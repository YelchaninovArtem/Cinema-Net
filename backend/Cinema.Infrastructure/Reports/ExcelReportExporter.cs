using Cinema.Application.Reports;
using ClosedXML.Excel;

namespace Cinema.Infrastructure.Reports;

public sealed class ExcelReportExporter : IReportExporter
{
    public byte[] ExportSalesXlsx(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales");

        ws.Cell(1, 1).Value = $"Sales Report: {from:yyyy-MM-dd} – {to:yyyy-MM-dd}";
        ws.Range(1, 1, 1, 3).Merge().Style.Font.Bold = true;

        ws.Cell(2, 1).Value = "Date";
        ws.Cell(2, 2).Value = "Bookings";
        ws.Cell(2, 3).Value = "Revenue (UAH)";
        var headerRow = ws.Range(2, 1, 2, 3);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        for (var i = 0; i < data.Count; i++)
        {
            ws.Cell(i + 3, 1).Value = data[i].Date;
            ws.Cell(i + 3, 2).Value = data[i].TotalBookings;
            ws.Cell(i + 3, 3).Value = data[i].TotalRevenue;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportOccupancyXlsx(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Occupancy");

        ws.Cell(1, 1).Value = $"Occupancy Report: {from:yyyy-MM-dd} – {to:yyyy-MM-dd}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;

        ws.Cell(2, 1).Value = "Hall";
        ws.Cell(2, 2).Value = "Movie";
        ws.Cell(2, 3).Value = "Date";
        ws.Cell(2, 4).Value = "Occupied";
        ws.Cell(2, 5).Value = "Total";
        ws.Cell(2, 6).Value = "Occupancy %";
        var headerRow = ws.Range(2, 1, 2, 6);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        for (var i = 0; i < data.Count; i++)
        {
            ws.Cell(i + 3, 1).Value = data[i].HallName;
            ws.Cell(i + 3, 2).Value = data[i].MovieTitle;
            ws.Cell(i + 3, 3).Value = data[i].Date;
            ws.Cell(i + 3, 4).Value = data[i].OccupiedSeats;
            ws.Cell(i + 3, 5).Value = data[i].TotalSeats;
            ws.Cell(i + 3, 6).Value = data[i].OccupancyPercent;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportSalesPdf(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to)
        => throw new NotSupportedException("Use PdfReportExporter for PDF.");

    public byte[] ExportOccupancyPdf(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to)
        => throw new NotSupportedException("Use PdfReportExporter for PDF.");
}
