using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Cinema.Application.Users;
using FluentAssertions;
using Xunit;

namespace Cinema.Tests.Integration;

[Collection("Integration")]
public class StaffUserTests : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CinemaWebApplicationFactory _factory;

    public StaffUserTests(CinemaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ═══════════════════ GET /api/admin/staff ═══════════════════

    [Fact]
    public async Task GetStaff_ReturnsOk_WithAdminAndCashier()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.GetAsync("/api/admin/staff");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var staff = await response.Content.ReadFromJsonAsync<IReadOnlyList<StaffUserDto>>();
        staff.Should().NotBeEmpty();
        staff!.Should().OnlyContain(u => u.Role == "Admin" || u.Role == "Cashier");
    }

    [Fact]
    public async Task GetStaff_WithoutAuth_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/admin/staff");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════ POST /api/admin/staff ═══════════════════

    [Fact]
    public async Task CreateStaff_ReturnsCreated_WhenValidAdmin()
    {
        await _factory.LoginAdminAsync(_client);
        var request = new
        {
            email = $"newadmin_{Guid.NewGuid():N}@cinema.test",
            password = "Admin_123!",
            firstName = "New",
            lastName = "Admin",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<StaffUserDto>();
        dto.Should().NotBeNull();
        dto!.Role.Should().Be("Admin");
        dto.Email.Should().Be(request.email);
    }

    [Fact]
    public async Task CreateStaff_ReturnsCreated_WhenValidCashier()
    {
        await _factory.LoginAdminAsync(_client);
        var request = new
        {
            email = $"newcashier_{Guid.NewGuid():N}@cinema.test",
            password = "Cashier_456!",
            firstName = "New",
            lastName = "Cashier",
            role = "Cashier"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<StaffUserDto>();
        dto!.Role.Should().Be("Cashier");
    }

    [Fact]
    public async Task CreateStaff_ReturnsBadRequest_WhenDuplicateEmail()
    {
        await _factory.LoginAdminAsync(_client);
        var request = new
        {
            email = "admin@cinema.local",
            password = "Admin_123!",
            firstName = "Dup",
            lastName = "Admin",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStaff_ReturnsBadRequest_WhenRoleIsClient()
    {
        await _factory.LoginAdminAsync(_client);
        var request = new
        {
            email = $"test_{Guid.NewGuid():N}@cinema.test",
            password = "Pass_123!",
            firstName = "Test",
            lastName = "User",
            role = "Client"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStaff_ReturnsBadRequest_WhenPasswordTooShort()
    {
        await _factory.LoginAdminAsync(_client);
        var request = new
        {
            email = $"test_{Guid.NewGuid():N}@cinema.test",
            password = "1",
            firstName = "Test",
            lastName = "User",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStaff_WithoutAuth_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var request = new
        {
            email = $"test_{Guid.NewGuid():N}@cinema.test",
            password = "Pass_123!",
            firstName = "T",
            lastName = "U",
            role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/admin/staff", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════ DELETE /api/admin/staff/{id} ═══════════════════

    [Fact]
    public async Task DeleteStaff_ReturnsNoContent_WhenExists()
    {
        await _factory.LoginAdminAsync(_client);
        var createRequest = new
        {
            email = $"todelete_{Guid.NewGuid():N}@cinema.test",
            password = "Delete_123!",
            firstName = "To",
            lastName = "Delete",
            role = "Cashier"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/admin/staff", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<StaffUserDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/admin/staff/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteStaff_ReturnsNotFound_WhenNotExists()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.DeleteAsync("/api/admin/staff/nonexistent-id-99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
