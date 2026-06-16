using Cinema.Domain.Entities;
using Cinema.Infrastructure.Identity;
using Cinema.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Tests.Unit.Persistence;

public sealed class FavoriteConfigurationTests
{
    [Fact]
    public void Model_MapsUserIdAsForeignKeyToApplicationUser()
    {
        var options = new DbContextOptionsBuilder<CinemaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new CinemaDbContext(options);

        var entityType = db.Model.FindEntityType(typeof(Favorite));
        var foreignKey = entityType!.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser));

        foreignKey.Properties.Single().Name.Should().Be(nameof(Favorite.UserId));
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }
}
