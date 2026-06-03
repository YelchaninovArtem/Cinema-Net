using Cinema.Domain.Common;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Infrastructure.Persistence;
using Cinema.Infrastructure.PromoCodes;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Tests.Unit.PromoCodes;

public sealed class PromoCodeServiceTests : IDisposable
{
    private readonly CinemaDbContext _db;
    private readonly PromoCodeService _svc;

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    public PromoCodeServiceTests()
    {
        var opts = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new CinemaDbContext(opts);
        _svc = new PromoCodeService(_db);
    }

    public void Dispose() => _db.Dispose();

    private PromoCode AddPromo(
        string code,
        DiscountType type = DiscountType.Percent,
        decimal value     = 10,
        bool isPersonal   = false,
        string? ownerId   = null,
        DateTime? from    = null,
        DateTime? to      = null,
        int usageLimit    = 0,
        int perUserLimit  = 1)
    {
        var promo = new PromoCode(code, type, value,
            from ?? Utc(2026, 1, 1), to ?? Utc(2027, 1, 1),
            usageLimit, perUserLimit, isPersonal, ownerId);
        _db.PromoCodes.Add(promo);
        _db.SaveChanges();
        return promo;
    }

    [Fact]
    public async Task ValidateAndGetAsync_ReturnsPromo_WhenValid()
    {
        AddPromo("TEST10", DiscountType.Percent, 10);
        var result = await _svc.ValidateAndGetAsync("TEST10", null);
        result.Code.Should().Be("TEST10");
    }

    [Fact]
    public async Task ValidateAndGetAsync_Throws_WhenNotFound()
    {
        var act = async () => await _svc.ValidateAndGetAsync("NOEXIST", null);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ValidateAndGetAsync_Throws_WhenExpired()
    {
        AddPromo("EXP", from: Utc(2020, 1, 1), to: Utc(2021, 1, 1));
        var act = async () => await _svc.ValidateAndGetAsync("EXP", null);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not valid at this time*");
    }

    [Fact]
    public async Task IncrementUsageAsync_IncrementsCount()
    {
        var promo = AddPromo("ONCE");
        await _svc.IncrementUsageAsync(promo.Id);
        var updated = await _db.PromoCodes.FindAsync(promo.Id);
        updated!.UsageCount.Should().Be(1);
    }
}