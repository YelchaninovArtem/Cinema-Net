using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

internal sealed class CinemaBranchConfiguration : IEntityTypeConfiguration<CinemaBranch>
{
    public void Configure(EntityTypeBuilder<CinemaBranch> b)
    {
        b.ToTable("CinemaBranches");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.City).HasMaxLength(80).IsRequired();
        b.Property(x => x.Address).HasMaxLength(200).IsRequired();
        b.Property(x => x.TimezoneId).HasMaxLength(80).IsRequired();

        b.HasMany(x => x.Halls)
         .WithOne(h => h.CinemaBranch)
         .HasForeignKey(h => h.CinemaBranchId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.City, x.Name });
    }
}
