using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GamingCafe.Core.Models;
using GamingCafe.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace GamingCafe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IUnitOfWork unitOfWork, ILogger<ReportsController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard overview statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            var users = await _unitOfWork.Repository<User>().GetAllAsync();
            var sessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var transactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var stations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            var reservations = await _unitOfWork.Repository<Reservation>().GetAllAsync();

            var periodSessions = sessions.Where(s => s.StartTime >= start && s.StartTime < end);
            var periodTransactions = transactions.Where(t => t.CreatedAt >= start && t.CreatedAt < end);
            var periodReservations = reservations.Where(r => r.ReservationDate >= start && r.ReservationDate < end);

            var activeUsers = users.Count(u => u.IsActive);
            var totalStations = stations.Count();
            var activeStations = stations.Count(s => s.IsActive);
            var availableStations = stations.Count(s => s.IsActive && s.IsAvailable);

            var stats = new DashboardStatsDto
            {
                // User Statistics
                TotalUsers = users.Count(),
                ActiveUsers = activeUsers,
                NewUsersThisPeriod = users.Count(u => u.CreatedAt >= start && u.CreatedAt < end),

                // Station Statistics
                TotalStations = totalStations,
                ActiveStations = activeStations,
                AvailableStations = availableStations,
                StationUtilization = activeStations > 0 ? (decimal)(activeStations - availableStations) / activeStations * 100 : 0,

                // Session Statistics
                TotalSessions = periodSessions.Count(),
                ActiveSessions = sessions.Count(s => s.Status == SessionStatus.Active),
                CompletedSessions = periodSessions.Count(s => s.Status == SessionStatus.Completed),
                AverageSessionDuration = periodSessions.Where(s => s.Duration.HasValue)
                    .Average(s => s.Duration!.Value.TotalMinutes),

                // Financial Statistics
                TotalRevenue = periodTransactions.Where(t => t.Status == TransactionStatus.Completed && t.Type != TransactionType.Refund)
                    .Sum(t => t.Amount),
                PendingPayments = transactions.Where(t => t.Status == TransactionStatus.Pending)
                    .Sum(t => t.Amount),
                RefundsIssued = periodTransactions.Where(t => t.Type == TransactionType.Refund)
                    .Sum(t => t.Amount),

                // Reservation Statistics
                TotalReservations = periodReservations.Count(),
                ConfirmedReservations = periodReservations.Count(r => r.Status == ReservationStatus.Confirmed),
                CancelledReservations = periodReservations.Count(r => r.Status == ReservationStatus.Cancelled),

                // Period
                StartDate = start,
                EndDate = end,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard stats");
            return StatusCode(500, "An error occurred while generating dashboard statistics");
        }
    }

    /// <summary>
    /// Get revenue report
    /// </summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> GetRevenueReport([FromQuery] GetRevenueReportRequest request)
    {
        try
        {
            var start = request.StartDate;
            var end = request.EndDate;

            var transactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();
            var periodTransactions = transactions.Where(t => 
                t.CreatedAt >= start && 
                t.CreatedAt < end && 
                t.Status == TransactionStatus.Completed);

            var revenueByDay = periodTransactions
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    TotalRevenue = g.Where(t => t.Type != TransactionType.Refund).Sum(t => t.Amount),
                    GameTimeRevenue = g.Where(t => t.Type == TransactionType.GameTime).Sum(t => t.Amount),
                    ProductRevenue = g.Where(t => t.Type == TransactionType.Product).Sum(t => t.Amount),
                    RefundsIssued = g.Where(t => t.Type == TransactionType.Refund).Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            var revenueByType = periodTransactions
                .Where(t => t.Type != TransactionType.Refund)
                .GroupBy(t => t.Type)
                .Select(g => new RevenueByTypeDto
                {
                    Type = g.Key.ToString(),
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = 0 // Will calculate below
                })
                .ToList();

            var totalRevenue = revenueByType.Sum(r => r.Amount);
            foreach (var item in revenueByType)
            {
                item.Percentage = totalRevenue > 0 ? (item.Amount / totalRevenue) * 100 : 0;
            }

            var report = new RevenueReportDto
            {
                StartDate = start,
                EndDate = end,
                TotalRevenue = totalRevenue,
                TotalTransactions = periodTransactions.Count(),
                AverageTransactionValue = periodTransactions.Any() ? periodTransactions.Average(t => t.Amount) : 0,
                DailyRevenue = revenueByDay,
                RevenueByType = revenueByType,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            return StatusCode(500, "An error occurred while generating the revenue report");
        }
    }

    /// <summary>
    /// Get usage report for stations and sessions
    /// </summary>
    [HttpGet("usage")]
    public async Task<ActionResult<UsageReportDto>> GetUsageReport([FromQuery] GetUsageReportRequest request)
    {
        try
        {
            var start = request.StartDate;
            var end = request.EndDate;

            var sessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var stations = await _unitOfWork.Repository<GameStation>().GetAllAsync();
            
            var periodSessions = sessions.Where(s => s.StartTime >= start && s.StartTime < end);

            var stationUsage = stations.Select(station =>
            {
                var stationSessions = periodSessions.Where(s => s.StationId == station.StationId);
                var totalDuration = stationSessions.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalHours);
                var sessionCount = stationSessions.Count();
                var revenue = stationSessions.Sum(s => s.TotalCost);

                return new StationUsageDto
                {
                    StationId = station.StationId,
                    StationName = station.StationName,
                    StationType = station.StationType,
                    SessionCount = sessionCount,
                    TotalHours = totalDuration,
                    Revenue = revenue,
                    UtilizationRate = CalculateUtilizationRate(totalDuration, start, end),
                    AverageSessionDuration = sessionCount > 0 ? totalDuration / sessionCount : 0
                };
            }).OrderByDescending(s => s.Revenue).ToList();

            var hourlyUsage = Enumerable.Range(0, 24).Select(hour =>
            {
                var hourSessions = periodSessions.Where(s => s.StartTime.Hour == hour);
                return new HourlyUsageDto
                {
                    Hour = hour,
                    SessionCount = hourSessions.Count(),
                    Revenue = hourSessions.Sum(s => s.TotalCost)
                };
            }).ToList();

            var dailyUsage = periodSessions
                .GroupBy(s => s.StartTime.Date)
                .Select(g => new DailyUsageDto
                {
                    Date = g.Key,
                    SessionCount = g.Count(),
                    UniqueUsers = g.Select(s => s.UserId).Distinct().Count(),
                    TotalHours = g.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalHours),
                    Revenue = g.Sum(s => s.TotalCost)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var report = new UsageReportDto
            {
                StartDate = start,
                EndDate = end,
                TotalSessions = periodSessions.Count(),
                TotalHours = periodSessions.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalHours),
                UniqueUsers = periodSessions.Select(s => s.UserId).Distinct().Count(),
                AverageSessionDuration = periodSessions.Where(s => s.Duration.HasValue).Any() 
                    ? periodSessions.Where(s => s.Duration.HasValue).Average(s => s.Duration!.Value.TotalMinutes) 
                    : 0,
                StationUsage = stationUsage,
                HourlyUsage = hourlyUsage,
                DailyUsage = dailyUsage,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating usage report");
            return StatusCode(500, "An error occurred while generating the usage report");
        }
    }

    /// <summary>
    /// Get user analytics report
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<UserAnalyticsDto>> GetUserAnalytics([FromQuery] GetUserAnalyticsRequest request)
    {
        try
        {
            var start = request.StartDate;
            var end = request.EndDate;

            var users = await _unitOfWork.Repository<User>().GetAllAsync();
            var sessions = await _unitOfWork.Repository<GameSession>().GetAllAsync();
            var transactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();

            var periodSessions = sessions.Where(s => s.StartTime >= start && s.StartTime < end);
            var periodTransactions = transactions.Where(t => t.CreatedAt >= start && t.CreatedAt < end);

            var topUsers = users.Select(user =>
            {
                var userSessions = periodSessions.Where(s => s.UserId == user.UserId);
                var userTransactions = periodTransactions.Where(t => t.UserId == user.UserId && t.Status == TransactionStatus.Completed);

                return new TopUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    SessionCount = userSessions.Count(),
                    TotalHours = userSessions.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalHours),
                    TotalSpent = userTransactions.Where(t => t.Type != TransactionType.Refund).Sum(t => t.Amount),
                    LoyaltyPoints = user.LoyaltyPoints,
                    LastActivity = userSessions.Any() ? userSessions.Max(s => s.StartTime) : user.CreatedAt
                };
            })
            .Where(u => u.SessionCount > 0 || u.TotalSpent > 0)
            .OrderByDescending(u => u.TotalSpent)
            .Take(request.TopCount)
            .ToList();

            var newUsersByDay = users
                .Where(u => u.CreatedAt >= start && u.CreatedAt < end)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new NewUsersDto
                {
                    Date = g.Key,
                    NewUsers = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            var userRetention = CalculateUserRetention(users, sessions, start, end);

            var analytics = new UserAnalyticsDto
            {
                StartDate = start,
                EndDate = end,
                TotalUsers = users.Count(),
                ActiveUsers = periodSessions.Select(s => s.UserId).Distinct().Count(),
                NewUsers = users.Count(u => u.CreatedAt >= start && u.CreatedAt < end),
                RetentionRate = userRetention,
                AverageSessionsPerUser = periodSessions.Any() ? 
                    (double)periodSessions.Count() / periodSessions.Select(s => s.UserId).Distinct().Count() : 0,
                AverageRevenuePerUser = topUsers.Any() ? topUsers.Average(u => u.TotalSpent) : 0,
                TopUsers = topUsers,
                NewUsersByDay = newUsersByDay,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user analytics");
            return StatusCode(500, "An error occurred while generating user analytics");
        }
    }

    /// <summary>
    /// Get inventory report
    /// </summary>
    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport()
    {
        try
        {
            var products = await _unitOfWork.Repository<Product>().GetAllAsync();
            var transactions = await _unitOfWork.Repository<Transaction>().GetAllAsync();

            var lowStockProducts = products
                .Where(p => p.StockQuantity <= p.MinStockLevel && p.IsActive)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Category = p.Category,
                    CurrentStock = p.StockQuantity,
                    LowStockThreshold = p.MinStockLevel,
                    UnitPrice = p.Price,
                    Status = p.StockQuantity == 0 ? "Out of Stock" : "Low Stock"
                })
                .OrderBy(p => p.CurrentStock)
                .ToList();

            var topSellingProducts = products
                .Select(p =>
                {
                    var productTransactions = transactions.Where(t => 
                        t.Type == TransactionType.Product && 
                        t.Description.Contains(p.Name) && 
                        t.Status == TransactionStatus.Completed);

                    return new TopSellingProductDto
                    {
                        ProductId = p.ProductId,
                        ProductName = p.Name,
                        Category = p.Category,
                        UnitsSold = productTransactions.Count(), // Simplified - should track quantities
                        Revenue = productTransactions.Sum(t => t.Amount),
                        CurrentStock = p.StockQuantity
                    };
                })
                .Where(p => p.UnitsSold > 0)
                .OrderByDescending(p => p.Revenue)
                .Take(20)
                .ToList();

            var categoryStats = products
                .GroupBy(p => p.Category)
                .Select(g => new CategoryStatsDto
                {
                    Category = g.Key,
                    ProductCount = g.Count(),
                    TotalValue = g.Sum(p => p.StockQuantity * p.Price),
                    LowStockCount = g.Count(p => p.StockQuantity <= p.MinStockLevel),
                    OutOfStockCount = g.Count(p => p.StockQuantity == 0)
                })
                .OrderByDescending(c => c.TotalValue)
                .ToList();

            var report = new InventoryReportDto
            {
                TotalProducts = products.Count(),
                ActiveProducts = products.Count(p => p.IsActive),
                LowStockCount = lowStockProducts.Count,
                OutOfStockCount = products.Count(p => p.StockQuantity == 0),
                TotalInventoryValue = products.Sum(p => p.StockQuantity * p.Price),
                LowStockProducts = lowStockProducts,
                TopSellingProducts = topSellingProducts,
                CategoryStats = categoryStats,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory report");
            return StatusCode(500, "An error occurred while generating the inventory report");
        }
    }

    private static double CalculateUtilizationRate(double totalHours, DateTime start, DateTime end)
    {
        var totalPossibleHours = (end - start).TotalHours;
        return totalPossibleHours > 0 ? (totalHours / totalPossibleHours) * 100 : 0;
    }

    private static double CalculateUserRetention(IEnumerable<User> users, IEnumerable<GameSession> sessions, DateTime start, DateTime end)
    {
        // Simplified retention calculation - users who had sessions in both halves of the period
        var midPoint = start.AddDays((end - start).TotalDays / 2);
        var firstHalfUsers = sessions.Where(s => s.StartTime >= start && s.StartTime < midPoint).Select(s => s.UserId).Distinct();
        var secondHalfUsers = sessions.Where(s => s.StartTime >= midPoint && s.StartTime < end).Select(s => s.UserId).Distinct();
        var retainedUsers = firstHalfUsers.Intersect(secondHalfUsers).Count();
        
        return firstHalfUsers.Any() ? (double)retainedUsers / firstHalfUsers.Count() * 100 : 0;
    }
}

// DTOs and Request/Response Models
public class DashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisPeriod { get; set; }
    public int TotalStations { get; set; }
    public int ActiveStations { get; set; }
    public int AvailableStations { get; set; }
    public decimal StationUtilization { get; set; }
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int CompletedSessions { get; set; }
    public double AverageSessionDuration { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingPayments { get; set; }
    public decimal RefundsIssued { get; set; }
    public int TotalReservations { get; set; }
    public int ConfirmedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class RevenueReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<RevenueByTypeDto> RevenueByType { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal GameTimeRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal RefundsIssued { get; set; }
    public int TransactionCount { get; set; }
}

public class RevenueByTypeDto
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class UsageReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSessions { get; set; }
    public double TotalHours { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageSessionDuration { get; set; }
    public List<StationUsageDto> StationUsage { get; set; } = new();
    public List<HourlyUsageDto> HourlyUsage { get; set; } = new();
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class StationUsageDto
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public double TotalHours { get; set; }
    public decimal Revenue { get; set; }
    public double UtilizationRate { get; set; }
    public double AverageSessionDuration { get; set; }
}

public class HourlyUsageDto
{
    public int Hour { get; set; }
    public int SessionCount { get; set; }
    public decimal Revenue { get; set; }
}

public class DailyUsageDto
{
    public DateTime Date { get; set; }
    public int SessionCount { get; set; }
    public int UniqueUsers { get; set; }
    public double TotalHours { get; set; }
    public decimal Revenue { get; set; }
}

public class UserAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsers { get; set; }
    public double RetentionRate { get; set; }
    public double AverageSessionsPerUser { get; set; }
    public decimal AverageRevenuePerUser { get; set; }
    public List<TopUserDto> TopUsers { get; set; } = new();
    public List<NewUsersDto> NewUsersByDay { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class TopUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public double TotalHours { get; set; }
    public decimal TotalSpent { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTime LastActivity { get; set; }
}

public class NewUsersDto
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
}

public class InventoryReportDto
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<CategoryStatsDto> CategoryStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public int CurrentStock { get; set; }
}

public class CategoryStatsDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}

public class GetRevenueReportRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class GetUsageReportRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class GetUserAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(1, 100)]
    public int TopCount { get; set; } = 10;
}
