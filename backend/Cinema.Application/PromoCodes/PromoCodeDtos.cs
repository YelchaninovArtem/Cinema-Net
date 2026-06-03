using Cinema.Domain.Enums;

namespace Cinema.Application.PromoCodes;

public sealed record ApplyPromoResponse(
    decimal DiscountAmount,
    decimal NewTotal);

public sealed record PromoCodeDto(
    int Id,
    string Code,
    DiscountType DiscountType,
    decimal Value,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit,
    int PerUserLimit,
    bool IsPersonal,
    string? OwnerUserId,
    int UsageCount);

public sealed record CreatePromoCodeRequest(
    string Code,
    DiscountType DiscountType,
    decimal Value,
    DateTime ValidFrom,
    DateTime ValidTo,
    int UsageLimit,
    int PerUserLimit,
    bool IsPersonal = false,
    string? OwnerUserId = null);
