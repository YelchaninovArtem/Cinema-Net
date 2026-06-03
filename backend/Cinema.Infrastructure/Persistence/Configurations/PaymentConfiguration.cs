using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> p)
    {
        p.HasKey(x => x.Id);
        p.Property(x => x.Provider).HasConversion<string>().HasMaxLength(20);
        p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        p.Property(x => x.Amount).HasPrecision(10, 2);
        p.Property(x => x.OriginalAmount).HasPrecision(10, 2);
        p.Property(x => x.ExternalId).HasMaxLength(200).IsRequired(false);
        p.Property(x => x.CreatedUtc)
            .HasColumnType("datetime2")
            .IsRequired();
        p.HasIndex(x => x.ExternalId);
        p.Navigation(x => x.TicketLinks).HasField("_ticketLinks");

        // No Booking FK anymore; Payment ↔ Ticket via PaymentTicket junction
    }
}
