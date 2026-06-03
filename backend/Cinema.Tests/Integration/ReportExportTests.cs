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
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/admin/reports/sales/pdf?{DateRange}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
