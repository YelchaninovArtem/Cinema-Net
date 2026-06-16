using Cinema.Domain.Entities;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Tests.Unit.Persistence;

public sealed class LoyaltyAccountConfigurationTests
{
    [Fact]
    public void Model_MapsUserIdAsUniqueForeignKeyToApplicationUser()
    {
        var options = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new CinemaDbContext(options);

        var entityType = db.Model.FindEntityType(typeof(LoyaltyAccount));
        var foreignKey = entityType!.GetForeignKeys().Single();

        foreignKey.Properties.Single().Name.Should().Be(nameof(LoyaltyAccount.UserId));
        foreignKey.PrincipalEntityType.ClrType.Should().Be(typeof(ApplicationUser));
        foreignKey.IsUnique.Should().BeTrue();
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }
}
