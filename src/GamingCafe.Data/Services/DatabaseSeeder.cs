using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;
using BCrypt.Net;

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

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            await SeedUsersAsync();
            await SeedGameStationsAsync();
            await SeedProductsAsync();
            await SeedLoyaltyProgramsAsync();
            await SeedGameConsolesAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    private async Task SeedUsersAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Users already exist, skipping user seeding");
            return;
        }

        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                Email = "admin@gamingcafe.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true,
                WalletBalance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "staff1",
                Email = "staff1@gamingcafe.com",
                FirstName = "Staff",
                LastName = "One",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                Role = UserRole.Staff,
                IsActive = true,
                WalletBalance = 500.00m,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "customer1",
                Email = "customer1@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                IsActive = true,
                WalletBalance = 50.00m,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "customer2",
                Email = "customer2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                IsActive = true,
                WalletBalance = 75.00m,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);
        _logger.LogInformation("Seeded {Count} users", users.Count);
    }

    private async Task SeedGameStationsAsync()
    {
        if (await _context.GameStations.AnyAsync())
        {
            _logger.LogInformation("Game stations already exist, skipping station seeding");
            return;
        }

        var stations = new List<GameStation>
        {
            new GameStation
            {
                StationName = "Gaming Station 01",
                StationType = "Gaming PC",
                HourlyRate = 5.00m,
                IsActive = true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                Location = "Main Floor",
                Processor = "Intel Core i7-12700K",
                GraphicsCard = "NVIDIA RTX 4070",
                Memory = "32GB DDR4",
                Storage = "1TB NVMe SSD"
            },
            new GameStation
            {
                StationName = "Gaming Station 02",
                StationType = "Gaming PC",
                HourlyRate = 5.00m,
                IsActive = true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                Location = "Main Floor",
                Processor = "Intel Core i7-12700K",
                GraphicsCard = "NVIDIA RTX 4070",
                Memory = "32GB DDR4",
                Storage = "1TB NVMe SSD"
            },
            new GameStation
            {
                StationName = "VIP Station 01",
                StationType = "VIP Gaming PC",
                HourlyRate = 8.00m,
                IsActive = true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                Location = "VIP Area",
                Processor = "Intel Core i9-13900K",
                GraphicsCard = "NVIDIA RTX 4090",
                Memory = "64GB DDR5",
                Storage = "2TB NVMe SSD"
            },
            new GameStation
            {
                StationName = "Console Station 01",
                StationType = "Console Gaming",
                HourlyRate = 6.00m,
                IsActive = true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                Location = "Console Area"
            },
            new GameStation
            {
                StationName = "Console Station 02",
                StationType = "Console Gaming",
                HourlyRate = 6.00m,
                IsActive = true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow,
                Location = "Console Area"
            }
        };

        await _context.GameStations.AddRangeAsync(stations);
        _logger.LogInformation("Seeded {Count} game stations", stations.Count);
    }

    private async Task SeedProductsAsync()
    {
        if (await _context.Products.AnyAsync())
        {
            _logger.LogInformation("Products already exist, skipping product seeding");
            return;
        }

        var products = new List<Product>
        {
            new Product
            {
                Name = "Energy Drink",
                Category = "Beverages",
                Price = 3.50m,
                StockQuantity = 100,
                MinStockLevel = 20,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Gaming Headset",
                Category = "Accessories",
                Price = 79.99m,
                StockQuantity = 25,
                MinStockLevel = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Sandwich",
                Category = "Food",
                Price = 6.50m,
                StockQuantity = 50,
                MinStockLevel = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Gaming Mouse",
                Category = "Accessories",
                Price = 45.00m,
                StockQuantity = 15,
                MinStockLevel = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "Coffee",
                Category = "Beverages",
                Price = 2.50m,
                StockQuantity = 200,
                MinStockLevel = 50,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Products.AddRangeAsync(products);
        _logger.LogInformation("Seeded {Count} products", products.Count);
    }

    private async Task SeedLoyaltyProgramsAsync()
    {
        if (await _context.LoyaltyPrograms.AnyAsync())
        {
            _logger.LogInformation("Loyalty programs already exist, skipping loyalty program seeding");
            return;
        }

        var loyaltyPrograms = new List<LoyaltyProgram>
        {
            new LoyaltyProgram
            {
                Name = "Bronze Membership",
                Description = "Basic membership with no additional benefits",
                PointsPerDollar = 1,
                MinPointsToRedeem = 100,
                RedemptionValue = 0.01m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LoyaltyProgram
            {
                Name = "Silver Membership",
                Description = "5% discount on all purchases and priority booking",
                PointsPerDollar = 2,
                MinPointsToRedeem = 500,
                RedemptionValue = 0.015m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LoyaltyProgram
            {
                Name = "Gold Membership",
                Description = "10% discount, priority booking, and free drink per session",
                PointsPerDollar = 3,
                MinPointsToRedeem = 1500,
                RedemptionValue = 0.02m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new LoyaltyProgram
            {
                Name = "Platinum Membership",
                Description = "15% discount, VIP access, free snacks, and extended gaming hours",
                PointsPerDollar = 5,
                MinPointsToRedeem = 3000,
                RedemptionValue = 0.025m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.LoyaltyPrograms.AddRangeAsync(loyaltyPrograms);
        _logger.LogInformation("Seeded {Count} loyalty programs", loyaltyPrograms.Count);
    }

    private async Task SeedGameConsolesAsync()
    {
        if (await _context.GameConsoles.AnyAsync())
        {
            _logger.LogInformation("Game consoles already exist, skipping console seeding");
            return;
        }

        var consoles = new List<GameConsole>
        {
            new GameConsole
            {
                ConsoleName = "PlayStation 5 #1",
                Type = ConsoleType.PlayStation5,
                Model = "PS5 Standard",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IsActive = true,
                HourlyRate = 6.00m,
                CreatedAt = DateTime.UtcNow
            },
            new GameConsole
            {
                ConsoleName = "Xbox Series X #1",
                Type = ConsoleType.XboxSeriesX,
                Model = "Xbox Series X",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IsActive = true,
                HourlyRate = 6.00m,
                CreatedAt = DateTime.UtcNow
            },
            new GameConsole
            {
                ConsoleName = "Nintendo Switch #1",
                Type = ConsoleType.NintendoSwitch,
                Model = "Switch OLED",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IsActive = true,
                HourlyRate = 5.00m,
                CreatedAt = DateTime.UtcNow
            },
            new GameConsole
            {
                ConsoleName = "PlayStation 4 #1",
                Type = ConsoleType.PlayStation4,
                Model = "PS4 Pro",
                Status = ConsoleStatus.Online,
                IsAvailable = true,
                IsActive = true,
                HourlyRate = 4.00m,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.GameConsoles.AddRangeAsync(consoles);
        _logger.LogInformation("Seeded {Count} game consoles", consoles.Count);
    }
}