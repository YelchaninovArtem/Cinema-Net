using Cinema.Api.Controllers;
using Cinema.Application.Reports;
using Cinema.Infrastructure.Reports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuestPDF.Infrastructure;

namespace Cinema.Tests.Unit.Api;

public sealed class ReportsControllerUnitTests
{
    private static readonly DateTime From = new(2026, 1, 1);
    private static readonly DateTime To = new(2026, 1, 31);

    static ReportsControllerUnitTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task GetSalesReport_ReturnsOk()
    {
        IReadOnlyList<SalesReportItem> data = [new("2026-01-01", 2, 300m)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetSalesReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetSalesReport(From, To, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(data);
    }

    [Fact]
    public async Task GetOccupancyReport_ReturnsOk()
    {
        IReadOnlyList<OccupancyReportItem> data = [new("Hall", "Movie", "2026-01-01", 5, 10, 50)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetOccupancyReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetOccupancyReport(From, To, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(data);
    }

    [Fact]
    public async Task GetSalesReportPdf_ReturnsPdfFileWithExpectedName()
    {
        IReadOnlyList<SalesReportItem> data = [new("2026-01-01", 2, 300m)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetSalesReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetSalesReportPdf(From, To, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("sales-report-2026-01-01--2026-01-31.pdf");
        file.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSalesReportXlsx_ReturnsXlsxFileWithExpectedName()
    {
        IReadOnlyList<SalesReportItem> data = [new("2026-01-01", 2, 300m)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetSalesReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetSalesReportXlsx(From, To, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileDownloadName.Should().Be("sales-report-2026-01-01--2026-01-31.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOccupancyReportPdf_ReturnsPdfFileWithExpectedName()
    {
        IReadOnlyList<OccupancyReportItem> data = [new("Hall", "Movie", "2026-01-01", 5, 10, 50)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetOccupancyReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetOccupancyReportPdf(From, To, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().Be("occupancy-report-2026-01-01--2026-01-31.pdf");
        file.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOccupancyReportXlsx_ReturnsXlsxFileWithExpectedName()
    {
        IReadOnlyList<OccupancyReportItem> data = [new("Hall", "Movie", "2026-01-01", 5, 10, 50)];
        var service = new Mock<IReportService>();
        service.Setup(s => s.GetOccupancyReportAsync(From, To, It.IsAny<CancellationToken>())).ReturnsAsync(data);
        var controller = CreateController(service.Object);

        var result = await controller.GetOccupancyReportXlsx(From, To, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileDownloadName.Should().Be("occupancy-report-2026-01-01--2026-01-31.xlsx");
        file.FileContents.Should().NotBeEmpty();
    }

    private static ReportsController CreateController(IReportService service) =>
        new(service, new PdfReportExporter(), new ExcelReportExporter());
}
