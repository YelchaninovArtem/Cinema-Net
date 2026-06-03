using Cinema.Application.Cinemas;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Admin;

public sealed class CinemaAdminService : ICinemaAdminService
{
    private readonly CinemaDbContext _db;

    public CinemaAdminService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<CinemaAdminDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.CinemaBranches
            .OrderBy(c => c.City).ThenBy(c => c.Name)
            .Select(c => new CinemaAdminDto(c.Id, c.Name, c.City, c.Address, c.TimezoneId))
            .ToListAsync(ct);

    public async Task<CinemaAdminDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.CinemaBranches
            .Where(c => c.Id == id)
            .Select(c => new CinemaAdminDto(c.Id, c.Name, c.City, c.Address, c.TimezoneId))
            .FirstOrDefaultAsync(ct);

    public async Task<int> CreateAsync(CreateCinemaRequest request, CancellationToken ct = default)
    {
        var exists = await _db.CinemaBranches
            .Where(c => c.Name == request.Name)
            .AnyAsync(ct);
        
        if (exists)
            throw new DomainException($"Cinema branch with name '{request.Name}' already exists.");

        var cinema = new CinemaBranch(request.Name, request.City, request.Address, "Europe/Kyiv");
        _db.CinemaBranches.Add(cinema);
        await _db.SaveChangesAsync(ct);
        return cinema.Id;
    }

    public async Task UpdateAsync(int id, UpdateCinemaRequest request, CancellationToken ct = default)
    {
        var cinema = await _db.CinemaBranches.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Cinema branch with ID {id} not found.");

        if (cinema.Name != request.Name)
        {
            var nameExists = await _db.CinemaBranches
                .AnyAsync(c => c.Name == request.Name && c.Id != id, ct);
            if (nameExists)
                throw new DomainException($"Cinema branch with name '{request.Name}' already exists.");
        }

        cinema.Rename(request.Name);
        cinema.Relocate(request.City, request.Address);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var cinema = await _db.CinemaBranches.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Cinema branch with ID {id} not found.");

        _db.CinemaBranches.Remove(cinema);
        await _db.SaveChangesAsync(ct);
    }
}
