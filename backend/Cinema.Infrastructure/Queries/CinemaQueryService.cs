using Cinema.Application.Cinemas;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Queries;

public sealed class CinemaQueryService : ICinemaQueryService
{
    private readonly CinemaDbContext _db;

    public CinemaQueryService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<CinemaBranchDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.CinemaBranches
            .AsNoTracking()
            .OrderBy(c => c.City).ThenBy(c => c.Name)
            .Select(c => new CinemaBranchDto(c.Id, c.Name, c.City, c.Address))
            .ToListAsync(ct);
}
