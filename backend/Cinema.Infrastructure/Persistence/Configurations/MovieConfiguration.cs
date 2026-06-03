using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

internal sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> b)
    {
        b.ToTable("Movies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        b.Property(x => x.PosterUrl).HasMaxLength(500);
        b.Property(x => x.TrailerUrl).HasMaxLength(500);

        b.HasMany(x => x.Genres)
         .WithMany(g => g.Movies)
         .UsingEntity("MovieGenres");

        b.HasIndex(x => x.Title);
        b.HasIndex(x => x.ReleaseDateUtc);
    }
}
