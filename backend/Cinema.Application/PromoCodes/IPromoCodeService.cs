using Cinema.Domain.Entities;
using Cinema.Domain.Enums;

namespace Cinema.Application.PromoCodes;

public interface IPromoCodeService
{
    /// <summary>
    /// Validates a promo code for a user and returns the PromoCode entity if valid.
    /// Throws DomainException if invalid (not found, expired, limit reached, etc.).
    /// </summary>
    Task<PromoCode> ValidateAndGetAsync(string code, string? userId, CancellationToken ct = default);

    /// <summary>
    /// Increments the usage count of a promo code (after successful ticket creation).
    /// </summary>
    Task IncrementUsageAsync(int promoCodeId, CancellationToken ct = default);

    // Адмінські методи
    Task<IReadOnlyList<PromoCodeDto>> GetAllAsync(CancellationToken ct = default);
    Task<PromoCodeDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PromoCodeDto> CreateAsync(CreatePromoCodeRequest request, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdatePromoCodeRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed record UpdatePromoCodeRequest(
    string Code,
    DiscountType DiscountType,
    decimal Value,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit,
    int PerUserLimit,
    bool IsPersonal = false,
    string? OwnerUserId = null);