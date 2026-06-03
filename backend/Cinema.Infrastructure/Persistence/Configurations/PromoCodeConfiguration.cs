using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> b)
    {
        b.ToTable("PromoCodes");

        b.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(x => x.Code).IsUnique();

        b.Property(x => x.DiscountType)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.Value)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        b.Property(x => x.OwnerUserId).HasMaxLength(450);
    }
}
