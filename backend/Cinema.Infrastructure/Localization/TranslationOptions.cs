namespace Cinema.Infrastructure.Localization;

public sealed class TranslationOptions
{
    public string BaseUrl { get; set; } = "https://translate.googleapis.com";
    public string? ApiKey { get; set; }
    public int CacheDurationMinutes { get; set; } = 1440;
}
