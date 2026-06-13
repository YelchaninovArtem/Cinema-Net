using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cinema.Application.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Infrastructure.Localization;

public sealed class ContentLocalizationService : IContentLocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> UkGenreNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Action"] = "Бойовик",
            ["Adventure"] = "Пригоди",
            ["Animation"] = "Анімація",
            ["Comedy"] = "Комедія",
            ["Crime"] = "Кримінал",
            ["Drama"] = "Драма",
            ["Family"] = "Сімейний",
            ["Fantasy"] = "Фентезі",
            ["Horror"] = "Жахи",
            ["Mystery"] = "Детектив",
            ["Romance"] = "Романтика",
            ["Science Fiction"] = "Наукова фантастика",
            ["Sci-Fi"] = "Наукова фантастика",
            ["Thriller"] = "Трилер",
            ["War"] = "Військовий",
            ["Western"] = "Вестерн",
        };

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly TranslationOptions _options;
    private readonly ILogger<ContentLocalizationService> _logger;

    public ContentLocalizationService(
        HttpClient http,
        IMemoryCache cache,
        IOptions<TranslationOptions> options,
        ILogger<ContentLocalizationService> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "en";

        var normalized = language.Trim().ToLowerInvariant();
        return normalized.StartsWith("uk") || normalized.StartsWith("ua") ? "uk" : "en";
    }

    public IReadOnlyList<string> LocalizeGenres(IEnumerable<string> genres, string? language)
    {
        var lang = NormalizeLanguage(language);
        return genres
            .Select(g => lang == "uk" && UkGenreNames.TryGetValue(g, out var translated) ? translated : g)
            .OrderBy(g => g)
            .ToList();
    }

    public async Task<string> LocalizeMovieDescriptionAsync(string description, string? language, CancellationToken ct = default)
    {
        var lang = NormalizeLanguage(language);
        if (lang == "en" || string.IsNullOrWhiteSpace(description))
            return description;

        var cacheKey = $"movie-description:{lang}:{Hash(description)}";
        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        try
        {
            var url = "translate_a/single?client=gtx&sl=en&dt=t" +
                      $"&tl={Uri.EscapeDataString(ToGoogleTargetLanguage(lang))}" +
                      $"&q={Uri.EscapeDataString(description)}";
            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return description;

            var payload = await response.Content.ReadAsStringAsync(ct);
            var translated = ExtractGoogleTranslation(payload);
            if (string.IsNullOrWhiteSpace(translated))
                return description;

            _cache.Set(cacheKey, translated, TimeSpan.FromMinutes(Math.Max(1, _options.CacheDurationMinutes)));
            return translated;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Movie description translation failed.");
            return description;
        }
    }

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private static string ToGoogleTargetLanguage(string language) => language switch
    {
        "uk" => "uk",
        _    => "en",
    };

    private static string? ExtractGoogleTranslation(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        var sentences = root[0];
        if (sentences.ValueKind != JsonValueKind.Array)
            return null;

        var builder = new StringBuilder();
        foreach (var sentence in sentences.EnumerateArray())
        {
            if (sentence.ValueKind == JsonValueKind.Array &&
                sentence.GetArrayLength() > 0 &&
                sentence[0].ValueKind == JsonValueKind.String)
            {
                builder.Append(sentence[0].GetString());
            }
        }

        return builder.ToString();
    }
}
