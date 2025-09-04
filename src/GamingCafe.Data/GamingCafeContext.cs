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

    // POS & Inventory
    public DbSet<Product> Products { get; set; }

    // Financial
    public DbSet<Transaction> Transactions { get; set; }

    // Loyalty Program
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
    public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }
    public DbSet<LoyaltyRedemption> LoyaltyRedemptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
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
            entity.Property(e => e.WalletBalance).HasColumnType("decimal(18,2)");
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

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed UserRole enum values (for reference)
        // The actual seeding will be done through DatabaseSeeder service
    }
}
