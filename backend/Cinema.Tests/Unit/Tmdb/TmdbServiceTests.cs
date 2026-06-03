using Cinema.Application.Tmdb;
using Cinema.Infrastructure.Tmdb;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using System.Net;

namespace Cinema.Tests.Unit.Tmdb;

public sealed class TmdbServiceTests
{
    private static TmdbService BuildService(MockHttpMessageHandler mockHttp, IMemoryCache? cache = null)
    {
        var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };
        var opts = Options.Create(new TmdbOptions
        {
            ApiKey        = "test-key",
            BaseUrl       = "https://api.themoviedb.org/3",
            ImageBaseUrl  = "https://image.tmdb.org/t/p/w500",
            CacheDurationMinutes = 30,
            MaxRetries    = 3
        });
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new TmdbService(httpClient, opts, cache);
    }

    private static string GenreListJson() => """
        { "genres": [{ "id": 28, "name": "Action" }, { "id": 18, "name": "Drama" }] }
        """;

    private static string SearchResultJson() => """
        {
          "results": [
            {
              "id": 550,
              "title": "Fight Club",
              "overview": "An insomniac...",
              "release_date": "1999-10-15",
              "poster_path": "/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
              "genre_ids": [28, 18]
            }
          ]
        }
        """;

    private static string MovieDetailJson() => """
        {
          "id": 550,
          "title": "Fight Club",
          "overview": "An insomniac office worker...",
          "runtime": 139,
          "release_date": "1999-10-15",
          "poster_path": "/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
          "genres": [{ "id": 28, "name": "Action" }]
        }
        """;

    private static string VideosJson() => """
        {
          "results": [
            { "key": "SUXWAEX2jlg", "site": "YouTube", "type": "Trailer" },
            { "key": "other",        "site": "YouTube", "type": "Teaser" }
          ]
        }
        """;

    private static string DiscoverJson(int page = 1, int totalPages = 3) => $$"""
        {
          "page": {{page}},
          "total_pages": {{totalPages}},
          "results": [
            {
              "id": 100,
              "title": "New Movie",
              "release_date": "2026-03-01",
              "poster_path": "/abc.jpg",
              "genre_ids": [28]
            }
          ]
        }
        """;

    private static string ReleaseDatesJson(string usCert = "R") => $$"""
        {
          "results": [
            {
              "iso_3166_1": "US",
              "release_dates": [{ "certification": "{{usCert}}", "release_date": "1999-10-15T00:00:00.000Z" }]
            }
          ]
        }
        """;

    private static string SimpleDetailJson(int id = 1) => $$"""
        { "id": {{id}}, "title": "T", "overview": "D", "runtime": 90,
          "release_date": "2020-01-01", "poster_path": null, "genres": [] }
        """;

    [Fact]
    public async Task SearchMoviesAsync_ReturnsResults_WhenQueryMatches()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*genre/movie/list*").Respond("application/json", GenreListJson());
        mock.When("*search/movie*").Respond("application/json", SearchResultJson());

        var svc    = BuildService(mock);
        var result = await svc.SearchMoviesAsync("Fight Club");

        result.Should().HaveCount(1);
        result[0].TmdbId.Should().Be(550);
        result[0].Title.Should().Be("Fight Club");
        result[0].Year.Should().Be(1999);
        result[0].Genres.Should().Contain("Action").And.Contain("Drama");
        result[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg");
    }

    [Fact]
    public async Task SearchMoviesAsync_ReturnsCachedResult_OnSecondCall()
    {
        var mock  = new MockHttpMessageHandler();
        int calls = 0;
        mock.When("*genre/movie/list*").Respond("application/json", GenreListJson());
        mock.When("*search/movie*").Respond(_ =>
        {
            calls++;
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent(SearchResultJson(), System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(resp);
        });

        var svc = BuildService(mock);
        await svc.SearchMoviesAsync("Fight Club");
        await svc.SearchMoviesAsync("Fight Club");

        calls.Should().Be(1, "second call should be served from cache");
    }

    [Fact]
    public async Task GetMovieDetailAsync_BatchesThreeCalls_AndMapsCorrectly()
    {
        var mock = new MockHttpMessageHandler();
        // Register sub-paths before the catch-all so they match first
        mock.When("*movie/550/videos*").Respond("application/json", VideosJson());
        mock.When("*movie/550/release_dates*").Respond("application/json", ReleaseDatesJson("R"));
        mock.When("*movie/550*").Respond("application/json", MovieDetailJson());

        var svc    = BuildService(mock);
        var result = await svc.GetMovieDetailAsync(550);

        result.Should().NotBeNull();
        result!.TmdbId.Should().Be(550);
        result.Title.Should().Be("Fight Club");
        result.Description.Should().Be("An insomniac office worker...");
        result.DurationMinutes.Should().Be(139);
        result.AgeRating.Should().Be("R");
        result.ReleaseDateUtc.Year.Should().Be(1999);
        result.PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg");
        result.TrailerUrl.Should().Be("https://www.youtube.com/watch?v=SUXWAEX2jlg");
        result.Genres.Should().Contain("Action");
    }

    [Theory]
    [InlineData("G",     "G")]
    [InlineData("PG",    "PG")]
    [InlineData("PG-13", "PG13")]
    [InlineData("R",     "R")]
    [InlineData("NC-17", "NC17")]
    public async Task GetMovieDetailAsync_MapsAgeRating(string tmdbCert, string expected)
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*movie/1/videos*").Respond("application/json", """{ "results": [] }""");
        mock.When("*movie/1/release_dates*").Respond("application/json", ReleaseDatesJson(tmdbCert));
        mock.When("*movie/1*").Respond("application/json", SimpleDetailJson(1));

        var svc    = BuildService(mock);
        var result = await svc.GetMovieDetailAsync(1);

        result!.AgeRating.Should().Be(expected);
    }

    [Fact]
    public async Task GetMovieDetailAsync_DefaultsAgeRatingToPG_WhenNoCertFound()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*movie/1/videos*").Respond("application/json", """{ "results": [] }""");
        mock.When("*movie/1/release_dates*").Respond("application/json", """{ "results": [] }""");
        mock.When("*movie/1*").Respond("application/json", SimpleDetailJson(1));

        var svc    = BuildService(mock);
        var result = await svc.GetMovieDetailAsync(1);

        result!.AgeRating.Should().Be("PG");
    }

    [Fact]
    public async Task GetMovieDetailAsync_ExtractsYouTubeTrailer_IgnoresNonTrailers()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*movie/1/videos*").Respond("application/json", """
            {
              "results": [
                { "key": "teaser_key", "site": "YouTube", "type": "Teaser" },
                { "key": "clip_key",   "site": "Vimeo",   "type": "Trailer" },
                { "key": "trailer_key","site": "YouTube", "type": "Trailer" }
              ]
            }
            """);
        mock.When("*movie/1/release_dates*").Respond("application/json", """{ "results": [] }""");
        mock.When("*movie/1*").Respond("application/json", SimpleDetailJson(1));

        var svc    = BuildService(mock);
        var result = await svc.GetMovieDetailAsync(1);

        result!.TrailerUrl.Should().Be("https://www.youtube.com/watch?v=trailer_key");
    }

    [Fact]
    public async Task GetMovieDetailAsync_ReturnsNull_When404()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*movie/9999*").Respond(HttpStatusCode.NotFound);

        var svc    = BuildService(mock);
        var result = await svc.GetMovieDetailAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNowPlayingAsync_ReturnsResults_WithCorrectMapping()
    {
        var mock = new MockHttpMessageHandler();
        mock.When("*genre/movie/list*").Respond("application/json", GenreListJson());
        mock.When("*discover/movie*").Respond("application/json", DiscoverJson(1, 3));

        var svc    = BuildService(mock);
        var result = await svc.GetNowPlayingAsync(new TmdbDiscoverFilters(null, null, "popularity.desc", 1));

        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(3);
        result.Results.Should().HaveCount(1);
        result.Results[0].TmdbId.Should().Be(100);
        result.Results[0].Title.Should().Be("New Movie");
        result.Results[0].Year.Should().Be(2026);
        result.Results[0].PosterUrl.Should().Be("https://image.tmdb.org/t/p/w500/abc.jpg");
        result.Results[0].Genres.Should().Contain("Action");
    }

    [Fact]
    public async Task GetNowPlayingAsync_AppliesGenreFilter_InUrl()
    {
        var mock = new MockHttpMessageHandler();
        string? capturedUrl = null;
        mock.When("*genre/movie/list*").Respond("application/json", GenreListJson());
        mock.When("*discover/movie*").Respond(req =>
        {
            capturedUrl = req.RequestUri?.ToString();
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            resp.Content = new StringContent(DiscoverJson(), System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(resp);
        });

        var svc = BuildService(mock);
        await svc.GetNowPlayingAsync(new TmdbDiscoverFilters(28, "en", "vote_average.desc", 2));

        capturedUrl.Should().Contain("with_genres=28");
        capturedUrl.Should().Contain("with_original_language=en");
        capturedUrl.Should().Contain("sort_by=vote_average.desc");
        capturedUrl.Should().Contain("page=2");
    }

    [Fact]
    public async Task GetNowPlayingAsync_CachesResultPerPageAndFilter()
    {
        var mock  = new MockHttpMessageHandler();
        int calls = 0;
        mock.When("*genre/movie/list*").Respond("application/json", GenreListJson());
        mock.When("*discover/movie*").Respond(_ =>
        {
            calls++;
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            resp.Content = new StringContent(DiscoverJson(), System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(resp);
        });

        var svc     = BuildService(mock);
        var filters = new TmdbDiscoverFilters(null, null, "popularity.desc", 1);
        await svc.GetNowPlayingAsync(filters);
        await svc.GetNowPlayingAsync(filters);

        calls.Should().Be(1, "second call with same filters+page should be cached");
    }
}
