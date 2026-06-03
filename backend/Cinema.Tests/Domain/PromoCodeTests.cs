using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using FluentAssertions;

namespace Cinema.Tests.Domain;

public sealed class PromoCodeTests
{
    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    // ── Конструктор ──────────────────────────────────────────────────────────

    [Fact]
    public void Creates_percent_promo_with_valid_data()
    {
        var promo = new PromoCode("SUMMER20", DiscountType.Percent, 20,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 100, 1);

        promo.Code.Should().Be("SUMMER20");
        promo.DiscountType.Should().Be(DiscountType.Percent);
        promo.Value.Should().Be(20);
        promo.UsageCount.Should().Be(0);
    }

    [Fact]
    public void Code_is_normalized_to_uppercase()
    {
        var promo = new PromoCode("save50", DiscountType.Fixed, 50,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        promo.Code.Should().Be("SAVE50");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_code(string code)
    {
        var act = () => new PromoCode(code, DiscountType.Fixed, 50,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_zero_value()
    {
        var act = () => new PromoCode("TEST", DiscountType.Fixed, 0,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_percent_above_100()
    {
        var act = () => new PromoCode("TEST", DiscountType.Percent, 101,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_validTo_before_validFrom()
    {
        var act = () => new PromoCode("TEST", DiscountType.Percent, 10,
            Utc(2027, 1, 1), Utc(2026, 1, 1), 0, 1);
        act.Should().Throw<DomainException>();
    }

    // ── CalculateDiscount ─────────────────────────────────────────────────────

    [Fact]
    public void CalculateDiscount_Percent_ReturnsCorrectValue()
    {
        var promo = new PromoCode("P10", DiscountType.Percent, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        promo.CalculateDiscount(200m).Should().Be(20m);
    }

    [Fact]
    public void CalculateDiscount_Fixed_CappedAtTotal()
    {
        var promo = new PromoCode("F500", DiscountType.Fixed, 500,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        // знижка не може перевищувати загальну суму
        promo.CalculateDiscount(100m).Should().Be(100m);
    }

    [Fact]
    public void CalculateDiscount_Fixed_ReturnsExactAmount()
    {
        var promo = new PromoCode("F50", DiscountType.Fixed, 50,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        promo.CalculateDiscount(200m).Should().Be(50m);
    }

    // ── Validate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_Throws_WhenExpired()
    {
        var promo = new PromoCode("OLD", DiscountType.Percent, 10,
            Utc(2024, 1, 1), Utc(2025, 1, 1), 0, 1);
        var act = () => promo.Validate(Utc(2026, 4, 15), null, 0);
        act.Should().Throw<DomainException>().WithMessage("*not valid at this time*");
    }

    [Fact]
    public void Validate_Throws_WhenUsageLimitReached()
    {
        var promo = new PromoCode("LIM", DiscountType.Percent, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), usageLimit: 1, perUserLimit: 1);
        promo.IncrementUsage();
        var act = () => promo.Validate(Utc(2026, 4, 15), null, 0);
        act.Should().Throw<DomainException>().WithMessage("*usage limit*");
    }

    [Fact]
    public void Validate_Throws_WhenPersonalPromoUsedByGuest()
    {
        var promo = new PromoCode("PERS", DiscountType.Percent, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1, isPersonal: true);
        var act = () => promo.Validate(Utc(2026, 4, 15), userId: null, userUsageCount: 0);
        act.Should().Throw<DomainException>().WithMessage("*registered users*");
    }

    [Fact]
    public void Validate_Throws_WhenPersonalPromoUsedByWrongUser()
    {
        var promo = new PromoCode("VIP", DiscountType.Percent, 20,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1, isPersonal: true, ownerUserId: "user1");
        var act = () => promo.Validate(Utc(2026, 4, 15), "user2", 0);
        act.Should().Throw<DomainException>().WithMessage("*does not belong*");
    }

    [Fact]
    public void Validate_Throws_WhenPerUserLimitReached()
    {
        var promo = new PromoCode("ONCE", DiscountType.Percent, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, perUserLimit: 1);
        var act = () => promo.Validate(Utc(2026, 4, 15), "user1", userUsageCount: 1);
        act.Should().Throw<DomainException>().WithMessage("*maximum number of times*");
    }

    [Fact]
    public void Validate_Passes_WhenCodeIsValid()
    {
        var promo = new PromoCode("GOOD", DiscountType.Percent, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        var act = () => promo.Validate(Utc(2026, 4, 15), null, 0);
        act.Should().NotThrow();
    }

    // ── IncrementUsage ────────────────────────────────────────────────────────

    [Fact]
    public void IncrementUsage_IncreasesCount()
    {
        var promo = new PromoCode("CNT", DiscountType.Fixed, 10,
            Utc(2026, 1, 1), Utc(2027, 1, 1), 0, 1);
        promo.IncrementUsage();
        promo.IncrementUsage();
        promo.UsageCount.Should().Be(2);
    }
}
