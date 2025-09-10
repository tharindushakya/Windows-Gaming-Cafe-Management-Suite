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

    // Console Management
    public DbSet<GameConsole> GameConsoles { get; set; }
    public DbSet<ConsoleSession> ConsoleSessions { get; set; }
    public DbSet<ConsoleRemoteCommand> ConsoleRemoteCommands { get; set; }
    public DbSet<ConsoleGame> ConsoleGames { get; set; }

    // POS & Inventory
    public DbSet<Product> Products { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }

    // Financial
    public DbSet<Transaction> Transactions { get; set; }
      public DbSet<Wallet> Wallets { get; set; }
      public DbSet<WalletTransaction> WalletTransactions { get; set; }
      // Idempotency keys for wallet-affecting endpoints
      public DbSet<GamingCafe.Core.Models.IdempotencyKey> IdempotencyKeys { get; set; }

    // Loyalty Program
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
    public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }
    public DbSet<LoyaltyRedemption> LoyaltyRedemptions { get; set; }

    // Audit Logging
    public DbSet<AuditLog> AuditLogs { get; set; }
    
      // Outbox for reliable messaging
      public DbSet<GamingCafe.Core.Models.OutboxMessage> OutboxMessages { get; set; }
      // Refresh tokens
      public DbSet<GamingCafe.Core.Models.RefreshToken> RefreshTokens { get; set; }

      // Scheduled jobs for optional persistent scheduling
      public DbSet<GamingCafe.Core.Models.ScheduledJob> ScheduledJobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      base.OnConfiguring(optionsBuilder);
      // NOTE: Interceptors are registered via DI when the DbContext is configured in Program.cs.
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            // Legacy per-user wallet balance column remains for compatibility with older migrations.
            // New canonical wallet model is represented by the Wallet entity.
#pragma warning disable CS0618 // Obsolete: mapping legacy User.WalletBalance kept for compatibility
            entity.Property(e => e.WalletBalance).HasColumnType("decimal(18,2)");
#pragma warning restore CS0618
            entity.Property(e => e.Role).HasConversion<int>();

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // GameStation Configuration
        modelBuilder.Entity<GameStation>(entity =>
        {
            entity.HasKey(e => e.StationId);
            entity.Property(e => e.StationName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StationType).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Processor).HasMaxLength(100);
            entity.Property(e => e.GraphicsCard).HasMaxLength(100);
            entity.Property(e => e.Memory).HasMaxLength(50);
            entity.Property(e => e.Storage).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(15);
            entity.Property(e => e.MacAddress).HasMaxLength(17);

            entity.HasOne(e => e.CurrentUser)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GameConsole Configuration
        modelBuilder.Entity<GameConsole>(entity =>
        {
            entity.HasKey(e => e.ConsoleId);
            entity.Property(e => e.ConsoleName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(15);
            entity.Property(e => e.MacAddress).HasMaxLength(17);
            entity.Property(e => e.SerialNumber).HasMaxLength(50);
            entity.Property(e => e.FirmwareVersion).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CurrentGame).HasMaxLength(100);
            entity.Property(e => e.ControllerSettings).HasMaxLength(200);
            entity.Property(e => e.DisplaySettings).HasMaxLength(200);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.CurrentUser)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // GameSession Configuration
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(e => e.GameSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Station)
                  .WithMany(e => e.GameSessions)
                  .HasForeignKey(e => e.StationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Reservation Configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(e => e.Reservations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Station)
                  .WithMany(e => e.Reservations)
                  .HasForeignKey(e => e.StationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category).HasMaxLength(50);
        });

        // Transaction Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(e => e.Transactions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LoyaltyProgram Configuration
        modelBuilder.Entity<LoyaltyProgram>(entity =>
        {
            entity.HasKey(e => e.ProgramId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RedemptionValue).HasColumnType("decimal(18,4)");
        });

        // LoyaltyReward Configuration
        modelBuilder.Entity<LoyaltyReward>(entity =>
        {
            entity.HasKey(e => e.RewardId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(200);

            entity.HasOne(e => e.Program)
                  .WithMany(e => e.Rewards)
                  .HasForeignKey(e => e.ProgramId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // LoyaltyRedemption Configuration
        modelBuilder.Entity<LoyaltyRedemption>(entity =>
        {
            entity.HasKey(e => e.RedemptionId);
            entity.Property(e => e.Description).HasMaxLength(200);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Reward)
                  .WithMany(e => e.Redemptions)
                  .HasForeignKey(e => e.RewardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // InventoryMovement Configuration
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Supplier).HasMaxLength(100);
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Type).HasConversion<int>();

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ConsoleSession Configuration
        modelBuilder.Entity<ConsoleSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Console)
                  .WithMany()
                  .HasForeignKey(e => e.ConsoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Game)
                  .WithMany()
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ConsoleRemoteCommand Configuration
        modelBuilder.Entity<ConsoleRemoteCommand>(entity =>
        {
            entity.HasKey(e => e.CommandId);
            entity.Property(e => e.Command).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Parameters).HasMaxLength(500);
            entity.Property(e => e.Response).HasMaxLength(1000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();

            entity.HasOne(e => e.Console)
                  .WithMany()
                  .HasForeignKey(e => e.ConsoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                  .WithMany()
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ConsoleGame Configuration
        modelBuilder.Entity<ConsoleGame>(entity =>
        {
            entity.HasKey(e => e.GameId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Platform).HasMaxLength(100);
            entity.Property(e => e.Genre).HasMaxLength(50);
            entity.Property(e => e.Rating).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GameTitle).HasMaxLength(200);
            entity.Property(e => e.SizeGB).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Publisher).HasMaxLength(100);
            entity.Property(e => e.Developer).HasMaxLength(100);
        });

        // TODO: Add InventoryMovement configuration back after resolving foreign key conflicts
        /*
        // InventoryMovement Configuration
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Supplier).HasMaxLength(100);
            entity.Property(e => e.BatchNumber).HasMaxLength(100);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Creator)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        */

        // Audit Log Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Details).HasColumnType("text");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

            // IdempotencyKey configuration
            modelBuilder.Entity<GamingCafe.Core.Models.IdempotencyKey>(entity =>
            {
                  entity.HasKey(e => e.Key);
                  entity.Property(e => e.Key).HasMaxLength(200);
                  entity.HasIndex(e => e.Key).IsUnique();
                  entity.Property(e => e.RequestHash).HasMaxLength(200);
                  entity.Property(e => e.Endpoint).HasMaxLength(200);
            });

            // RefreshToken Configuration
            modelBuilder.Entity<GamingCafe.Core.Models.RefreshToken>(entity =>
            {
                  entity.HasKey(e => e.TokenId);
                  entity.Property(e => e.TokenHash).IsRequired();
                  entity.Property(e => e.DeviceInfo).HasMaxLength(200);
                  entity.Property(e => e.IpAddress).HasMaxLength(45);

                  entity.HasOne(e => e.User)
                          .WithMany(u => u.RefreshTokens)
                          .HasForeignKey(e => e.UserId)
                          .OnDelete(DeleteBehavior.Cascade);

                  entity.HasIndex(e => e.TokenHash).IsUnique();
                  entity.HasIndex(e => e.UserId);
            });

                  // ScheduledJob Configuration
                  modelBuilder.Entity<GamingCafe.Core.Models.ScheduledJob>(entity =>
                  {
                        entity.HasKey(e => e.JobId);
                        entity.Property(e => e.PayloadType).HasMaxLength(200);
                        entity.Property(e => e.CreatedAt).IsRequired();
                        entity.Property(e => e.ScheduledAt).IsRequired();
                        entity.Property(e => e.Processed).IsRequired();
                        entity.HasIndex(e => e.ScheduledAt);
                  });

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed UserRole enum values (for reference)
        // The actual seeding will be done through DatabaseSeeder service
    }
}
