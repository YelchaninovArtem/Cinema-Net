using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => new { f.UserId, f.MovieId });

        builder.HasOne(f => f.Movie)
               .WithMany()
               .HasForeignKey(f => f.MovieId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
