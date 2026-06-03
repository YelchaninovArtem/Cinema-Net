using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> t)
    {
        t.ToTable("Tickets");

        t.Property(x => x.SeatType)
            .HasConversion<int>()
            .IsRequired();

        t.Property(x => x.Price)
            .HasColumnType("decimal(10,2)");

        t.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        t.Property(x => x.QrToken)
            .HasMaxLength(64)
            .IsRequired();

        t.Property(x => x.PromoDiscount)
            .HasColumnType("decimal(10,2)");

        t.Property(x => x.LoyaltyDiscount)
            .HasColumnType("decimal(10,2)");

        t.Property(x => x.FinalAmount)
            .HasColumnType("decimal(10,2)");

        t.Navigation(x => x.PaymentLinks).HasField("_paymentLinks");

        // Foreign keys
        t.HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        t.HasOne(x => x.PromoCode)
            .WithMany()
            .HasForeignKey(x => x.PromoCodeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Filtered unique index: cancelled and refunded tickets no longer block the seat.
        t.HasIndex(x => new { x.ShowtimeId, x.Row, x.Col })
            .IsUnique()
            .HasFilter("[Status] <> 2 AND [Status] <> 4")
            .HasDatabaseName("IX_Tickets_ShowtimeId_Row_Col");

        // Indexes for common queries
        t.HasIndex(x => x.UserId);
        t.HasIndex(x => x.GuestEmail);
        t.HasIndex(x => x.Status);
        t.HasIndex(x => x.ReminderSentUtc);
    }
}
