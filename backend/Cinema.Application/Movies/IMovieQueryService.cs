namespace Cinema.Application.Movies;

public interface IMovieQueryService
{
    Task<IReadOnlyList<MovieSummaryDto>> GetMoviesAsync(MovieFilters filters, CancellationToken ct = default);
    Task<IReadOnlyList<MovieSummaryDto>> GetMoviesAsync(MovieFilters filters, string? language, CancellationToken ct = default);
    Task<MovieDetailDto?> GetMovieByIdAsync(int id, CancellationToken ct = default);
    Task<MovieDetailDto?> GetMovieByIdAsync(int id, string? language, CancellationToken ct = default);
}
