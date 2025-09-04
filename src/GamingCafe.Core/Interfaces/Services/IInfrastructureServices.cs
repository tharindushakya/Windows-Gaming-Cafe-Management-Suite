using GamingCafe.Core.Models;
using GamingCafe.Core.DTOs;

namespace GamingCafe.Core.Interfaces.Services;

public interface IValidationService
{
    // Entity validation
    ValidationResult ValidateUser(User user);
    ValidationResult ValidateGameSession(GameSession session);
    ValidationResult ValidateReservation(Reservation reservation);
    ValidationResult ValidateTransaction(Transaction transaction);

    // Field validation
    ValidationResult ValidateEmail(string email);
    ValidationResult ValidatePassword(string password);
    Task<ValidationResult> ValidateUniqueUsernameAsync(string username, int? excludeUserId = null);
    Task<ValidationResult> ValidateUniqueEmailAsync(string email, int? excludeUserId = null);
}

public interface ICacheService
{
    // Basic cache operations
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);

    // Advanced cache operations
    Task RemoveByPatternAsync(string pattern);
    Task ClearAllAsync();
    Task<string> GetCacheHealthStatusAsync();
}

public interface IFileUploadService
{
    // File operations
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<Stream> DownloadFileAsync(string fileUrl);
    Task<bool> FileExistsAsync(string fileUrl);
    Task<long> GetFileSizeAsync(string fileUrl);
}

public interface INotificationService
{
    // Push notifications
    Task<bool> SendNotificationAsync(int userId, string title, string message);
    Task<bool> SendBroadcastNotificationAsync(string title, string message);
    Task<IEnumerable<object>> GetUserNotificationsAsync(int userId, int page, int pageSize);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<int> GetUnreadNotificationCountAsync(int userId);
}

public interface IAuditService
{
    // Audit logging
    Task LogActionAsync(string action, int? userId, string? details = null);
    Task LogEntityChangeAsync(string entityType, int entityId, string action, object? oldValues = null, object? newValues = null);
    Task<IEnumerable<object>> GetAuditLogsAsync(int page, int pageSize, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<object>> GetEntityAuditLogsAsync(string entityType, int entityId);
}

public interface IHealthCheckService
{
    // System health monitoring
    Task<object> GetSystemHealthAsync();
    Task<bool> CheckDatabaseHealthAsync();
    Task<bool> CheckCacheHealthAsync();
    Task<bool> CheckExternalServicesHealthAsync();
}

public interface ITwoFactorService
{
    // Two-Factor Authentication
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(int userId, string password);
    Task<bool> VerifyTwoFactorAsync(int userId, string code);
    Task<bool> VerifyRecoveryCodeAsync(int userId, string recoveryCode);
    Task<bool> DisableTwoFactorAsync(int userId, string password);
    Task<TwoFactorRecoveryCodesResponse> GenerateNewRecoveryCodesAsync(int userId);
    Task<bool> IsTwoFactorEnabledAsync(int userId);
    string GenerateQrCodeDataUrl(string email, string secretKey);
}

public interface IBackupService
{
    // Database backup and restore
    Task<bool> CreateBackupAsync(string backupName);
    Task<bool> RestoreBackupAsync(string backupName);
    Task<IEnumerable<object>> GetAvailableBackupsAsync();
    Task<bool> DeleteBackupAsync(string backupName);
    Task<bool> ScheduleBackupAsync(TimeSpan interval);
    Task CreateScheduledBackupAsync();
}
