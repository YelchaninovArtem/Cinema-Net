namespace Cinema.Application.Movies;

public interface IMovieQueryService
{
    Task<IReadOnlyList<MovieSummaryDto>> GetMoviesAsync(MovieFilters filters, CancellationToken ct = default);
    Task<MovieDetailDto?> GetMovieByIdAsync(int id, CancellationToken ct = default);
}
