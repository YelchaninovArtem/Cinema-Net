using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

internal sealed class HallConfiguration : IEntityTypeConfiguration<Hall>
{
    public void Configure(EntityTypeBuilder<Hall> b)
    {
        b.ToTable("Halls");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(60).IsRequired();
        b.Property(x => x.Rows).IsRequired();
        b.Property(x => x.Cols).IsRequired();
        b.Property(x => x.SeatLayoutJson).IsRequired();

        b.HasIndex(x => new { x.CinemaBranchId, x.Name }).IsUnique();
    }
}
