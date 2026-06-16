using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cinema.Infrastructure.Persistence.Migrations;

public partial class AddLoyaltyAccountUserForeignKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE loyaltyAccount
            FROM [LoyaltyAccounts] AS loyaltyAccount
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [AspNetUsers] AS applicationUser
                WHERE applicationUser.[Id] = loyaltyAccount.[UserId]
            );
            """);

        migrationBuilder.AddForeignKey(
            name: "FK_LoyaltyAccounts_AspNetUsers_UserId",
            table: "LoyaltyAccounts",
            column: "UserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_LoyaltyAccounts_AspNetUsers_UserId",
            table: "LoyaltyAccounts");
    }
}
