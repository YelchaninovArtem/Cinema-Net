using Cinema.Application.Reports;
using Cinema.Infrastructure.Reports;
using ClosedXML.Excel;
using FluentAssertions;
using QuestPDF.Infrastructure;
using Xunit;

namespace Cinema.Tests.Unit.Reports;

public sealed class ExcelReportExporterTests
{
    private static readonly ExcelReportExporter _sut = new();
    private static readonly DateTime _from = new(2024, 1, 1);
    private static readonly DateTime _to   = new(2024, 1, 31);

    [Fact]
    public void ExportSalesXlsx_ReturnsNonEmptyBytes()
    {
        var data = new List<SalesReportItem>
        {
            new("2024-01-05", 10, 2500.00m),
            new("2024-01-12", 5,  1000.00m),
        };

        var bytes = _sut.ExportSalesXlsx(data, _from, _to);

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void ExportSalesXlsx_ContainsCorrectData()
    {
        var data = new List<SalesReportItem>
        {
            new("2024-01-05", 3, 750.00m),
        };

        var bytes = _sut.ExportSalesXlsx(data, _from, _to);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        ws.Cell(3, 1).GetString().Should().Be("2024-01-05");
        ws.Cell(3, 2).GetValue<int>().Should().Be(3);
        ws.Cell(3, 3).GetValue<decimal>().Should().Be(750.00m);
    }

    [Fact]
    public void ExportOccupancyXlsx_ReturnsNonEmptyBytes()
    {
        var data = new List<OccupancyReportItem>
        {
            new("Hall A", "Inception", "2024-01-05", 80, 100, 80.0),
        };

        var bytes = _sut.ExportOccupancyXlsx(data, _from, _to);

        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public void ExportOccupancyXlsx_ContainsCorrectData()
    {
        var data = new List<OccupancyReportItem>
        {
            new("Hall B", "Dune", "2024-01-10", 60, 120, 50.0),
        };

        var bytes = _sut.ExportOccupancyXlsx(data, _from, _to);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        ws.Cell(3, 1).GetString().Should().Be("Hall B");
        ws.Cell(3, 2).GetString().Should().Be("Dune");
        ws.Cell(3, 3).GetString().Should().Be("2024-01-10");
        ws.Cell(3, 4).GetValue<int>().Should().Be(60);
        ws.Cell(3, 5).GetValue<int>().Should().Be(120);
    }
}

public sealed class PdfReportExporterTests
{
    static PdfReportExporterTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static readonly PdfReportExporter _sut = new();
    private static readonly DateTime _from = new(2024, 1, 1);
    private static readonly DateTime _to   = new(2024, 1, 31);

    [Fact]
    public void ExportSalesPdf_ReturnsValidPdfBytes()
    {
        var data = new List<SalesReportItem>
        {
            new("2024-01-05", 10, 2500.00m),
            new("2024-01-12", 5,  1000.00m),
        };

        var bytes = _sut.ExportSalesPdf(data, _from, _to);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void ExportOccupancyPdf_ReturnsValidPdfBytes()
    {
        var data = new List<OccupancyReportItem>
        {
            new("Hall A", "Inception", "2024-01-05", 80, 100, 80.0),
        };

        var bytes = _sut.ExportOccupancyPdf(data, _from, _to);

        bytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
