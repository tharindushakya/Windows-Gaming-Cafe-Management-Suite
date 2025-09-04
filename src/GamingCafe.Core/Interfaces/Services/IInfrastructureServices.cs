using System.Linq.Expressions;

namespace GamingCafe.Core.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true);
    Task<bool> SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName, bool isHtml = true);
    Task<bool> SendPasswordResetEmailAsync(string to, string resetLink);
    Task<bool> SendEmailConfirmationAsync(string to, string confirmationLink);
    Task<bool> SendWelcomeEmailAsync(string to, string userName);
    Task<bool> SendSessionReminderAsync(string to, string userName, DateTime sessionTime);
    Task<bool> SendLowStockAlertAsync(string to, IEnumerable<string> lowStockItems);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<string?> GetStringAsync(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<bool> ExistsAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task ClearAllAsync();
    Task<IEnumerable<string>> GetKeysAsync(string pattern = "*");
}

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<string> UploadImageAsync(Stream imageStream, string fileName, int? maxWidth = null, int? maxHeight = null);
    Task<Stream> DownloadFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task<FileInfo> GetFileInfoAsync(string filePath);
    Task<IEnumerable<FileInfo>> ListFilesAsync(string? directory = null);
    Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiration = null);
}

public interface IBackgroundJobService
{
    string Enqueue(Expression<Func<Task>> methodCall);
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
    string ScheduleRecurring(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
    string ScheduleRecurring<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);
    bool Delete(string jobId);
    bool DeleteRecurring(string jobId);
    JobStatus GetJobStatus(string jobId);
}

public interface INotificationService
{
    // Real-time notifications
    Task SendToUserAsync(int userId, string message, NotificationType type = NotificationType.Info);
    Task SendToUsersAsync(IEnumerable<int> userIds, string message, NotificationType type = NotificationType.Info);
    Task SendToRoleAsync(string role, string message, NotificationType type = NotificationType.Info);
    Task SendToAllAsync(string message, NotificationType type = NotificationType.Info);

    // System notifications
    Task NotifySessionStartedAsync(int userId, int stationId);
    Task NotifySessionEndedAsync(int userId, decimal cost);
    Task NotifyReservationReminderAsync(int userId, int reservationId);
    Task NotifyLowStockAsync(string productName, int quantity);
    Task NotifyPaymentFailedAsync(int userId, decimal amount);
    Task NotifyMaintenanceScheduledAsync(int stationId, DateTime scheduledDate);

    // Notification history
    Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetUserNotificationsAsync(int userId, int page, int pageSize);
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, int? entityId, string? oldValues, string? newValues, int? userId = null);
    Task LogLoginAsync(int userId, string ipAddress, bool success);
    Task LogLogoutAsync(int userId);
    Task LogPasswordChangeAsync(int userId);
    Task LogPermissionChangeAsync(int userId, string permission, bool granted);
    Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(int page, int pageSize, AuditFilter? filter = null);
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityType, int entityId);
}

public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckDatabaseAsync();
    Task<HealthCheckResult> CheckCacheAsync();
    Task<HealthCheckResult> CheckEmailServiceAsync();
    Task<HealthCheckResult> CheckFileStorageAsync();
    Task<HealthCheckResult> CheckPaymentGatewayAsync();
    Task<HealthCheckResult> CheckExternalServicesAsync();
    Task<SystemHealthStatus> GetOverallHealthAsync();
}

public interface IValidationService
{
    ValidationResult ValidateUser(User user);
    ValidationResult ValidateGameSession(GameSession session);
    ValidationResult ValidateReservation(Reservation reservation);
    ValidationResult ValidateTransaction(Transaction transaction);
    ValidationResult ValidatePassword(string password);
    ValidationResult ValidateEmail(string email);
    Task<ValidationResult> ValidateUniqueUsernameAsync(string username, int? excludeUserId = null);
    Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null);
}

public interface ISearchService
{
    Task<SearchResult<User>> SearchUsersAsync(string query, int page = 1, int pageSize = 10);
    Task<SearchResult<GameStation>> SearchStationsAsync(string query, int page = 1, int pageSize = 10);
    Task<SearchResult<Product>> SearchProductsAsync(string query, int page = 1, int pageSize = 10);
    Task<SearchResult<T>> SearchAsync<T>(string query, int page = 1, int pageSize = 10) where T : class;
    Task IndexEntityAsync<T>(T entity) where T : class;
    Task UpdateIndexAsync<T>(T entity) where T : class;
    Task RemoveFromIndexAsync<T>(int entityId) where T : class;
    Task RebuildIndexAsync<T>() where T : class;
}

public interface IReportingService
{
    // Revenue reports
    Task<RevenueReport> GetRevenueReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<DailyRevenue>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<MonthlyRevenue>> GetMonthlyRevenueAsync(int year);

    // Usage reports
    Task<UsageReport> GetUsageReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<StationUsage>> GetStationUsageAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<UserActivity>> GetUserActivityAsync(DateTime startDate, DateTime endDate);

    // Product reports
    Task<ProductSalesReport> GetProductSalesReportAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<TopSellingProduct>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int count = 10);
    Task<InventoryReport> GetInventoryReportAsync();

    // Export functionality
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data);
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data);
    Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, string title);
}

// Supporting models and enums

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public enum JobStatus
{
    Enqueued,
    Processing,
    Succeeded,
    Failed,
    Deleted
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditFilter
{
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Action { get; set; }
}

public class HealthCheckResult
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class SystemHealthStatus
{
    public HealthStatus OverallStatus { get; set; }
    public IEnumerable<HealthCheckResult> Checks { get; set; } = new List<HealthCheckResult>();
    public DateTime CheckedAt { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class SearchResult<T>
{
    public IEnumerable<T> Results { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string Query { get; set; } = string.Empty;
}

public class FileInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

// Report models
public class RevenueReport
{
    public decimal TotalRevenue { get; set; }
    public decimal SessionRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class MonthlyRevenue
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class UsageReport
{
    public int TotalSessions { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public decimal AverageSessionDuration { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class StationUsage
{
    public int StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public TimeSpan TotalUsage { get; set; }
    public decimal UtilizationRate { get; set; }
}

public class UserActivity
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ProductSalesReport
{
    public decimal TotalSales { get; set; }
    public int TotalItemsSold { get; set; }
    public int UniqueProductsSold { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class TopSellingProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class InventoryReport
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public DateTime GeneratedAt { get; set; }
}
