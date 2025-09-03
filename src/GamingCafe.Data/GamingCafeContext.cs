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

    // PS5 Integration
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
            CreatedAt = DateTime.UtcNow
        });

        // Seed default gaming stations
        modelBuilder.Entity<GameStation>().HasData(
            new GameStation { StationId = 1, StationName = "PC-01", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 2, StationName = "PC-02", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 3, StationName = "PC-03", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 4, StationName = "PC-04", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" },
            new GameStation { StationId = 5, StationName = "PC-05", StationType = "PC", HourlyRate = 5.00m, Description = "High-end Gaming PC" }
        );

        // Seed PS5 console
        modelBuilder.Entity<PS5Console>().HasData(new PS5Console
        {
            ConsoleId = 1,
            ConsoleName = "PS5-01",
            Status = ConsoleStatus.Offline,
            CreatedAt = DateTime.UtcNow
        });

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
            CreatedAt = DateTime.UtcNow
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
