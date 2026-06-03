using System.Net;
using System.Net.Http.Json;
using Cinema.Application.Cinemas;
using Cinema.Application.Genres;
using Cinema.Application.Movies;
using Cinema.Application.Showtimes;
using Cinema.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cinema.Tests.Integration;

public sealed class CatalogControllerTests : IClassFixture<CinemaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CinemaWebApplicationFactory _factory;

    public CatalogControllerTests(CinemaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Genres ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGenres_ReturnsNonEmptyList()
    {
        var genres = await _client.GetFromJsonAsync<List<GenreDto>>("/api/genres");

        Assert.NotNull(genres);
        Assert.NotEmpty(genres);
        Assert.All(genres, g => Assert.False(string.IsNullOrWhiteSpace(g.Name)));
    }

    // ── Cinemas ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCinemas_ReturnsNonEmptyList()
    {
        var cinemas = await _client.GetFromJsonAsync<List<CinemaBranchDto>>("/api/cinemas");

        Assert.NotNull(cinemas);
        Assert.NotEmpty(cinemas);
        Assert.All(cinemas, c => Assert.False(string.IsNullOrWhiteSpace(c.City)));
    }

    // ── Movies ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMovies_NoFilters_ReturnsSeedMovies()
    {
        await SeedCatalogAsync();

        var movies = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies");

        Assert.NotNull(movies);
        Assert.NotEmpty(movies);
        Assert.All(movies, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Title));
            Assert.True(m.DurationMinutes > 0);
            Assert.NotEmpty(m.Genres);
        });
    }

    [Fact]
    public async Task GetMovies_FilterByCity_ReturnsSubset()
    {
        await SeedCatalogAsync();

        var all  = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies");
        var kyiv = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies?city=Kyiv");

        Assert.NotNull(all);
        Assert.NotNull(kyiv);
        Assert.True(kyiv.Count <= all.Count);
    }

    [Fact]
    public async Task GetMovies_FilterByUnknownCity_ReturnsEmpty()
    {
        await SeedCatalogAsync();

        var movies = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies?city=NoSuchCity");

        Assert.NotNull(movies);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task GetMovies_FilterByTitle_IsCaseInsensitive()
    {
        await SeedCatalogAsync();

        var all = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies");
        Assert.NotNull(all);
        Assert.NotEmpty(all);
        var expected = all.First();
        var query = expected.Title.ToUpperInvariant();

        var movies = await _client.GetFromJsonAsync<List<MovieSummaryDto>>(
            $"/api/movies?title={Uri.EscapeDataString(query)}");

        Assert.NotNull(movies);
        Assert.Contains(movies, m => m.Id == expected.Id);
        Assert.All(movies, m => Assert.Contains(query, m.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMovieById_ExistingId_ReturnsDetail()
    {
        await SeedCatalogAsync();

        var movies = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies");
        Assert.NotNull(movies);
        var first = movies.First();

        var detail = await _client.GetFromJsonAsync<MovieDetailDto>($"/api/movies/{first.Id}");

        Assert.NotNull(detail);
        Assert.Equal(first.Id, detail.Id);
        Assert.Equal(first.Title, detail.Title);
        Assert.False(string.IsNullOrWhiteSpace(detail.Description));
    }

    [Fact]
    public async Task GetMovieById_NonExistingId_Returns404()
    {
        var response = await _client.GetAsync("/api/movies/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Showtimes ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShowtimes_FilterByMovieId_ReturnsOnlyThatMovie()
    {
        await SeedCatalogAsync();

        var movies = await _client.GetFromJsonAsync<List<MovieSummaryDto>>("/api/movies");
        Assert.NotNull(movies);
        var movie = movies.First();

        var showtimes = await _client.GetFromJsonAsync<List<ShowtimeDto>>(
            $"/api/showtimes?movieId={movie.Id}");

        Assert.NotNull(showtimes);
        Assert.NotEmpty(showtimes);
        Assert.All(showtimes, s => Assert.Equal(movie.Id, s.MovieId));
    }

    [Fact]
    public async Task GetShowtimes_FilterByFormat_ReturnsMatchingFormat()
    {
        await SeedCatalogAsync();

        var showtimes = await _client.GetFromJsonAsync<List<ShowtimeDto>>("/api/showtimes?format=IMAX");

        Assert.NotNull(showtimes);
        if (showtimes.Count > 0)
            Assert.All(showtimes, s => Assert.Equal("IMAX", s.Format));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task SeedCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        // Сід виконується через DbSeeder лише якщо записів немає; повторний виклик — no-op
        if (!db.Movies.Any())
            await CinemaDbSeeder.SeedCatalogForTestsAsync(db);
    }
}
