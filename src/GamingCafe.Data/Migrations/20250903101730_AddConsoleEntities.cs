using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GamingCafe.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsoleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                columns: table => new
                {
                    ProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PointsPerDollar = table.Column<int>(type: "int", nullable: false),
                    MinPointsToRedeem = table.Column<int>(type: "int", nullable: false),
                    RedemptionValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.ProgramId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    MinStockLevel = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoyaltyPointsEarned = table.Column<int>(type: "int", nullable: false),
                    LoyaltyPointsRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    WalletBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoyaltyPoints = table.Column<int>(type: "int", nullable: false),
                    MembershipExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    RewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PointsRequired = table.Column<int>(type: "int", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "int", nullable: true),
                    CurrentRedemptions = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.RewardId);
                    table.ForeignKey(
                        name: "FK_LoyaltyRewards_LoyaltyPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "LoyaltyPrograms",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    MovementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.MovementId);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameConsoles",
                columns: table => new
                {
                    ConsoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LastPingAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentUserId = table.Column<int>(type: "int", nullable: true),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentGame = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    AllowGameDownloads = table.Column<bool>(type: "bit", nullable: false),
                    ParentalControlsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ControllerSettings = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplaySettings = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameConsoles", x => x.ConsoleId);
                    table.ForeignKey(
                        name: "FK_GameConsoles_Users_CurrentUserId",
                        column: x => x.CurrentUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GameStations",
                columns: table => new
                {
                    StationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Processor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GraphicsCard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Memory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Storage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    CurrentUserId = table.Column<int>(type: "int", nullable: true),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStations", x => x.StationId);
                    table.ForeignKey(
                        name: "FK_GameStations_Users_CurrentUserId",
                        column: x => x.CurrentUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PS5Consoles",
                columns: table => new
                {
                    ConsoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LastPingAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentUserId = table.Column<int>(type: "int", nullable: true),
                    SessionStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentGame = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PS5Consoles", x => x.ConsoleId);
                    table.ForeignKey(
                        name: "FK_PS5Consoles_Users_CurrentUserId",
                        column: x => x.CurrentUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.WalletTransactionId);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRedemptions",
                columns: table => new
                {
                    RedemptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RewardId = table.Column<int>(type: "int", nullable: false),
                    PointsUsed = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRedemptions", x => x.RedemptionId);
                    table.ForeignKey(
                        name: "FK_LoyaltyRedemptions_LoyaltyRewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "LoyaltyRewards",
                        principalColumn: "RewardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyRedemptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsoleGames",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleId = table.Column<int>(type: "int", nullable: false),
                    GameTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SizeGB = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    InstallDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPlayed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInstalled = table.Column<bool>(type: "bit", nullable: false),
                    IsDownloading = table.Column<bool>(type: "bit", nullable: false),
                    DownloadProgress = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GameImageUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Developer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleGames", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_ConsoleGames_GameConsoles_ConsoleId",
                        column: x => x.ConsoleId,
                        principalTable: "GameConsoles",
                        principalColumn: "ConsoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsoleRemoteCommands",
                columns: table => new
                {
                    CommandId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Command = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleRemoteCommands", x => x.CommandId);
                    table.ForeignKey(
                        name: "FK_ConsoleRemoteCommands_GameConsoles_ConsoleId",
                        column: x => x.ConsoleId,
                        principalTable: "GameConsoles",
                        principalColumn: "ConsoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsoleSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GameTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GameGenre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SessionData = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PlayersCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsoleSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_ConsoleSessions_GameConsoles_ConsoleId",
                        column: x => x.ConsoleId,
                        principalTable: "GameConsoles",
                        principalColumn: "ConsoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsoleSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_GameSessions_GameStations_StationId",
                        column: x => x.StationId,
                        principalTable: "GameStations",
                        principalColumn: "StationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    ReservationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.ReservationId);
                    table.ForeignKey(
                        name: "FK_Reservations_GameStations_StationId",
                        column: x => x.StationId,
                        principalTable: "GameStations",
                        principalColumn: "StationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PS5RemoteCommands",
                columns: table => new
                {
                    CommandId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Command = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PS5RemoteCommands", x => x.CommandId);
                    table.ForeignKey(
                        name: "FK_PS5RemoteCommands_PS5Consoles_ConsoleId",
                        column: x => x.ConsoleId,
                        principalTable: "PS5Consoles",
                        principalColumn: "ConsoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PS5Sessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GameTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PS5Sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_PS5Sessions_PS5Consoles_ConsoleId",
                        column: x => x.ConsoleId,
                        principalTable: "PS5Consoles",
                        principalColumn: "ConsoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PS5Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "GameConsoles",
                columns: new[] { "ConsoleId", "AllowGameDownloads", "ConsoleName", "ControllerSettings", "CreatedAt", "CurrentGame", "CurrentUserId", "DisplaySettings", "FirmwareVersion", "HourlyRate", "IpAddress", "IsAvailable", "IsOnline", "LastPingAt", "MacAddress", "Model", "Notes", "ParentalControlsEnabled", "SerialNumber", "SessionStartTime", "Status", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, "PlayStation5-01", "", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9456), "", null, "", "8.03", 8.00m, "", true, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "PS5 Standard", "Main PlayStation 5 console with 4K gaming", false, "", null, 0, 11, null },
                    { 2, true, "XboxSeriesX-01", "", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9846), "", null, "", "10.0.25398", 8.00m, "", true, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Xbox Series X", "Xbox Series X with Game Pass Ultimate", false, "", null, 0, 24, null },
                    { 3, true, "NintendoSwitch-01", "", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9848), "", null, "", "16.1.0", 6.00m, "", true, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Switch OLED", "Nintendo Switch OLED with dock for TV play", false, "", null, 0, 32, null },
                    { 4, true, "PlayStation4-01", "", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9851), "", null, "", "11.00", 5.00m, "", true, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "PS4 Pro", "PlayStation 4 Pro for budget gaming", false, "", null, 0, 10, null },
                    { 5, true, "SteamDeck-01", "", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(9853), "", null, "", "3.5.7", 7.00m, "", true, true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "Steam Deck 512GB", "Portable PC gaming with Steam library", false, "", null, 0, 41, null }
                });

            migrationBuilder.InsertData(
                table: "GameStations",
                columns: new[] { "StationId", "CreatedAt", "CurrentUserId", "Description", "GraphicsCard", "HourlyRate", "IpAddress", "IsActive", "IsAvailable", "MacAddress", "Memory", "Processor", "SessionStartTime", "StationName", "StationType", "Storage" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(3069), null, "High-end Gaming PC", "", 5.00m, "", true, true, "", "", "", null, "PC-01", "PC", "" },
                    { 2, new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4206), null, "High-end Gaming PC", "", 5.00m, "", true, true, "", "", "", null, "PC-02", "PC", "" },
                    { 3, new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4210), null, "High-end Gaming PC", "", 5.00m, "", true, true, "", "", "", null, "PC-03", "PC", "" },
                    { 4, new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4211), null, "High-end Gaming PC", "", 5.00m, "", true, true, "", "", "", null, "PC-04", "PC", "" },
                    { 5, new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(4212), null, "High-end Gaming PC", "", 5.00m, "", true, true, "", "", "", null, "PC-05", "PC", "" }
                });

            migrationBuilder.InsertData(
                table: "LoyaltyPrograms",
                columns: new[] { "ProgramId", "CreatedAt", "Description", "IsActive", "MinPointsToRedeem", "Name", "PointsPerDollar", "RedemptionValue" },
                values: new object[] { 1, new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(5293), "Earn points for every dollar spent and redeem for gaming time or products", true, 100, "Gaming Café Rewards", 1, 0.01m });

            migrationBuilder.InsertData(
                table: "PS5Consoles",
                columns: new[] { "ConsoleId", "ConsoleName", "CreatedAt", "CurrentGame", "CurrentUserId", "IpAddress", "IsAvailable", "LastPingAt", "MacAddress", "SerialNumber", "SessionStartTime", "Status", "UpdatedAt" },
                values: new object[] { 1, "PS5-01", new DateTime(2025, 9, 3, 10, 17, 28, 709, DateTimeKind.Utc).AddTicks(5659), "", null, "", true, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "", "", null, 0, null });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "Category", "Cost", "CreatedAt", "Description", "ImageUrl", "IsActive", "LoyaltyPointsEarned", "LoyaltyPointsRequired", "MinStockLevel", "Name", "Price", "SKU", "StockQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Beverages", 1.50m, new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(6525), "", "", true, 0, 0, 5, "Energy Drink", 3.50m, "BEV001", 50, null },
                    { 2, "Accessories", 40.00m, new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8056), "", "", true, 0, 0, 5, "Gaming Headset", 79.99m, "ACC001", 10, null },
                    { 3, "Accessories", 70.00m, new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8060), "", "", true, 0, 0, 5, "Mechanical Keyboard", 129.99m, "ACC002", 5, null },
                    { 4, "Food", 2.50m, new DateTime(2025, 9, 3, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(8061), "", "", true, 0, 0, 5, "Snack Pack", 5.99m, "FOD001", 25, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "DateOfBirth", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "LoyaltyPoints", "MembershipExpiryDate", "PasswordHash", "PhoneNumber", "Role", "Username", "WalletBalance" },
                values: new object[] { 1, new DateTime(2025, 9, 3, 10, 17, 28, 708, DateTimeKind.Utc).AddTicks(7058), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@gamingcafe.local", "System", true, null, "Administrator", 0, null, "$2a$11$3rKvv5rUZ5g5zKq7hNjY9.nGxQZRqJXs3YTvFZdXmXPQs4BNt5DQi", "", 3, "admin", 0m });

            migrationBuilder.InsertData(
                table: "ConsoleGames",
                columns: new[] { "GameId", "ConsoleId", "Description", "Developer", "DownloadProgress", "GameImageUrl", "GameTitle", "Genre", "InstallDate", "IsDownloading", "IsInstalled", "LastPlayed", "Publisher", "Rating", "ReleaseDate", "SizeGB" },
                values: new object[,]
                {
                    { 1, 1, "Swing through NYC as Spider-Man in this thrilling adventure", "Insomniac Games", 100.0m, "", "Spider-Man 2", "Action/Adventure", new DateTime(2025, 8, 4, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2478), false, true, null, "Sony Interactive Entertainment", "T", null, 98.5m },
                    { 2, 1, "Epic conclusion to the Norse saga of Kratos and Atreus", "Santa Monica Studio", 100.0m, "", "God of War Ragnarök", "Action/Adventure", new DateTime(2025, 8, 9, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2787), false, true, null, "Sony Interactive Entertainment", "M", null, 90.6m },
                    { 3, 2, "Master Chief returns in this sci-fi shooter", "343 Industries", 100.0m, "", "Halo Infinite", "FPS", new DateTime(2025, 8, 14, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2792), false, true, null, "Microsoft Studios", "T", null, 48.4m },
                    { 4, 2, "Open-world racing across beautiful Mexico", "Playground Games", 100.0m, "", "Forza Horizon 5", "Racing", new DateTime(2025, 7, 30, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2794), false, true, null, "Microsoft Studios", "E", null, 103.0m },
                    { 5, 3, "Epic adventure in the skies and depths of Hyrule", "Nintendo EPD", 100.0m, "", "The Legend of Zelda: Tears of the Kingdom", "Action/Adventure", new DateTime(2025, 7, 25, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2796), false, true, null, "Nintendo", "E10+", null, 18.2m },
                    { 6, 3, "Join Mario on a 3D platforming adventure across kingdoms", "Nintendo EPD", 100.0m, "", "Super Mario Odyssey", "Platformer", new DateTime(2025, 7, 15, 10, 17, 28, 710, DateTimeKind.Utc).AddTicks(2798), false, true, null, "Nintendo", "E10+", null, 5.7m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleGames_ConsoleId",
                table: "ConsoleGames",
                column: "ConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleRemoteCommands_ConsoleId",
                table: "ConsoleRemoteCommands",
                column: "ConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleSessions_ConsoleId",
                table: "ConsoleSessions",
                column: "ConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsoleSessions_UserId",
                table: "ConsoleSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameConsoles_ConsoleName",
                table: "GameConsoles",
                column: "ConsoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameConsoles_CurrentUserId",
                table: "GameConsoles",
                column: "CurrentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_StationId",
                table: "GameSessions",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_UserId",
                table: "GameSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStations_CurrentUserId",
                table: "GameStations",
                column: "CurrentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameStations_StationName",
                table: "GameStations",
                column: "StationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_RewardId",
                table: "LoyaltyRedemptions",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_UserId",
                table: "LoyaltyRedemptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_ProgramId",
                table: "LoyaltyRewards",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PS5Consoles_ConsoleName",
                table: "PS5Consoles",
                column: "ConsoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PS5Consoles_CurrentUserId",
                table: "PS5Consoles",
                column: "CurrentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PS5RemoteCommands_ConsoleId",
                table: "PS5RemoteCommands",
                column: "ConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PS5Sessions_ConsoleId",
                table: "PS5Sessions",
                column: "ConsoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PS5Sessions_UserId",
                table: "PS5Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_StationId",
                table: "Reservations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProductId",
                table: "Transactions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SessionId",
                table: "Transactions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId",
                table: "WalletTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsoleGames");

            migrationBuilder.DropTable(
                name: "ConsoleRemoteCommands");

            migrationBuilder.DropTable(
                name: "ConsoleSessions");

            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "LoyaltyRedemptions");

            migrationBuilder.DropTable(
                name: "PS5RemoteCommands");

            migrationBuilder.DropTable(
                name: "PS5Sessions");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "GameConsoles");

            migrationBuilder.DropTable(
                name: "LoyaltyRewards");

            migrationBuilder.DropTable(
                name: "PS5Consoles");

            migrationBuilder.DropTable(
                name: "GameSessions");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms");

            migrationBuilder.DropTable(
                name: "GameStations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
