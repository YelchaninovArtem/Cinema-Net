using System.Net;
using System.Net.Http.Json;
using Cinema.Application.Tmdb;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cinema.Tests.Integration;

/// <summary>Fake ITmdbService — returns predictable data without real HTTP calls.</summary>
internal sealed class FakeTmdbService : ITmdbService
{
    public Task<IReadOnlyList<TmdbMovieSearchResult>> SearchMoviesAsync(string query, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TmdbMovieSearchResult>>(
        [
            new TmdbMovieSearchResult(550, "Fight Club", 1999, null, ["Drama"])
        ]);

    public Task<TmdbMovieDetail?> GetMovieDetailAsync(int tmdbId, CancellationToken ct = default)
        => Task.FromResult<TmdbMovieDetail?>(null);

    public Task<TmdbMoviePageResult> GetNowPlayingAsync(TmdbDiscoverFilters filters, CancellationToken ct = default)
        => Task.FromResult(new TmdbMoviePageResult(
            [new TmdbMovieSearchResult(42, "Now Movie", 2026, null, ["Action"])],
            filters.Page,
            5));

    public Task<IReadOnlyList<TmdbGenre>> GetGenresAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TmdbGenre>>([new TmdbGenre(28, "Action"), new TmdbGenre(18, "Drama")]);
}

[Collection("Integration")]
public class TmdbControllerTests : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CinemaWebApplicationFactory _factory;

    public TmdbControllerTests(CinemaWebApplicationFactory factory)
    {
        _factory = factory;
        // Replace ITmdbService with the fake for this client
        _client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var d = services.SingleOrDefault(s => s.ServiceType == typeof(ITmdbService));
                if (d is not null) services.Remove(d);
                services.AddSingleton<ITmdbService, FakeTmdbService>();
            })).CreateClient();
    }

    [Fact]
    public async Task Search_Returns200_WithResults()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.GetAsync("/api/admin/tmdb/search?q=Fight+Club");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<IReadOnlyList<TmdbMovieSearchResult>>();
        results.Should().HaveCount(1);
        results![0].Title.Should().Be("Fight Club");
    }

    [Fact]
    public async Task Search_Returns400_WhenQueryEmpty()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.GetAsync("/api/admin/tmdb/search?q=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDetail_Returns404_WhenServiceReturnsNull()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.GetAsync("/api/admin/tmdb/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NowPlaying_Returns200_WithResults()
    {
        await _factory.LoginAdminAsync(_client);
        var response = await _client.GetAsync("/api/admin/tmdb/now-playing?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TmdbMoviePageResult>();
        result.Should().NotBeNull();
        result!.Results.Should().HaveCount(1);
        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public async Task NowPlaying_Returns401_WithoutAuth()
    {
        var anonClient = _factory.CreateClient();
        var response   = await anonClient.GetAsync("/api/admin/tmdb/now-playing?page=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
