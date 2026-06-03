namespace Cinema.Application.Halls;

public interface IHallAdminService
{
    Task<IReadOnlyList<HallAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HallAdminDto>> GetByCinemaAsync(int cinemaBranchId, CancellationToken ct = default);
    Task<HallAdminDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateHallRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateHallRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
