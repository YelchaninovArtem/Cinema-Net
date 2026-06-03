using Cinema.Application.Movies;

namespace Cinema.Application.Movies;

public interface IMovieAdminService
{
    Task<IReadOnlyList<MovieAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<MovieAdminDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateMovieRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateMovieRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
