using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

internal sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> b)
    {
        b.ToTable("Showtimes");
        b.HasKey(x => x.Id);
        b.Property(x => x.StartUtc).IsRequired();
        b.Property(x => x.Format).IsRequired().HasConversion<int>();
        b.Property(x => x.BasePrice).HasColumnType("decimal(10,2)").IsRequired();

        b.HasOne(x => x.Movie)
         .WithMany(m => m.Showtimes)
         .HasForeignKey(x => x.MovieId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Hall)
         .WithMany(h => h.Showtimes)
         .HasForeignKey(x => x.HallId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.HallId, x.StartUtc });
        b.HasIndex(x => x.StartUtc);
    }
}
