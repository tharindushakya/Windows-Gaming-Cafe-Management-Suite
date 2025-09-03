using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingCafe.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDynamicDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 1,
                column: "InstallDate",
                value: new DateTime(2024, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 2,
                column: "InstallDate",
                value: new DateTime(2024, 12, 5, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 3,
                column: "InstallDate",
                value: new DateTime(2024, 12, 10, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 4,
                column: "InstallDate",
                value: new DateTime(2024, 11, 25, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 5,
                column: "InstallDate",
                value: new DateTime(2024, 11, 20, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 6,
                column: "InstallDate",
                value: new DateTime(2024, 11, 10, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 163, DateTimeKind.Utc).AddTicks(3356));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 163, DateTimeKind.Utc).AddTicks(5006));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 163, DateTimeKind.Utc).AddTicks(5010));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 163, DateTimeKind.Utc).AddTicks(5011));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 163, DateTimeKind.Utc).AddTicks(5012));

            migrationBuilder.UpdateData(
                table: "LoyaltyPrograms",
                keyColumn: "ProgramId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "PS5Consoles",
                keyColumn: "ConsoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 165, DateTimeKind.Utc).AddTicks(530));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 165, DateTimeKind.Utc).AddTicks(2101));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 165, DateTimeKind.Utc).AddTicks(2105));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 20, 48, 165, DateTimeKind.Utc).AddTicks(2106));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 1,
                column: "InstallDate",
                value: new DateTime(2025, 8, 4, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2478));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 2,
                column: "InstallDate",
                value: new DateTime(2025, 8, 9, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2787));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 3,
                column: "InstallDate",
                value: new DateTime(2025, 8, 14, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2792));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 4,
                column: "InstallDate",
                value: new DateTime(2025, 7, 30, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2794));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 5,
                column: "InstallDate",
                value: new DateTime(2025, 7, 25, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2796));

            migrationBuilder.UpdateData(
                table: "ConsoleGames",
                keyColumn: "GameId",
                keyValue: 6,
                column: "InstallDate",
                value: new DateTime(2025, 7, 15, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2798));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9456));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9846));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9848));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9851));

            migrationBuilder.UpdateData(
                table: "GameConsoles",
                keyColumn: "ConsoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9853));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(3069));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4206));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4210));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4211));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4212));

            migrationBuilder.UpdateData(
                table: "LoyaltyPrograms",
                keyColumn: "ProgramId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(5293));

            migrationBuilder.UpdateData(
                table: "PS5Consoles",
                keyColumn: "ConsoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(5659));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(6525));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8056));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8060));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8061));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 17, 28, 708, DateTimeKind.Utc).AddTicks(7058));
        }
    }
}
