using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class PaymentTicketConfiguration : IEntityTypeConfiguration<PaymentTicket>
{
    public void Configure(EntityTypeBuilder<PaymentTicket> pt)
    {
        pt.ToTable("PaymentTickets");

        pt.HasKey(x => new { x.PaymentId, x.TicketId });

        pt.HasOne(x => x.Payment)
            .WithMany(p => p.TicketLinks)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        pt.HasOne(x => x.Ticket)
            .WithMany(t => t.PaymentLinks)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure one ticket belongs to only one payment
        pt.HasIndex(x => x.TicketId)
           .IsUnique()
           .HasDatabaseName("IX_PaymentTickets_TicketId_Unique");
    }
}
