using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingCafe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorRecoveryCode",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecretKey",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorRecoveryCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecretKey",
                table: "Users");
        }
    }
}
