using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CinemaDbContext))]
[Migration("20260531130000_AllowResaleOfRefundedSeats")]
public sealed class AllowResaleOfRefundedSeats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tickets_ShowtimeId_Row_Col",
            table: "Tickets");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_ShowtimeId_Row_Col",
            table: "Tickets",
            columns: new[] { "ShowtimeId", "Row", "Col" },
            unique: true,
            filter: "[Status] <> 2 AND [Status] <> 4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tickets_ShowtimeId_Row_Col",
            table: "Tickets");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_ShowtimeId_Row_Col",
            table: "Tickets",
            columns: new[] { "ShowtimeId", "Row", "Col" },
            unique: true,
            filter: "[Status] <> 2");
    }
}
