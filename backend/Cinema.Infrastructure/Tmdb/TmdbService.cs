using Cinema.Application.Tmdb;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cinema.Infrastructure.Tmdb;

public sealed class TmdbService : ITmdbService
{
    private readonly HttpClient      _http;
    private readonly TmdbOptions     _opts;
    private readonly IMemoryCache    _cache;

    private static readonly Dictionary<string, string> _certMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["G"]     = "G",
        ["PG"]    = "PG",
        ["PG-13"] = "PG13",
        ["R"]     = "R",
        ["NC-17"] = "NC17",
    };

    public TmdbService(HttpClient http, IOptions<TmdbOptions> opts, IMemoryCache cache)
    {
        _http  = http;
        _opts  = opts.Value;
        _cache = cache;
    }

    public async Task<IReadOnlyList<TmdbMovieSearchResult>> SearchMoviesAsync(
        string query, CancellationToken ct = default)
    {
        var cacheKey = $"tmdb:search:{query.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<TmdbMovieSearchResult>? cached))
            return cached!;

        var genreMap = await GetGenreMapAsync(ct);
        var url      = $"search/movie?api_key={_opts.ApiKey}&query={Uri.EscapeDataString(query)}&language=en-US&page=1";
        var resp     = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json    = await resp.Content.ReadAsStringAsync(ct);
        var doc     = JsonNode.Parse(json)!;
        var results = doc["results"]?.AsArray() ?? new JsonArray();

        var list = results
            .Take(10)
            .Select(r =>
            {
                var genreIds = r!["genre_ids"]?.AsArray()
                    .Select(g => g!.GetValue<int>())
                    .ToArray() ?? [];
                var genres = genreIds
                    .Select(id => genreMap.TryGetValue(id, out var n) ? n : null)
                    .Where(n => n is not null)
                    .Select(n => n!)
                    .ToArray();

                var releaseDate = r["release_date"]?.GetValue<string>();
                int? year = releaseDate is { Length: >= 4 }
                    ? int.TryParse(releaseDate[..4], out var y) ? y : null
                    : null;

                var posterPath = r["poster_path"]?.GetValue<string>();
                var posterUrl  = posterPath is not null ? $"{_opts.ImageBaseUrl}{posterPath}" : null;

                return new TmdbMovieSearchResult(
                    r["id"]!.GetValue<int>(),
                    r["title"]?.GetValue<string>() ?? "",
                    year,
                    posterUrl,
                    genres);
            })
            .ToList()
            .AsReadOnly();

        _cache.Set(cacheKey, (IReadOnlyList<TmdbMovieSearchResult>)list,
            TimeSpan.FromMinutes(_opts.CacheDurationMinutes));

        return list;
    }

    public async Task<TmdbMovieDetail?> GetMovieDetailAsync(int tmdbId, CancellationToken ct = default)
    {
        var cacheKey = $"tmdb:detail:{tmdbId}";
        if (_cache.TryGetValue(cacheKey, out TmdbMovieDetail? cached))
            return cached;

        // Три паралельні запити
        var detailTask  = FetchJsonAsync($"movie/{tmdbId}?api_key={_opts.ApiKey}&language=en-US", ct);
        var videosTask  = FetchJsonAsync($"movie/{tmdbId}/videos?api_key={_opts.ApiKey}&language=en-US", ct);
        var relDateTask = FetchJsonAsync($"movie/{tmdbId}/release_dates?api_key={_opts.ApiKey}", ct);

        JsonNode? detailJson, videosJson, relDateJson;
        try
        {
            await Task.WhenAll(detailTask, videosTask, relDateTask);
            detailJson  = detailTask.Result;
            videosJson  = videosTask.Result;
            relDateJson = relDateTask.Result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (detailJson is null) return null;

        // Trailer: перший YouTube Trailer
        var trailer = videosJson?["results"]?.AsArray()
            .FirstOrDefault(v =>
                v!["site"]?.GetValue<string>() == "YouTube" &&
                v["type"]?.GetValue<string>() == "Trailer");
        var trailerUrl = trailer is not null
            ? $"https://www.youtube.com/watch?v={trailer["key"]!.GetValue<string>()}"
            : null;

        // Age rating: US certification
        var usCerts = relDateJson?["results"]?.AsArray()
            .FirstOrDefault(r => r!["iso_3166_1"]?.GetValue<string>() == "US");
        var cert = usCerts?["release_dates"]?.AsArray()
            .Select(d => d!["certification"]?.GetValue<string>() ?? "")
            .FirstOrDefault(c => c.Length > 0);
        var ageRating = cert is not null && _certMap.TryGetValue(cert, out var mapped) ? mapped : "PG";

        // Poster
        var posterPath = detailJson["poster_path"]?.GetValue<string>();
        var posterUrl  = posterPath is not null ? $"{_opts.ImageBaseUrl}{posterPath}" : null;

        // Genres
        var genres = detailJson["genres"]?.AsArray()
            .Select(g => g!["name"]?.GetValue<string>() ?? "")
            .Where(n => n.Length > 0)
            .ToArray() ?? [];

        // Release date
        var releaseDateStr = detailJson["release_date"]?.GetValue<string>() ?? "1970-01-01";
        var releaseDate    = DateTime.SpecifyKind(
            DateTime.Parse(releaseDateStr), DateTimeKind.Utc);

        var detail = new TmdbMovieDetail(
            detailJson["id"]!.GetValue<int>(),
            detailJson["title"]?.GetValue<string>() ?? "",
            detailJson["overview"]?.GetValue<string>() ?? "",
            detailJson["runtime"]?.GetValue<int>() ?? 0,
            ageRating,
            releaseDate,
            posterUrl,
            trailerUrl,
            genres);

        _cache.Set(cacheKey, detail, TimeSpan.FromMinutes(_opts.CacheDurationMinutes));
        return detail;
    }

    public async Task<TmdbMoviePageResult> GetNowPlayingAsync(
        TmdbDiscoverFilters filters, CancellationToken ct = default)
    {
        var cacheKey = $"tmdb:now:{filters.GenreId}:{filters.OriginalLanguage}:{filters.SortBy}:{filters.Page}";
        if (_cache.TryGetValue(cacheKey, out TmdbMoviePageResult? cached))
            return cached!;

        var genreMap = await GetGenreMapAsync(ct);

        var today = DateTime.UtcNow.Date;
        var from  = today.AddMonths(-2).ToString("yyyy-MM-dd");
        var to    = today.ToString("yyyy-MM-dd");

        var url = $"discover/movie?api_key={_opts.ApiKey}&language=en-US" +
                  $"&release_date.gte={from}&release_date.lte={to}" +
                  $"&with_release_type=2|3" +
                  $"&sort_by={Uri.EscapeDataString(filters.SortBy)}" +
                  $"&page={filters.Page}";

        if (filters.GenreId.HasValue)
            url += $"&with_genres={filters.GenreId.Value}";
        if (!string.IsNullOrEmpty(filters.OriginalLanguage))
            url += $"&with_original_language={Uri.EscapeDataString(filters.OriginalLanguage)}";

        var doc     = await FetchJsonAsync(url, ct);
        var page    = doc?["page"]?.GetValue<int>() ?? 1;
        var total   = doc?["total_pages"]?.GetValue<int>() ?? 1;
        var results = doc?["results"]?.AsArray() ?? new JsonArray();

        var list = results.Select(r =>
        {
            var genreIds = r!["genre_ids"]?.AsArray()
                .Select(g => g!.GetValue<int>()).ToArray() ?? [];
            var genres = genreIds
                .Select(id => genreMap.TryGetValue(id, out var n) ? n : null)
                .Where(n => n is not null).Select(n => n!).ToArray();

            var releaseDate = r["release_date"]?.GetValue<string>();
            int? year = releaseDate is { Length: >= 4 }
                ? int.TryParse(releaseDate[..4], out var y) ? y : null
                : null;

            var posterPath = r["poster_path"]?.GetValue<string>();
            var posterUrl  = posterPath is not null ? $"{_opts.ImageBaseUrl}{posterPath}" : null;

            return new TmdbMovieSearchResult(
                r["id"]!.GetValue<int>(),
                r["title"]?.GetValue<string>() ?? "",
                year,
                posterUrl,
                genres);
        }).ToList().AsReadOnly();

        var result = new TmdbMoviePageResult(list, page, total);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_opts.CacheDurationMinutes));
        return result;
    }

    public async Task<IReadOnlyList<TmdbGenre>> GetGenresAsync(CancellationToken ct = default)
    {
        var map = await GetGenreMapAsync(ct);
        return map.Select(kv => new TmdbGenre(kv.Key, kv.Value))
                  .OrderBy(g => g.Name)
                  .ToList()
                  .AsReadOnly();
    }

    private async Task<Dictionary<int, string>> GetGenreMapAsync(CancellationToken ct)
    {
        const string key = "tmdb:genres";
        if (_cache.TryGetValue(key, out Dictionary<int, string>? map))
            return map!;

        var url  = $"genre/movie/list?api_key={_opts.ApiKey}&language=en-US";
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        var doc  = JsonNode.Parse(json)!;

        var result = doc["genres"]!.AsArray()
            .ToDictionary(
                g => g!["id"]!.GetValue<int>(),
                g => g!["name"]!.GetValue<string>());

        _cache.Set(key, result, TimeSpan.FromHours(24));
        return result;
    }

    private async Task<JsonNode?> FetchJsonAsync(string url, CancellationToken ct)
    {
        var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            throw new HttpRequestException("Not found", null, HttpStatusCode.NotFound);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonNode.Parse(json);
    }
}
