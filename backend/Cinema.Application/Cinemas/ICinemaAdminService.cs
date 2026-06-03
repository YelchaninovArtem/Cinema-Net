namespace Cinema.Application.Cinemas;

public interface ICinemaAdminService
{
    Task<IReadOnlyList<CinemaAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<CinemaAdminDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateCinemaRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateCinemaRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
