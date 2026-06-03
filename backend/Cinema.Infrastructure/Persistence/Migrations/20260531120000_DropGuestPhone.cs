using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CinemaDbContext))]
[Migration("20260531120000_DropGuestPhone")]
public sealed class DropGuestPhone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GuestPhone",
            table: "Tickets");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GuestPhone",
            table: "Tickets",
            type: "nvarchar(max)",
            nullable: true);
    }
}
