using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cinema.Application.Account;
using Cinema.Application.Tickets;
using FluentAssertions;
using Xunit;

namespace Cinema.Tests.Integration;

public sealed class AccountFavoritesTests(CinemaWebApplicationFactory factory)
    : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> LoginAsClientAsync()
    {
        var email = $"fav_test_{Guid.NewGuid():N}@example.com";
        var reg = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password  = "Test_1234!",
            firstName = "Test",
            lastName  = "User",
        });
        reg.EnsureSuccessStatusCode();
        var body = await reg.Content.ReadFromJsonAsync<AuthBody>();
        return body!.AccessToken;
    }

    private sealed record AuthBody(string AccessToken, string RefreshToken, string Role, string Email);

    [Fact]
    public async Task AddAndGet_Favorite_ReturnsMovie()
    {
        var token = await LoginAsClientAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/account/favorites/1", null);
        var list = await _client.GetFromJsonAsync<FavoriteSummaryDto[]>("/api/account/favorites");
        list.Should().NotBeNull();
        list!.Should().Contain(f => f.MovieId == 1);
    }

    [Fact]
    public async Task RemoveFavorite_DisappearsFromList()
    {
        var token = await LoginAsClientAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/account/favorites/2", null);
        var del = await _client.DeleteAsync("/api/account/favorites/2");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<FavoriteSummaryDto[]>("/api/account/favorites");
        list.Should().NotBeNull();
        list!.Should().NotContain(f => f.MovieId == 2);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_IsIdempotent()
    {
        var token = await LoginAsClientAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/account/favorites/3", null);
        var second = await _client.PostAsync("/api/account/favorites/3", null);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<FavoriteSummaryDto[]>("/api/account/favorites");
        list!.Count(f => f.MovieId == 3).Should().Be(1);
    }

    [Fact]
    public async Task GetTickets_ReturnsListForAuthenticatedUser()
    {
        var token = await LoginAsClientAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var list = await _client.GetFromJsonAsync<TicketSummaryDto[]>("/api/account/tickets");
        list.Should().NotBeNull(); // may be empty but request succeeds
    }

    [Fact]
    public async Task GetTickets_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var resp = await _client.GetAsync("/api/account/tickets");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}