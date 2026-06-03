# Report Export (PDF + Excel) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add PDF and Excel export buttons to the admin Reports tab; four new backend endpoints stream file downloads for sales and occupancy reports.

**Architecture:** `IReportExporter` interface lives in `Cinema.Application.Reports`; two implementations (`PdfReportExporter` using QuestPDF, `ExcelReportExporter` using ClosedXML) live in `Cinema.Infrastructure.Reports`. `ReportsController` gets four new endpoints that call `IReportService` then `IReportExporter`. The Angular admin component adds export buttons that construct the download URL and trigger an anchor click — no frontend library needed.

**Tech Stack:** QuestPDF 2024.x (MIT), ClosedXML 0.102.x (MIT), ASP.NET 8, Angular 19 + Material.

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `backend/Cinema.Application/Reports/IReportExporter.cs` | Interface with 4 export methods |
| Create | `backend/Cinema.Infrastructure/Reports/PdfReportExporter.cs` | QuestPDF implementation |
| Create | `backend/Cinema.Infrastructure/Reports/ExcelReportExporter.cs` | ClosedXML implementation |
| Modify | `backend/Cinema.Infrastructure/Cinema.Infrastructure.csproj` | Add QuestPDF + ClosedXML packages |
| Modify | `backend/Cinema.Infrastructure/DependencyInjection.cs` | Register IReportExporter |
| Modify | `backend/Cinema.Api/Controllers/ReportsController.cs` | Add 4 export endpoints |
| Create | `backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs` | Unit tests for both exporters |
| Modify | `frontend/src/app/core/services/admin.service.ts` | Add 4 export URL helper methods |
| Modify | `frontend/src/app/features/admin/admin.component.ts` | Export buttons + downloadReport() |
| Modify | `frontend/public/i18n/en.json` | i18n keys |
| Modify | `frontend/public/i18n/uk.json` | i18n keys |

---

### Task 1: Install NuGet packages + define IReportExporter

**Files:**
- Modify: `backend/Cinema.Infrastructure/Cinema.Infrastructure.csproj`
- Create: `backend/Cinema.Application/Reports/IReportExporter.cs`

- [ ] **Step 1: Add packages to Infrastructure .csproj**

Open `backend/Cinema.Infrastructure/Cinema.Infrastructure.csproj` and add inside the existing `<ItemGroup>`:

```xml
<PackageReference Include="QuestPDF" Version="2024.10.4" />
<PackageReference Include="ClosedXML" Version="0.102.3" />
```

- [ ] **Step 2: Restore packages**

```bash
cd backend
dotnet restore Cinema.Infrastructure/Cinema.Infrastructure.csproj
```

Expected: no errors, both packages downloaded.

- [ ] **Step 3: Create IReportExporter interface**

Create `backend/Cinema.Application/Reports/IReportExporter.cs`:

```csharp
namespace Cinema.Application.Reports;

public interface IReportExporter
{
    byte[] ExportSalesPdf(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to);
    byte[] ExportSalesXlsx(IReadOnlyList<SalesReportItem> data, DateTime from, DateTime to);
    byte[] ExportOccupancyPdf(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to);
    byte[] ExportOccupancyXlsx(IReadOnlyList<OccupancyReportItem> data, DateTime from, DateTime to);
}
```

- [ ] **Step 4: Build Application project to verify interface compiles**

```bash
dotnet build Cinema.Application/Cinema.Application.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add backend/Cinema.Infrastructure/Cinema.Infrastructure.csproj \
        backend/Cinema.Application/Reports/IReportExporter.cs
git commit -m "feat(reports): add IReportExporter interface and install QuestPDF/ClosedXML"
```

---

### Task 2: Implement ExcelReportExporter

**Files:**
- Create: `backend/Cinema.Infrastructure/Reports/ExcelReportExporter.cs`
- Create: `backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs` (Excel tests only)

- [ ] **Step 1: Write failing tests for Excel export**

Create `backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs`:

```csharp
using Cinema.Application.Reports;
using Cinema.Infrastructure.Reports;
using ClosedXML.Excel;
using FluentAssertions;
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
        ws.Cell(2, 1).GetString().Should().Be("2024-01-05");
        ws.Cell(2, 2).GetValue<int>().Should().Be(3);
        ws.Cell(2, 3).GetValue<decimal>().Should().Be(750.00m);
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
        ws.Cell(2, 1).GetString().Should().Be("Hall B");
        ws.Cell(2, 2).GetString().Should().Be("Dune");
        ws.Cell(2, 3).GetString().Should().Be("2024-01-10");
        ws.Cell(2, 4).GetValue<int>().Should().Be(60);
        ws.Cell(2, 5).GetValue<int>().Should().Be(120);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd backend
dotnet test Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~ExcelReportExporterTests" --no-build 2>&1 | tail -5
```

Expected: compile error — `ExcelReportExporter` not found.

- [ ] **Step 3: Implement ExcelReportExporter**

Create `backend/Cinema.Infrastructure/Reports/ExcelReportExporter.cs`:

```csharp
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
```

**Important design note:** Since both exporters implement `IReportExporter` but only cover half the methods each, the controller will inject both explicitly via named/keyed DI or by injecting concrete types. We keep `IReportExporter` as a single interface but register two separate named services. See Task 5 (DI registration) for how the controller accesses them.

- [ ] **Step 4: Add ClosedXML package to Tests project**

Open `backend/Cinema.Tests/Cinema.Tests.csproj` and add:

```xml
<PackageReference Include="ClosedXML" Version="0.102.3" />
```

Then restore:

```bash
dotnet restore Cinema.Tests/Cinema.Tests.csproj
```

- [ ] **Step 5: Run Excel tests to verify they pass**

```bash
dotnet test Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~ExcelReportExporterTests" 2>&1 | tail -8
```

Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 6: Commit**

```bash
git add backend/Cinema.Infrastructure/Reports/ExcelReportExporter.cs \
        backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs \
        backend/Cinema.Tests/Cinema.Tests.csproj
git commit -m "feat(reports): implement ExcelReportExporter with ClosedXML"
```

---

### Task 3: Implement PdfReportExporter

**Files:**
- Create: `backend/Cinema.Infrastructure/Reports/PdfReportExporter.cs`
- Modify: `backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs` (add PDF tests)

- [ ] **Step 1: Add PDF tests**

Append to `backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs`:

```csharp
public sealed class PdfReportExporterTests
{
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
        // PDF magic bytes: %PDF
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
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd backend
dotnet test Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~PdfReportExporterTests" 2>&1 | tail -5
```

Expected: compile error — `PdfReportExporter` not found.

- [ ] **Step 3: Configure QuestPDF license (Community)**

QuestPDF Community license requires a one-line call at startup. We'll do this in `Program.cs`.

Open `backend/Cinema.Api/Program.cs` and add near the top (after `var builder = WebApplication.CreateBuilder(args);`):

```csharp
QuestPDF.Infrastructure.QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
```

Also add the using at the top of the file if not already present (or it can be a global using). Add to `backend/Cinema.Api/Program.cs` imports:

```csharp
using QuestPDF.Infrastructure;
```

- [ ] **Step 4: Implement PdfReportExporter**

Create `backend/Cinema.Infrastructure/Reports/PdfReportExporter.cs`:

```csharp
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

                    // header
                    foreach (var header in new[] { "Date", "Bookings", "Revenue (UAH)" })
                    {
                        table.Cell().Background(Colors.Blue.Lighten3)
                            .Padding(5).Text(header).Bold();
                    }

                    // rows
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
```

- [ ] **Step 5: Run PDF tests**

```bash
dotnet test Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~PdfReportExporterTests" 2>&1 | tail -8
```

Expected: `Passed! - Failed: 0, Passed: 2`

- [ ] **Step 6: Commit**

```bash
git add backend/Cinema.Infrastructure/Reports/PdfReportExporter.cs \
        backend/Cinema.Tests/Unit/Reports/ReportExporterTests.cs \
        backend/Cinema.Api/Program.cs
git commit -m "feat(reports): implement PdfReportExporter with QuestPDF"
```

---

### Task 4: Register exporters in DI and add controller endpoints

**Files:**
- Modify: `backend/Cinema.Infrastructure/DependencyInjection.cs`
- Modify: `backend/Cinema.Api/Controllers/ReportsController.cs`

Since the two exporters split the interface by method (PDF vs XLSX), the controller will receive both concrete types directly (not through `IReportExporter`) to avoid ambiguity. This is simpler than keyed DI and avoids `NotSupportedException` at runtime.

- [ ] **Step 1: Register both exporters in DI**

Open `backend/Cinema.Infrastructure/DependencyInjection.cs` and add after the `IReportService` registration line (`services.AddScoped<IReportService, ReportService>();`):

```csharp
services.AddSingleton<PdfReportExporter>();
services.AddSingleton<ExcelReportExporter>();
```

Also add the using at the top of the file:

```csharp
using Cinema.Infrastructure.Reports;
```

(This using may already exist or may need to be added — verify it's not duplicated.)

- [ ] **Step 2: Update ReportsController with export endpoints**

Replace the entire content of `backend/Cinema.Api/Controllers/ReportsController.cs` with:

```csharp
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
```

- [ ] **Step 3: Build the solution**

```bash
cd backend
dotnet build Cinema.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Run all tests**

```bash
dotnet test Cinema.sln 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 200` (194 existing + 6 new).

- [ ] **Step 5: Commit**

```bash
git add backend/Cinema.Infrastructure/DependencyInjection.cs \
        backend/Cinema.Api/Controllers/ReportsController.cs
git commit -m "feat(reports): register exporters in DI, add PDF/XLSX endpoints to ReportsController"
```

---

### Task 5: Integration tests for export endpoints

**Files:**
- Create: `backend/Cinema.Tests/Integration/ReportExportTests.cs`

- [ ] **Step 1: Write integration tests**

Create `backend/Cinema.Tests/Integration/ReportExportTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Xunit;

namespace Cinema.Tests.Integration;

[Collection("Integration")]
public sealed class ReportExportTests : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CinemaWebApplicationFactory _factory;

    public ReportExportTests(CinemaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task LoginAsAdmin() => await _factory.LoginAdminAsync(_client);

    private static string DateRange =>
        "startDate=2024-01-01&endDate=2024-12-31";

    [Fact]
    public async Task SalesPdf_Returns200_WithPdfContentType_WhenAdmin()
    {
        await LoginAsAdmin();
        var response = await _client.GetAsync($"/api/admin/reports/sales/pdf?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SalesXlsx_Returns200_WithXlsxContentType_WhenAdmin()
    {
        await LoginAsAdmin();
        var response = await _client.GetAsync($"/api/admin/reports/sales/xlsx?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OccupancyPdf_Returns200_WithPdfContentType_WhenAdmin()
    {
        await LoginAsAdmin();
        var response = await _client.GetAsync($"/api/admin/reports/occupancy/pdf?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task OccupancyXlsx_Returns200_WithXlsxContentType_WhenAdmin()
    {
        await LoginAsAdmin();
        var response = await _client.GetAsync($"/api/admin/reports/occupancy/xlsx?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task SalesPdf_Returns401_WithoutAuth()
    {
        var response = await _client.GetAsync($"/api/admin/reports/sales/pdf?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
cd backend
dotnet test Cinema.Tests/Cinema.Tests.csproj --filter "FullyQualifiedName~ReportExportTests" 2>&1 | tail -8
```

Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 3: Commit**

```bash
git add backend/Cinema.Tests/Integration/ReportExportTests.cs
git commit -m "test(reports): add integration tests for PDF/XLSX export endpoints"
```

---

### Task 6: Frontend — i18n keys + export buttons

**Files:**
- Modify: `frontend/public/i18n/en.json`
- Modify: `frontend/public/i18n/uk.json`
- Modify: `frontend/src/app/core/services/admin.service.ts`
- Modify: `frontend/src/app/features/admin/admin.component.ts`

- [ ] **Step 1: Add i18n keys**

In `frontend/public/i18n/en.json`, find the `"reportGenerated"` key and add after it:

```json
"exportPdf": "Export PDF",
"exportXlsx": "Export Excel",
```

In `frontend/public/i18n/uk.json`, same location:

```json
"exportPdf": "Експорт PDF",
"exportXlsx": "Експорт Excel",
```

- [ ] **Step 2: Add export URL helpers to AdminService**

In `frontend/src/app/core/services/admin.service.ts`, after the `getOccupancyReport` method (around line 328), add:

```typescript
getReportExportUrl(type: 'sales' | 'occupancy', format: 'pdf' | 'xlsx', startDate: string, endDate: string): string {
  return `${this.base}/admin/reports/${type}/${format}?startDate=${startDate}&endDate=${endDate}`;
}
```

- [ ] **Step 3: Add downloadReport method and export buttons to AdminComponent**

In `frontend/src/app/features/admin/admin.component.ts`, locate the `loadReports()` method (around line 2087) and add the `downloadReport` method directly after:

```typescript
downloadReport(type: 'sales' | 'occupancy', format: 'pdf' | 'xlsx') {
  const start = this.reportStartDate.toISOString().split('T')[0];
  const end = this.reportEndDate.toISOString().split('T')[0];
  const token = this.authSvc.getAccessToken();
  const url = this.adminSvc.getReportExportUrl(type, format, start, end);

  // Use fetch with auth header, then trigger browser download
  fetch(url, { headers: { Authorization: `Bearer ${token}` } })
    .then(res => res.blob())
    .then(blob => {
      const ext = format === 'pdf' ? 'pdf' : 'xlsx';
      const filename = `${type}-report-${start}--${end}.${ext}`;
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = filename;
      a.click();
      URL.revokeObjectURL(a.href);
    });
}
```

- [ ] **Step 4: Inject AuthService in AdminComponent**

Verify that `AuthService` is already injected in `AdminComponent`. Search for `authSvc` in the file:

```bash
grep -n "authSvc\|AuthService" frontend/src/app/features/admin/admin.component.ts | head -10
```

If `AuthService` is not injected, find the constructor/injection section and add:

```typescript
private readonly authSvc = inject(AuthService);
```

Also ensure the import at the top includes `AuthService`:

```typescript
import { AuthService } from '../../core/auth/auth.service';
```

- [ ] **Step 5: Add export buttons to the Reports tab template**

In `admin.component.ts`, find the reports toolbar section (around line 352):

```html
              <button mat-flat-button color="primary" (click)="loadReports()">
                <mat-icon>search</mat-icon>
                {{ 'admin.generate' | translate }}
              </button>
```

Replace it with:

```html
              <button mat-flat-button color="primary" (click)="loadReports()">
                <mat-icon>search</mat-icon>
                {{ 'admin.generate' | translate }}
              </button>
              <button mat-stroked-button color="warn" (click)="downloadReport('sales', 'pdf')">
                <mat-icon>picture_as_pdf</mat-icon>
                {{ 'admin.exportPdf' | translate }} (Sales)
              </button>
              <button mat-stroked-button color="accent" (click)="downloadReport('sales', 'xlsx')">
                <mat-icon>table_chart</mat-icon>
                {{ 'admin.exportXlsx' | translate }} (Sales)
              </button>
              <button mat-stroked-button color="warn" (click)="downloadReport('occupancy', 'pdf')">
                <mat-icon>picture_as_pdf</mat-icon>
                {{ 'admin.exportPdf' | translate }} (Occupancy)
              </button>
              <button mat-stroked-button color="accent" (click)="downloadReport('occupancy', 'xlsx')">
                <mat-icon>table_chart</mat-icon>
                {{ 'admin.exportXlsx' | translate }} (Occupancy)
              </button>
```

- [ ] **Step 6: Build frontend**

```bash
npm --prefix frontend run build 2>&1 | tail -10
```

Expected: build completes with no errors.

- [ ] **Step 7: Run frontend tests**

```bash
npm --prefix frontend run test:ci 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 8: Commit**

```bash
git add frontend/public/i18n/en.json \
        frontend/public/i18n/uk.json \
        frontend/src/app/core/services/admin.service.ts \
        frontend/src/app/features/admin/admin.component.ts
git commit -m "feat(reports): add PDF/Excel export buttons to admin Reports tab"
```

---

### Task 7: Final full test run

- [ ] **Step 1: Run all backend tests**

```bash
cd backend
dotnet test Cinema.sln 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: ≥204` (194 original + 6 unit + 5 integration new).

- [ ] **Step 2: Run all frontend tests**

```bash
npm --prefix frontend run test:ci 2>&1 | tail -5
```

Expected: all passing.

- [ ] **Step 3: Final commit if any stragglers**

If any files were modified during cleanup, commit them. Otherwise, done.

---

## Spec Coverage Checklist

- [x] `IReportExporter` in Application layer → Task 1
- [x] `PdfReportExporter` (QuestPDF) → Task 3
- [x] `ExcelReportExporter` (ClosedXML) → Task 2
- [x] 4 new endpoints (`sales/pdf`, `sales/xlsx`, `occupancy/pdf`, `occupancy/xlsx`) → Task 4
- [x] DI registration → Task 4
- [x] Integration tests (auth + content-type) → Task 5
- [x] Unit tests for both exporters → Tasks 2–3
- [x] Frontend export buttons (anchor-download pattern with auth) → Task 6
- [x] i18n EN + UK → Task 6
