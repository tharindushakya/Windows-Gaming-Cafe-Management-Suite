using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingCafe.Data.Migrations
{
    public partial class RemoveLegacyUserWalletBalance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create Wallet rows for users that don't have one, seeded from the legacy Users.WalletBalance
            // Use SQL to perform this in a single statement to avoid client-side loops.
            migrationBuilder.Sql(@"
                INSERT INTO ""Wallets"" (""UserId"", ""Balance"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                SELECT u.""UserId"", u.""WalletBalance"", TRUE, NOW(), NOW()
                FROM ""Users"" u
                LEFT JOIN ""Wallets"" w ON w.""UserId"" = u.""UserId""
                WHERE w.""WalletId"" IS NULL
            ");

            // 2) Remove the legacy column from Users now that the data is copied into Wallets
            // Note: dropping a column is irreversible at DB-level without a restore; Down will recreate the column
            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the legacy column with the original type and default 0
            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Users",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            // Copy balances back from Wallets into Users where applicable
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""WalletBalance"" = COALESCE(w.""Balance"", 0)
                FROM ""Wallets"" w
                WHERE w.""UserId"" = u.""UserId""
            ");
        }
    }
}
