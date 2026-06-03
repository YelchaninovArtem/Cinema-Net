using System.Net;
using System.Net.Http.Json;
using Cinema.Application.Auth;
using FluentAssertions;

namespace Cinema.Tests.Integration;

[Collection("Integration")]
public sealed class AuthControllerTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_200_with_tokens()
    {
        var request = new RegisterRequest(
            Email: $"test_{Guid.NewGuid():N}@example.com",
            Password: "Test_1234",
            FirstName: "Test",
            LastName: "User");

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.Role.Should().Be("Client");
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_400()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest(email, "Test_1234", "A", "B");

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var second = await _client.PostAsJsonAsync("/api/auth/register", request);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_200()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test_1234", "A", "B"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Test_1234"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var email = $"bad_{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test_1234", "A", "B"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Wrong_9999"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_valid_token_returns_new_tokens()
    {
        // реєстрація → отримуємо refresh token
        var email = $"ref_{Guid.NewGuid():N}@example.com";
        var reg = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test_1234", "A", "B"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth!.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.RefreshToken.Should().NotBe(auth.RefreshToken); // токен змінено
    }

    [Fact]
    public async Task Refresh_with_used_token_returns_401()
    {
        var email = $"used_{Guid.NewGuid():N}@example.com";
        var reg = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "Test_1234", "A", "B"));
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();

        // перше оновлення - рокає старий токен
        await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth!.RefreshToken));

        // повторне використання - помилка
        var second = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_endpoint_requires_authorization()
    {
        // /api/auth/logout - вимагає авторизацію
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new RefreshRequest("fake-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
