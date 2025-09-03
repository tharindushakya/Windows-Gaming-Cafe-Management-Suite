using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingCafe.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultDateTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 27, DateTimeKind.Utc).AddTicks(7127));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 27, DateTimeKind.Utc).AddTicks(8307));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 27, DateTimeKind.Utc).AddTicks(8310));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 27, DateTimeKind.Utc).AddTicks(8312));

            migrationBuilder.UpdateData(
                table: "GameStations",
                keyColumn: "StationId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 27, DateTimeKind.Utc).AddTicks(8313));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 28, DateTimeKind.Utc).AddTicks(9941));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 29, DateTimeKind.Utc).AddTicks(1487));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 29, DateTimeKind.Utc).AddTicks(1491));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 3, 10, 23, 3, 29, DateTimeKind.Utc).AddTicks(1493));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
