using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GamingCafe.Core.Models;
using GamingCafe.Data.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace GamingCafe.Data.Services;

public class DatabaseSeeder
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(GamingCafeContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(bool isDevelopment = false)
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            _logger.LogInformation("Starting database seeding...");

            // Seed in order due to foreign key dependencies
            await SeedLoyaltyProgramsAsync();
            await SeedUsersAsync(isDevelopment);
            await SeedGameStationsAsync();
            await SeedGameConsolesAsync();
            await SeedProductsAsync();
            
            if (isDevelopment)
            {
                await SeedDevelopmentDataAsync();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedLoyaltyProgramsAsync()
    {
        if (await _context.LoyaltyPrograms.AnyAsync())
        {
            _logger.LogInformation("Loyalty programs already exist, skipping seeding");
            return;
        }

        var loyaltyPrograms = new[]
        {
            new LoyaltyProgram
            {
                ProgramName = "Gaming Enthusiast",
                Description = "Basic loyalty program for all gamers",
                PointsPerDollar = 1.0m,
                RedemptionRate = 0.01m, // 1 point = $0.01
                MinimumSpend = 0m,
                BonusThreshold = 100m,
                BonusMultiplier = 1.5m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LoyaltyProgram
            {
                ProgramName = "Pro Gamer",
                Description = "Premium loyalty program for frequent players",
                PointsPerDollar = 1.5m,
                RedemptionRate = 0.012m, // 1 point = $0.012
                MinimumSpend = 500m,
                BonusThreshold = 200m,
                BonusMultiplier = 2.0m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LoyaltyProgram
            {
                ProgramName = "Gaming Legend",
                Description = "Exclusive loyalty program for VIP members",
                PointsPerDollar = 2.0m,
                RedemptionRate = 0.015m, // 1 point = $0.015
                MinimumSpend = 1000m,
                BonusThreshold = 300m,
                BonusMultiplier = 2.5m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.LoyaltyPrograms.AddRangeAsync(loyaltyPrograms);
        _logger.LogInformation("Seeded {Count} loyalty programs", loyaltyPrograms.Length);
    }

    private async Task SeedUsersAsync(bool isDevelopment)
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Users already exist, skipping seeding");
            return;
        }

        var defaultLoyaltyProgram = await _context.LoyaltyPrograms.FirstAsync();

        var users = new List<User>
        {
            // Admin user
            new User
            {
                Username = "admin",
                Email = "admin@gamingcafe.com",
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = BC.HashPassword("admin123"),
                Role = "Admin",
                IsActive = true,
                IsEmailVerified = true,
                WalletBalance = 0m,
                LoyaltyPoints = 0,
                LoyaltyProgramId = defaultLoyaltyProgram.ProgramId,
                CreatedAt = DateTime.UtcNow
            },
            // Manager user
            new User
            {
                Username = "manager",
                Email = "manager@gamingcafe.com",
                FirstName = "John",
                LastName = "Manager",
                PasswordHash = BC.HashPassword("manager123"),
                Role = "Manager",
                IsActive = true,
                IsEmailVerified = true,
                WalletBalance = 0m,
                LoyaltyPoints = 0,
                LoyaltyProgramId = defaultLoyaltyProgram.ProgramId,
                CreatedAt = DateTime.UtcNow
            },
            // Staff user
            new User
            {
                Username = "staff",
                Email = "staff@gamingcafe.com",
                FirstName = "Jane",
                LastName = "Staff",
                PasswordHash = BC.HashPassword("staff123"),
                Role = "Staff",
                IsActive = true,
                IsEmailVerified = true,
                WalletBalance = 0m,
                LoyaltyPoints = 0,
                LoyaltyProgramId = defaultLoyaltyProgram.ProgramId,
                CreatedAt = DateTime.UtcNow
            }
        };

        if (isDevelopment)
        {
            // Add demo customer users
            var demoUsers = new[]
            {
                new User
                {
                    Username = "demo_user1",
                    Email = "demo1@example.com",
                    FirstName = "Alice",
                    LastName = "Johnson",
                    PasswordHash = BC.HashPassword("password123"),
                    Role = "Customer",
                    IsActive = true,
                    IsEmailVerified = true,
                    WalletBalance = 50.00m,
                    LoyaltyPoints = 150,
                    LoyaltyProgramId = defaultLoyaltyProgram.ProgramId,
                    DateOfBirth = DateTime.Today.AddYears(-25),
                    PhoneNumber = "+1234567890",
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new User
                {
                    Username = "demo_user2",
                    Email = "demo2@example.com",
                    FirstName = "Bob",
                    LastName = "Smith",
                    PasswordHash = BC.HashPassword("password123"),
                    Role = "Customer",
                    IsActive = true,
                    IsEmailVerified = true,
                    WalletBalance = 25.50m,
                    LoyaltyPoints = 75,
                    LoyaltyProgramId = defaultLoyaltyProgram.ProgramId,
                    DateOfBirth = DateTime.Today.AddYears(-22),
                    PhoneNumber = "+1234567891",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                }
            };

            users.AddRange(demoUsers);
        }

        await _context.Users.AddRangeAsync(users);
        _logger.LogInformation("Seeded {Count} users", users.Count);
    }

    private async Task SeedGameStationsAsync()
    {
        if (await _context.GameStations.AnyAsync())
        {
            _logger.LogInformation("Game stations already exist, skipping seeding");
            return;
        }

        var gameStations = new[]
        {
            new GameStation
            {
                StationName = "Gaming PC #1",
                StationType = "PC",
                Description = "High-end gaming PC with RTX 4080",
                HourlyRate = 8.00m,
                IsActive = true,
                IsAvailable = true,
                MaxSessionDuration = TimeSpan.FromHours(8),
                Notes = "Equipped with latest games and VR support",
                CreatedAt = DateTime.UtcNow
            },
            new GameStation
            {
                StationName = "Gaming PC #2",
                StationType = "PC",
                Description = "Mid-range gaming PC with RTX 4060",
                HourlyRate = 6.00m,
                IsActive = true,
                IsAvailable = true,
                MaxSessionDuration = TimeSpan.FromHours(8),
                Notes = "Great for most modern games",
                CreatedAt = DateTime.UtcNow
            },
            new GameStation
            {
                StationName = "PlayStation 5 #1",
                StationType = "Console",
                Description = "Sony PlayStation 5 console",
                HourlyRate = 10.00m,
                IsActive = true,
                IsAvailable = true,
                MaxSessionDuration = TimeSpan.FromHours(6),
                Notes = "Includes latest PS5 exclusive games",
                CreatedAt = DateTime.UtcNow
            },
            new GameStation
            {
                StationName = "Xbox Series X #1",
                StationType = "Console",
                Description = "Microsoft Xbox Series X console",
                HourlyRate = 10.00m,
                IsActive = true,
                IsAvailable = true,
                MaxSessionDuration = TimeSpan.FromHours(6),
                Notes = "Game Pass Ultimate included",
                CreatedAt = DateTime.UtcNow
            },
            new GameStation
            {
                StationName = "Nintendo Switch #1",
                StationType = "Console",
                Description = "Nintendo Switch console",
                HourlyRate = 7.00m,
                IsActive = true,
                IsAvailable = true,
                MaxSessionDuration = TimeSpan.FromHours(4),
                Notes = "Perfect for family gaming",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.GameStations.AddRangeAsync(gameStations);
        _logger.LogInformation("Seeded {Count} game stations", gameStations.Length);
    }

    private async Task SeedGameConsolesAsync()
    {
        if (await _context.GameConsoles.AnyAsync())
        {
            _logger.LogInformation("Game consoles already exist, skipping seeding");
            return;
        }

        var gameConsoles = new[]
        {
            new GameConsole
            {
                ConsoleName = "PlayStation 5 - Station 1",
                Type = ConsoleType.PlayStation,
                Model = "PS5 Standard",
                SerialNumber = "PS5-001-2024",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IPAddress = "192.168.1.101",
                MACAddress = "AA:BB:CC:DD:EE:01",
                FirmwareVersion = "6.02",
                StorageCapacity = 825,
                UsedStorage = 200,
                LastOnline = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new GameConsole
            {
                ConsoleName = "Xbox Series X - Station 1",
                Type = ConsoleType.Xbox,
                Model = "Xbox Series X",
                SerialNumber = "XBX-001-2024",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IPAddress = "192.168.1.102",
                MACAddress = "AA:BB:CC:DD:EE:02",
                FirmwareVersion = "10.0.22000",
                StorageCapacity = 1000,
                UsedStorage = 350,
                LastOnline = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new GameConsole
            {
                ConsoleName = "Nintendo Switch - Station 1",
                Type = ConsoleType.Nintendo,
                Model = "Nintendo Switch OLED",
                SerialNumber = "NSW-001-2024",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IPAddress = "192.168.1.103",
                MACAddress = "AA:BB:CC:DD:EE:03",
                FirmwareVersion = "16.0.3",
                StorageCapacity = 64,
                UsedStorage = 32,
                LastOnline = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.GameConsoles.AddRangeAsync(gameConsoles);
        _logger.LogInformation("Seeded {Count} game consoles", gameConsoles.Length);
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync())
        {
            _logger.LogInformation("Products already exist, skipping seeding");
            return;
        }

        var products = new[]
        {
            // Snacks
            new Product
            {
                ProductName = "Energy Drink",
                Description = "High-caffeine energy drink for gamers",
                Price = 3.50m,
                StockQuantity = 50,
                Category = "Beverages",
                SKU = "BEV-001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "Gaming Chips",
                Description = "Crispy potato chips - gamer's favorite",
                Price = 2.25m,
                StockQuantity = 30,
                Category = "Snacks",
                SKU = "SNK-001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "Protein Bar",
                Description = "High-protein energy bar for sustained gaming",
                Price = 4.00m,
                StockQuantity = 25,
                Category = "Snacks",
                SKU = "SNK-002",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            // Gaming Accessories
            new Product
            {
                ProductName = "Gaming Mouse Pad",
                Description = "Large RGB gaming mouse pad",
                Price = 25.99m,
                StockQuantity = 15,
                Category = "Accessories",
                SKU = "ACC-001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "Gaming Headset",
                Description = "7.1 Surround sound gaming headset",
                Price = 79.99m,
                StockQuantity = 8,
                Category = "Accessories",
                SKU = "ACC-002",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "Mechanical Keyboard",
                Description = "RGB mechanical gaming keyboard",
                Price = 129.99m,
                StockQuantity = 5,
                Category = "Accessories",
                SKU = "ACC-003",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            // Gift Cards
            new Product
            {
                ProductName = "$10 Gaming Card",
                Description = "Gaming cafe credit card worth $10",
                Price = 10.00m,
                StockQuantity = 100,
                Category = "Gift Cards",
                SKU = "GC-010",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                ProductName = "$25 Gaming Card",
                Description = "Gaming cafe credit card worth $25",
                Price = 25.00m,
                StockQuantity = 100,
                Category = "Gift Cards",
                SKU = "GC-025",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Products.AddRangeAsync(products);
        _logger.LogInformation("Seeded {Count} products", products.Length);
    }

    private async Task SeedDevelopmentDataAsync()
    {
        _logger.LogInformation("Seeding development-specific data...");

        // Add some sample transactions
        var users = await _context.Users.Where(u => u.Role == "Customer").ToListAsync();
        if (users.Any())
        {
            var transactions = new List<Transaction>();
            var random = new Random();

            foreach (var user in users)
            {
                // Add some wallet transactions
                for (int i = 0; i < 3; i++)
                {
                    transactions.Add(new Transaction
                    {
                        UserId = user.UserId,
                        Amount = Math.Round((decimal)(random.NextDouble() * 50 + 10), 2),
                        Type = TransactionType.WalletDeposit,
                        Description = "Wallet top-up",
                        PaymentMethod = "Card",
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    });
                }

                // Add some gaming session transactions
                for (int i = 0; i < 2; i++)
                {
                    transactions.Add(new Transaction
                    {
                        UserId = user.UserId,
                        Amount = Math.Round((decimal)(random.NextDouble() * 30 + 5), 2),
                        Type = TransactionType.SessionPayment,
                        Description = "Gaming session payment",
                        PaymentMethod = "Wallet",
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 15))
                    });
                }
            }

            await _context.Transactions.AddRangeAsync(transactions);
        }

        // Add some sample reservations
        var stations = await _context.GameStations.Take(3).ToListAsync();
        if (users.Any() && stations.Any())
        {
            var reservations = new List<Reservation>();
            var random = new Random();

            foreach (var user in users.Take(2))
            {
                var station = stations[random.Next(stations.Count)];
                var reservationDate = DateTime.Today.AddDays(random.Next(1, 7));
                var startTime = new DateTime(reservationDate.Year, reservationDate.Month, reservationDate.Day, 
                    10 + random.Next(8), 0, 0);
                var endTime = startTime.AddHours(random.Next(1, 4));

                reservations.Add(new Reservation
                {
                    UserId = user.UserId,
                    StationId = station.StationId,
                    ReservationDate = reservationDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Status = ReservationStatus.Confirmed,
                    EstimatedCost = (decimal)(endTime - startTime).TotalHours * station.HourlyRate,
                    Notes = "Demo reservation",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 5))
                });
            }

            await _context.Reservations.AddRangeAsync(reservations);
        }

        _logger.LogInformation("Development data seeding completed");
    }

    public async Task<bool> IsDatabaseSeededAsync()
    {
        return await _context.Users.AnyAsync() && 
               await _context.GameStations.AnyAsync() && 
               await _context.Products.AnyAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        _logger.LogWarning("Resetting database - all data will be lost!");
        
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        
        _logger.LogInformation("Database reset completed");
    }
}
