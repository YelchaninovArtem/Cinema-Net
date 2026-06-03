using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.HasKey(a => a.UserId);
        builder.Property(a => a.UserId).HasMaxLength(450);
    }
}

public sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.UserId).HasMaxLength(450);
        builder.Property(t => t.Reason).HasMaxLength(200);
        builder.HasIndex(t => t.UserId);

        // Optional FK to Ticket (nullable for non-purchase events)
        builder.HasOne(t => t.Ticket)
            .WithMany()
            .HasForeignKey(t => t.TicketId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
