namespace Cinema.Application.Showtimes;

public interface IShowtimeAdminService
{
    Task<IReadOnlyList<ShowtimeAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ShowtimeAdminDto>> GetByCinemaAsync(int cinemaBranchId, CancellationToken ct = default);
    Task<IReadOnlyList<ShowtimeAdminDto>> GetByHallAsync(int hallId, CancellationToken ct = default);
    Task<ShowtimeAdminDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CreateShowtimeRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateShowtimeRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<ShowtimeConflictResult> CheckConflictAsync(int? excludeShowtimeId, int hallId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}