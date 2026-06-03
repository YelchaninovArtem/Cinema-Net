namespace Cinema.Infrastructure.Tmdb;

public sealed class TmdbOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3";
    public string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/w500";
    public int CacheDurationMinutes { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
