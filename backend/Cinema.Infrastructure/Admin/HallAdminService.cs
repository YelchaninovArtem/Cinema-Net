using Cinema.Application.Halls;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.Admin;

public sealed class HallAdminService : IHallAdminService
{
    private readonly CinemaDbContext _db;

    public HallAdminService(CinemaDbContext db) => _db = db;

    public async Task<IReadOnlyList<HallAdminDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Halls
            .Include(h => h.CinemaBranch)
            .OrderBy(h => h.CinemaBranch!.City)
            .ThenBy(h => h.Name)
            .Select(h => new HallAdminDto(
                h.Id,
                h.CinemaBranchId,
                h.CinemaBranch!.Name,
                h.Name,
                h.Rows,
                h.Cols,
                h.GetLayout()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HallAdminDto>> GetByCinemaAsync(int cinemaBranchId, CancellationToken ct = default) =>
        await _db.Halls
            .Include(h => h.CinemaBranch)
            .Where(h => h.CinemaBranchId == cinemaBranchId)
            .Select(h => new HallAdminDto(
                h.Id,
                h.CinemaBranchId,
                h.CinemaBranch!.Name,
                h.Name,
                h.Rows,
                h.Cols,
                h.GetLayout()))
            .ToListAsync(ct);

    public async Task<HallAdminDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _db.Halls
            .Include(h => h.CinemaBranch)
            .Where(h => h.Id == id)
            .Select(h => new HallAdminDto(
                h.Id,
                h.CinemaBranchId,
                h.CinemaBranch!.Name,
                h.Name,
                h.Rows,
                h.Cols,
                h.GetLayout()))
            .FirstOrDefaultAsync(ct);

    public async Task<int> CreateAsync(CreateHallRequest request, CancellationToken ct = default)
    {
        var hall = new Hall(
            request.CinemaBranchId,
            request.Name,
            request.Rows,
            request.Cols,
            request.Layout);

        _db.Halls.Add(hall);
        await _db.SaveChangesAsync(ct);
        return hall.Id;
    }

    public async Task UpdateAsync(int id, UpdateHallRequest request, CancellationToken ct = default)
    {
        var hall = await _db.Halls.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Hall with ID {id} not found.");

        hall.Rename(request.Name);
        hall.SetLayout(request.Rows, request.Cols, request.Layout);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var hall = await _db.Halls.FindAsync([id], cancellationToken: ct)
            ?? throw new DomainException($"Hall with ID {id} not found.");

        _db.Halls.Remove(hall);
        await _db.SaveChangesAsync(ct);
    }
}
