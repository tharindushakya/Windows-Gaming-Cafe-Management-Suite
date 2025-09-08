using System.ComponentModel.DataAnnotations;

namespace GamingCafe.Core.DTOs;

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
    public int NewUsers { get; set; }
    public int ActiveUsers { get; set; }
    public List<TopUserDto> TopUsers { get; set; } = new();
    public List<NewUsersDto> NewUsersByPeriod { get; set; } = new();
    public double AverageSessionsPerUser { get; set; }
    public decimal AverageSpendingPerUser { get; set; }
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
    public DateTime LastVisit { get; set; }
}

public class NewUsersDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class InventoryReportDto
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<LowStockProductDto> LowStockItems { get; set; } = new();
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<CategoryStatsDto> CategoryStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public decimal Price { get; set; }
}

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Price { get; set; }
}

public class CategoryStatsDto
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class GetRevenueReportRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]  
    public DateTime EndDate { get; set; }

    public string? GroupBy { get; set; } = "day"; // day, week, month
}

public class GetUsageReportRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? StationId { get; set; }
    public string? GroupBy { get; set; } = "day"; // day, week, month
}

public class GetUserAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? TopUsersCount { get; set; } = 10;
    public string? SortBy { get; set; } = "spending"; // spending, sessions, hours
}
