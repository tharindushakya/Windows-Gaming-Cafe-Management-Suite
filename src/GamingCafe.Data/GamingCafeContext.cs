using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;

namespace GamingCafe.Data;

public class GamingCafeContext : DbContext
{
    public GamingCafeContext(DbContextOptions<GamingCafeContext> options) : base(options)
    {
    }

    // User Management
    public DbSet<User> Users { get; set; }

    // Gaming Stations & Sessions
    public DbSet<GameStation> GameStations { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    // Console Integration - Modern Multi-Platform Support
    public DbSet<GameConsole> GameConsoles { get; set; }
    public DbSet<ConsoleSession> ConsoleSessions { get; set; }
    public DbSet<ConsoleRemoteCommand> ConsoleRemoteCommands { get; set; }
    public DbSet<ConsoleGame> ConsoleGames { get; set; }

    // Legacy PS5 Integration (for backward compatibility)
    public DbSet<PS5Console> PS5Consoles { get; set; }
    public DbSet<PS5Session> PS5Sessions { get; set; }
    public DbSet<PS5RemoteCommand> PS5RemoteCommands { get; set; }

    // POS & Inventory
    public DbSet<Product> Products { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }

    // Financial
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    // Loyalty Program
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
    public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }
    public DbSet<LoyaltyRedemption> LoyaltyRedemptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Suppress the pending model changes warning caused by seeded data
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.WalletBalance).HasColumnType("decimal(18,2)");
        });

        // Game Station Configuration
        modelBuilder.Entity<GameStation>(entity =>
        {
            entity.HasKey(e => e.StationId);
            entity.HasIndex(e => e.StationName).IsUnique();
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.CurrentUser)
                .WithMany()
                .HasForeignKey(e => e.CurrentUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Game Session Configuration
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.User)
                .WithMany(u => u.GameSessions)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Station)
                .WithMany(s => s.GameSessions)
                .HasForeignKey(e => e.StationId);
        });

        // Reservation Configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId);
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Station)
                .WithMany(s => s.Reservations)
                .HasForeignKey(e => e.StationId);
        });

        // PS5 Console Configuration
        modelBuilder.Entity<PS5Console>(entity =>
        {
            entity.HasKey(e => e.ConsoleId);
            entity.HasIndex(e => e.ConsoleName).IsUnique();
            entity.HasOne(e => e.CurrentUser)
                .WithMany()
                .HasForeignKey(e => e.CurrentUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PS5 Session Configuration
        modelBuilder.Entity<PS5Session>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Console)
                .WithMany(c => c.Sessions)
                .HasForeignKey(e => e.ConsoleId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.PS5Sessions)
                .HasForeignKey(e => e.UserId);
        });

        // PS5 Remote Command Configuration
        modelBuilder.Entity<PS5RemoteCommand>(entity =>
        {
            entity.HasKey(e => e.CommandId);
            entity.HasOne(e => e.Console)
                .WithMany()
                .HasForeignKey(e => e.ConsoleId);
        });

        // Modern Game Console Configuration
        modelBuilder.Entity<GameConsole>(entity =>
        {
            entity.HasKey(e => e.ConsoleId);
            entity.HasIndex(e => e.ConsoleName).IsUnique();
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.CurrentUser)
                .WithMany()
                .HasForeignKey(e => e.CurrentUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Console Session Configuration
        modelBuilder.Entity<ConsoleSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Console)
                .WithMany(c => c.Sessions)
                .HasForeignKey(e => e.ConsoleId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });

        // Console Remote Command Configuration
        modelBuilder.Entity<ConsoleRemoteCommand>(entity =>
        {
            entity.HasKey(e => e.CommandId);
            entity.HasOne(e => e.Console)
                .WithMany(c => c.RemoteCommands)
                .HasForeignKey(e => e.ConsoleId);
        });

        // Console Game Configuration
        modelBuilder.Entity<ConsoleGame>(entity =>
        {
            entity.HasKey(e => e.GameId);
            entity.Property(e => e.SizeGB).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DownloadProgress).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Console)
                .WithMany(c => c.InstalledGames)
                .HasForeignKey(e => e.ConsoleId);
        });

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
        });

        // Inventory Movement Configuration
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.InventoryMovements)
                .HasForeignKey(e => e.ProductId);
        });

        // Transaction Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Transactions)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Transactions)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Wallet Transaction Configuration
        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.User)
                .WithMany(u => u.WalletTransactions)
                .HasForeignKey(e => e.UserId);
        });

        // Loyalty Program Configuration
        modelBuilder.Entity<LoyaltyProgram>(entity =>
        {
            entity.HasKey(e => e.ProgramId);
            entity.Property(e => e.RedemptionValue).HasColumnType("decimal(18,4)");
        });

        // Loyalty Reward Configuration
        modelBuilder.Entity<LoyaltyReward>(entity =>
        {
            entity.HasKey(e => e.RewardId);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
            entity.HasOne(e => e.Program)
                .WithMany(p => p.Rewards)
                .HasForeignKey(e => e.ProgramId);
        });

        // Loyalty Redemption Configuration
        modelBuilder.Entity<LoyaltyRedemption>(entity =>
        {
            entity.HasKey(e => e.RedemptionId);
            entity.HasOne(e => e.User)
                .WithMany(u => u.LoyaltyRedemptions)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Reward)
                .WithMany(r => r.Redemptions)
                .HasForeignKey(e => e.RewardId);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            UserId = 1,
            Username = "admin",
            Email = "admin@gamingcafe.local",
            PasswordHash = "$2a$11$3rKvv5rUZ5g5zKq7hNjY9.nGxQZRqJXs3YTvFZdXmXPQs4BNt5DQi", // password: admin123
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed default gaming stations
        modelBuilder.Entity<GameStation>().HasData(
            new GameStation { StationId = 1, StationName = "PC-01", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 2, StationName = "PC-02", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 3, StationName = "PC-03", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 4, StationName = "PC-04", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 5, StationName = "PC-05", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" }
        );

        // Seed PS5 console (legacy)
        modelBuilder.Entity<PS5Console>().HasData(new PS5Console
        {
            ConsoleId = 1,
            ConsoleName = "PS5-01",
            Status = ConsoleStatus.Offline,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed modern gaming consoles
        modelBuilder.Entity<GameConsole>().HasData(
            new GameConsole
            {
                ConsoleId = 1,
                ConsoleName = "PlayStation5-01",
                Type = ConsoleType.PlayStation5,
                Model = "PS5 Standard",
                FirmwareVersion = "8.03",
                HourlyRate = 8.00m,
                IsAvailable = true,
                Status = ConsoleStatus.Offline,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Main PlayStation 5 console with 4K gaming"
            },
            new GameConsole
            {
                ConsoleId = 2,
                ConsoleName = "XboxSeriesX-01",
                Type = ConsoleType.XboxSeriesX,
                Model = "Xbox Series X",
                FirmwareVersion = "10.0.25398",
                HourlyRate = 8.00m,
                IsAvailable = true,
                Status = ConsoleStatus.Offline,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Xbox Series X with Game Pass Ultimate"
            },
            new GameConsole
            {
                ConsoleId = 3,
                ConsoleName = "NintendoSwitch-01",
                Type = ConsoleType.NintendoSwitchOLED,
                Model = "Switch OLED",
                FirmwareVersion = "16.1.0",
                HourlyRate = 6.00m,
                IsAvailable = true,
                Status = ConsoleStatus.Offline,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Nintendo Switch OLED with dock for TV play"
            },
            new GameConsole
            {
                ConsoleId = 4,
                ConsoleName = "PlayStation4-01",
                Type = ConsoleType.PlayStation4,
                Model = "PS4 Pro",
                FirmwareVersion = "11.00",
                HourlyRate = 5.00m,
                IsAvailable = true,
                Status = ConsoleStatus.Offline,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "PlayStation 4 Pro for budget gaming"
            },
            new GameConsole
            {
                ConsoleId = 5,
                ConsoleName = "SteamDeck-01",
                Type = ConsoleType.SteamDeck,
                Model = "Steam Deck 512GB",
                FirmwareVersion = "3.5.7",
                HourlyRate = 7.00m,
                IsAvailable = true,
                Status = ConsoleStatus.Offline,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Notes = "Portable PC gaming with Steam library"
            }
        );

        // Seed sample console games
        modelBuilder.Entity<ConsoleGame>().HasData(
            // PlayStation 5 Games
            new ConsoleGame
            {
                GameId = 1,
                ConsoleId = 1,
                GameTitle = "Spider-Man 2",
                Genre = "Action/Adventure",
                Rating = "T",
                SizeGB = 98.5m,
                Publisher = "Sony Interactive Entertainment",
                Developer = "Insomniac Games",
                Description = "Swing through NYC as Spider-Man in this thrilling adventure",
                InstallDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ConsoleGame
            {
                GameId = 2,
                ConsoleId = 1,
                GameTitle = "God of War Ragnarök",
                Genre = "Action/Adventure",
                Rating = "M",
                SizeGB = 90.6m,
                Publisher = "Sony Interactive Entertainment",
                Developer = "Santa Monica Studio",
                Description = "Epic conclusion to the Norse saga of Kratos and Atreus",
                InstallDate = new DateTime(2024, 12, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            // Xbox Series X Games
            new ConsoleGame
            {
                GameId = 3,
                ConsoleId = 2,
                GameTitle = "Halo Infinite",
                Genre = "FPS",
                Rating = "T",
                SizeGB = 48.4m,
                Publisher = "Microsoft Studios",
                Developer = "343 Industries",
                Description = "Master Chief returns in this sci-fi shooter",
                InstallDate = new DateTime(2024, 12, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new ConsoleGame
            {
                GameId = 4,
                ConsoleId = 2,
                GameTitle = "Forza Horizon 5",
                Genre = "Racing",
                Rating = "E",
                SizeGB = 103.0m,
                Publisher = "Microsoft Studios",
                Developer = "Playground Games",
                Description = "Open-world racing across beautiful Mexico",
                InstallDate = new DateTime(2024, 11, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            // Nintendo Switch Games
            new ConsoleGame
            {
                GameId = 5,
                ConsoleId = 3,
                GameTitle = "The Legend of Zelda: Tears of the Kingdom",
                Genre = "Action/Adventure",
                Rating = "E10+",
                SizeGB = 18.2m,
                Publisher = "Nintendo",
                Developer = "Nintendo EPD",
                Description = "Epic adventure in the skies and depths of Hyrule",
                InstallDate = new DateTime(2024, 11, 20, 0, 0, 0, DateTimeKind.Utc)
            },
            new ConsoleGame
            {
                GameId = 6,
                ConsoleId = 3,
                GameTitle = "Super Mario Odyssey",
                Genre = "Platformer",
                Rating = "E10+",
                SizeGB = 5.7m,
                Publisher = "Nintendo",
                Developer = "Nintendo EPD",
                Description = "Join Mario on a 3D platforming adventure across kingdoms",
                InstallDate = new DateTime(2024, 11, 10, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default loyalty program
        modelBuilder.Entity<LoyaltyProgram>().HasData(new LoyaltyProgram
        {
            ProgramId = 1,
            Name = "Gaming Café Rewards",
            Description = "Earn points for every dollar spent and redeem for gaming time or products",
            PointsPerDollar = 1,
            MinPointsToRedeem = 100,
            RedemptionValue = 0.01m,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed some sample products
        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, Name = "Energy Drink", Category = "Beverages", SKU = "BEV001", Price = 3.50m, Cost = 1.50m, StockQuantity = 50 },
            new Product { ProductId = 2, Name = "Gaming Headset", Category = "Accessories", SKU = "ACC001", Price = 79.99m, Cost = 40.00m, StockQuantity = 10 },
            new Product { ProductId = 3, Name = "Mechanical Keyboard", Category = "Accessories", SKU = "ACC002", Price = 129.99m, Cost = 70.00m, StockQuantity = 5 },
            new Product { ProductId = 4, Name = "Snack Pack", Category = "Food", SKU = "FOD001", Price = 5.99m, Cost = 2.50m, StockQuantity = 25 }
        );
    }
}
