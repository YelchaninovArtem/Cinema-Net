using Cinema.Domain.Common;
using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public sealed class PromoCode
{
    private PromoCode() { }

    public PromoCode(
        string code,
        DiscountType discountType,
        decimal value,
        DateTime validFrom,
        DateTime validTo,
        int usageLimit,
        int perUserLimit,
        bool isPersonal = false,
        string? ownerUserId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Promo code cannot be empty.");
        if (value <= 0)
            throw new DomainException("Promo code value must be positive.");
        if (discountType == DiscountType.Percent && value > 100)
            throw new DomainException("Percent discount cannot exceed 100.");
        if (validTo <= validFrom)
            throw new DomainException("ValidTo must be after ValidFrom.");
        if (usageLimit < 0)
            throw new DomainException("Usage limit cannot be negative.");
        if (perUserLimit < 1)
            throw new DomainException("Per-user limit must be at least 1.");

        Code         = code.Trim().ToUpperInvariant();
        DiscountType = discountType;
        Value        = value;
        ValidFrom    = validFrom;
        ValidTo      = validTo;
        UsageLimit   = usageLimit;
        PerUserLimit = perUserLimit;
        IsPersonal   = isPersonal;
        OwnerUserId  = ownerUserId;
        UsageCount   = 0;
    }

    public int          Id          { get; private set; }
    public string       Code        { get; private set; } = default!;
    public DiscountType DiscountType { get; private set; }
    public decimal      Value       { get; private set; }
    public DateTime     ValidFrom   { get; private set; }
    public DateTime     ValidTo     { get; private set; }

    /// <summary>0 = необмежено.</summary>
    public int          UsageLimit  { get; private set; }
    public int          PerUserLimit { get; private set; }
    public bool         IsPersonal  { get; private set; }

    /// <summary>Якщо IsPersonal = true, код видається конкретному користувачу.</summary>
    public string?      OwnerUserId { get; private set; }

    public int          UsageCount  { get; private set; }

    // ── Обчислення знижки ─────────────────────────────────────────────────────

    /// <summary>Обчислює суму знижки для переданого total.</summary>
    public decimal CalculateDiscount(decimal total)
    {
        if (total <= 0) return 0;
        return DiscountType == DiscountType.Percent
            ? Math.Round(total * Value / 100, 2)
            : Math.Min(Value, total);
    }

    // ── Валідація ─────────────────────────────────────────────────────────────

    /// <summary>Перевіряє, чи можна застосувати код до зазначеного замовлення.</summary>
    public void Validate(DateTime now, string? userId, int userUsageCount)
    {
        if (now < ValidFrom || now > ValidTo)
            throw new DomainException("Promo code is not valid at this time.");
        if (UsageLimit > 0 && UsageCount >= UsageLimit)
            throw new DomainException("Promo code usage limit reached.");
        if (IsPersonal && userId == null)
            throw new DomainException("This promo code is for registered users only.");
        if (IsPersonal && OwnerUserId != null && OwnerUserId != userId)
            throw new DomainException("This promo code does not belong to you.");
        if (userUsageCount >= PerUserLimit)
            throw new DomainException("You have already used this promo code the maximum number of times.");
    }

    public void IncrementUsage() => UsageCount++;

    public void Update(
        string code,
        DiscountType discountType,
        decimal value,
        DateTime validFrom,
        DateTime validTo,
        int usageLimit,
        int perUserLimit,
        bool isPersonal = false,
        string? ownerUserId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Promo code cannot be empty.");
        if (value <= 0)
            throw new DomainException("Promo code value must be positive.");
        if (discountType == DiscountType.Percent && value > 100)
            throw new DomainException("Percent discount cannot exceed 100.");
        if (validTo <= validFrom)
            throw new DomainException("ValidTo must be after ValidFrom.");
        if (usageLimit < 0)
            throw new DomainException("Usage limit cannot be negative.");
        if (perUserLimit < 1)
            throw new DomainException("Per-user limit must be at least 1.");

        Code         = code.Trim().ToUpperInvariant();
        DiscountType = discountType;
        Value        = value;
        ValidFrom    = validFrom;
        ValidTo      = validTo;
        UsageLimit   = usageLimit;
        PerUserLimit = perUserLimit;
        IsPersonal   = isPersonal;
        OwnerUserId  = ownerUserId;
    }
}
