namespace Cinema.Application.Tmdb;

public interface ITmdbService
{
    Task<IReadOnlyList<TmdbMovieSearchResult>> SearchMoviesAsync(string query, CancellationToken ct = default);
    Task<TmdbMovieDetail?> GetMovieDetailAsync(int tmdbId, CancellationToken ct = default);
    Task<TmdbMoviePageResult> GetNowPlayingAsync(TmdbDiscoverFilters filters, CancellationToken ct = default);
    Task<IReadOnlyList<TmdbGenre>> GetGenresAsync(CancellationToken ct = default);
}
