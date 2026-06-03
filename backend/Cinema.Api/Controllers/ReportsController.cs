using Cinema.Application.Reports;
using Cinema.Infrastructure.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(
    IReportService reports,
    PdfReportExporter pdfExporter,
    ExcelReportExporter excelExporter) : ControllerBase
{
    [HttpGet("sales")]
    [ProducesResponseType(typeof(IReadOnlyList<SalesReportItem>), 200)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var result = await reports.GetSalesReportAsync(startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(IReadOnlyList<OccupancyReportItem>), 200)]
    public async Task<IActionResult> GetOccupancyReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var result = await reports.GetOccupancyReportAsync(startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("sales/pdf")]
    public async Task<IActionResult> GetSalesReportPdf(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var data = await reports.GetSalesReportAsync(startDate, endDate, ct);
        var bytes = pdfExporter.ExportSalesPdf(data, startDate, endDate);
        var filename = $"sales-report-{startDate:yyyy-MM-dd}--{endDate:yyyy-MM-dd}.pdf";
        return File(bytes, "application/pdf", filename);
    }

    [HttpGet("sales/xlsx")]
    public async Task<IActionResult> GetSalesReportXlsx(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var data = await reports.GetSalesReportAsync(startDate, endDate, ct);
        var bytes = excelExporter.ExportSalesXlsx(data, startDate, endDate);
        var filename = $"sales-report-{startDate:yyyy-MM-dd}--{endDate:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    [HttpGet("occupancy/pdf")]
    public async Task<IActionResult> GetOccupancyReportPdf(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var data = await reports.GetOccupancyReportAsync(startDate, endDate, ct);
        var bytes = pdfExporter.ExportOccupancyPdf(data, startDate, endDate);
        var filename = $"occupancy-report-{startDate:yyyy-MM-dd}--{endDate:yyyy-MM-dd}.pdf";
        return File(bytes, "application/pdf", filename);
    }

    [HttpGet("occupancy/xlsx")]
    public async Task<IActionResult> GetOccupancyReportXlsx(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        var data = await reports.GetOccupancyReportAsync(startDate, endDate, ct);
        var bytes = excelExporter.ExportOccupancyXlsx(data, startDate, endDate);
        var filename = $"occupancy-report-{startDate:yyyy-MM-dd}--{endDate:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }
}
