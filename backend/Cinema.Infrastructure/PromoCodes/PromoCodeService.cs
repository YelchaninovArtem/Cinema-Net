using Cinema.Application.PromoCodes;
using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Infrastructure.PromoCodes;

public sealed class PromoCodeService : IPromoCodeService
{
    private readonly CinemaDbContext _db;

    public PromoCodeService(CinemaDbContext db) => _db = db;

    public async Task<PromoCode> ValidateAndGetAsync(string code, string? userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Promo code is required.");

        var normalized = code.Trim().ToUpperInvariant();
        var promo = await _db.PromoCodes
            .FirstOrDefaultAsync(p => p.Code == normalized, ct)
            ?? throw new DomainException("Promo code not found.");

        // Validate dates
        var now = DateTime.UtcNow;
        promo.Validate(now, userId, await GetUserUsageCountAsync(promo.Id, userId, ct));

        return promo;
    }

    public async Task IncrementUsageAsync(int promoCodeId, CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync([promoCodeId], ct)
            ?? throw new DomainException("Promo code not found.");
        promo.IncrementUsage();
        await _db.SaveChangesAsync(ct);
    }

    // Admin methods

    public async Task<IReadOnlyList<PromoCodeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var codes = await _db.PromoCodes.OrderBy(p => p.Code).ToListAsync(ct);
        return codes.Select(ToDto).ToList();
    }

    public async Task<PromoCodeDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync([id], ct);
        return promo is null ? null : ToDto(promo);
    }

    public async Task<PromoCodeDto> CreateAsync(CreatePromoCodeRequest req, CancellationToken ct = default)
    {
        var promo = new PromoCode(
            req.Code, req.DiscountType, req.Value,
            req.ValidFrom, req.ValidTo,
            req.UsageLimit, req.PerUserLimit,
            req.IsPersonal, req.OwnerUserId);

        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync(ct);
        return ToDto(promo);
    }

    public async Task UpdateAsync(int id, UpdatePromoCodeRequest req, CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync([id], ct)
            ?? throw new DomainException("Promo code not found.");

        promo.Update(
            req.Code, req.DiscountType, req.Value,
            req.ValidFrom, req.ValidTo,
            req.UsageLimit, req.PerUserLimit,
            req.IsPersonal, req.OwnerUserId);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var promo = await _db.PromoCodes.FindAsync([id], ct)
            ?? throw new DomainException("Promo code not found.");
        _db.PromoCodes.Remove(promo);
        await _db.SaveChangesAsync(ct);
    }

    private static PromoCodeDto ToDto(PromoCode p) => new(
        p.Id, p.Code, p.DiscountType, p.Value,
        p.ValidFrom, p.ValidTo,
        p.UsageLimit, p.PerUserLimit,
        p.IsPersonal, p.OwnerUserId,
        p.UsageCount);

    private async Task<int> GetUserUsageCountAsync(int promoId, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId)) return 0;

        // Count of paid tickets that used this promo code
        return await _db.Tickets
            .CountAsync(t => t.UserId == userId && t.PromoCodeId == promoId && t.Status == TicketStatus.Paid, ct);
    }
}