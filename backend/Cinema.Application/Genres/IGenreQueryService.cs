namespace Cinema.Application.Genres;

public interface IGenreQueryService
{
    Task<IReadOnlyList<GenreDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GenreDto>> GetAllAsync(string? language, CancellationToken ct = default);
    Task<IReadOnlyList<GenreDto>> EnsureAsync(string[] names, CancellationToken ct = default);
}
