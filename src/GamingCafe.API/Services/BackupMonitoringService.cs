using GamingCafe.Core.Configuration;
using GamingCafe.Core.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GamingCafe.API.Services;

public interface IBackupMonitoringService
{
    Task<BackupHealthStatus> GetBackupHealthStatusAsync();
    Task RecordBackupEventAsync(BackupEvent backupEvent);
    Task<IEnumerable<BackupMetrics>> GetBackupMetricsAsync(DateTime from, DateTime to);
    Task SendAlertAsync(BackupAlert alert);
    Task<bool> ValidateBackupIntegrityAsync(string backupFilePath);
}

public class BackupMonitoringService : IBackupMonitoringService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupMonitoringService> _logger;
    private readonly BackupSettings _backupSettings;
    private readonly string _metricsFilePath;
    private readonly string _eventsFilePath;

    public BackupMonitoringService(
        IConfiguration configuration,
        ILogger<BackupMonitoringService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _backupSettings = new BackupSettings();
        configuration.GetSection("BackupSettings").Bind(_backupSettings);
        
        var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "monitoring");
        Directory.CreateDirectory(dataDir);
        
        _metricsFilePath = Path.Combine(dataDir, "backup_metrics.json");
        _eventsFilePath = Path.Combine(dataDir, "backup_events.json");
    }

    public async Task<BackupHealthStatus> GetBackupHealthStatusAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving backup health status");

            var status = new BackupHealthStatus
            {
                StatusDate = DateTime.UtcNow,
                OverallHealth = BackupHealth.Healthy
            };

            // Check last backup time
            var lastBackupTime = await GetLastBackupTimeAsync();
            status.LastBackupTime = lastBackupTime;
            status.TimeSinceLastBackup = DateTime.UtcNow - lastBackupTime;

            // Determine health based on time since last backup
            var maxTimeBetweenBackups = TimeSpan.FromHours(_backupSettings.Monitoring.AlertThresholdHours);
            if (status.TimeSinceLastBackup > maxTimeBetweenBackups)
            {
                status.OverallHealth = BackupHealth.Warning;
                status.HealthIssues.Add($"Last backup was {status.TimeSinceLastBackup.TotalHours:F1} hours ago");
            }

            // Check backup directory space
            var spaceInfo = await CheckBackupDirectorySpaceAsync();
            status.AvailableStorageGB = spaceInfo.AvailableGB;
            status.UsedStorageGB = spaceInfo.UsedGB;

            if (spaceInfo.UsagePercentage > 85) // Default 85% threshold
            {
                status.OverallHealth = BackupHealth.Warning;
                status.HealthIssues.Add($"Storage usage at {spaceInfo.UsagePercentage:F1}%");
            }

            // Check recent backup failures
            var recentFailures = await GetRecentBackupFailuresAsync();
            status.RecentFailures = recentFailures.Count();
            
            if (status.RecentFailures > 3) // Default max 3 failures
            {
                status.OverallHealth = BackupHealth.Critical;
                status.HealthIssues.Add($"{status.RecentFailures} recent backup failures");
            }

            // Get backup metrics
            status.BackupMetrics = await GetRecentMetricsAsync();

            _logger.LogInformation("Backup health status: {Health}", status.OverallHealth);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup health status");
            return new BackupHealthStatus
            {
                OverallHealth = BackupHealth.Critical,
                StatusDate = DateTime.UtcNow,
                HealthIssues = { $"Error checking health: {ex.Message}" }
            };
        }
    }

    public async Task RecordBackupEventAsync(BackupEvent backupEvent)
    {
        try
        {
            var events = await LoadBackupEventsAsync();
            events.Add(backupEvent);

            // Keep only recent events (last 30 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            events = events.Where(e => e.Timestamp >= cutoffDate).ToList();

            await SaveBackupEventsAsync(events);

            _logger.LogInformation("Recorded backup event: {EventType} - {Status}", 
                backupEvent.EventType, backupEvent.Status);

            // Send alert if this is a failure
            if (backupEvent.Status == BackupEventStatus.Failed)
            {
                await SendAlertAsync(new BackupAlert
                {
                    AlertType = BackupAlertType.BackupFailure,
                    Message = $"Backup failed: {backupEvent.ErrorMessage}",
                    Severity = AlertSeverity.High,
                    BackupEvent = backupEvent
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording backup event");
        }
    }

    public async Task<IEnumerable<BackupMetrics>> GetBackupMetricsAsync(DateTime from, DateTime to)
    {
        try
        {
            var events = await LoadBackupEventsAsync();
            
            var metrics = events
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => new BackupMetrics
                {
                    Date = g.Key,
                    TotalBackups = g.Count(e => e.EventType == BackupEventType.BackupStarted),
                    SuccessfulBackups = g.Count(e => e.EventType == BackupEventType.BackupCompleted && e.Status == BackupEventStatus.Success),
                    FailedBackups = g.Count(e => e.EventType == BackupEventType.BackupCompleted && e.Status == BackupEventStatus.Failed),
                    AverageBackupDuration = g.Where(e => e.Duration.HasValue)
                                           .Select(e => e.Duration!.Value)
                                           .DefaultIfEmpty()
                                           .Average(ts => ts.TotalMinutes),
                    TotalBackupSizeGB = g.Where(e => e.BackupSizeBytes.HasValue)
                                       .Sum(e => e.BackupSizeBytes!.Value) / (1024.0 * 1024.0 * 1024.0)
                })
                .OrderBy(m => m.Date)
                .ToList();

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup metrics");
            return Enumerable.Empty<BackupMetrics>();
        }
    }

    public async Task SendAlertAsync(BackupAlert alert)
    {
        try
        {
            _logger.LogWarning("Backup Alert - {AlertType}: {Message}", alert.AlertType, alert.Message);

            // In a real implementation, you would:
            // 1. Send email notifications
            // 2. Send to monitoring systems (e.g., Slack, Teams, PagerDuty)
            // 3. Write to event logs
            // 4. Update dashboard systems

            // For now, we'll just log and store the alert
            var alertsDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "alerts");
            Directory.CreateDirectory(alertsDir);
            
            var alertFile = Path.Combine(alertsDir, $"alert_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            var alertJson = JsonSerializer.Serialize(alert, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(alertFile, alertJson);

            _logger.LogInformation("Alert saved to file: {AlertFile}", alertFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending backup alert");
        }
    }

    public async Task<bool> ValidateBackupIntegrityAsync(string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                _logger.LogWarning("Backup file not found for integrity check: {BackupFile}", backupFilePath);
                return false;
            }

            var fileInfo = new FileInfo(backupFilePath);
            
            // Basic file validation
            if (fileInfo.Length == 0)
            {
                _logger.LogWarning("Backup file is empty: {BackupFile}", backupFilePath);
                return false;
            }

            // Check if file is readable
            using var stream = File.OpenRead(backupFilePath);
            var buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            // For PostgreSQL backups, check for SQL dump header
            var header = System.Text.Encoding.UTF8.GetString(buffer);
            var isValidSqlDump = header.Contains("PostgreSQL database dump") || 
                               header.Contains("--") ||
                               header.Contains("SET");

            _logger.LogInformation("Backup integrity check completed for {BackupFile}. Valid: {IsValid}", 
                backupFilePath, isValidSqlDump);

            return isValidSqlDump;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup integrity for {BackupFile}", backupFilePath);
            return false;
        }
    }

    private async Task<DateTime> GetLastBackupTimeAsync()
    {
        try
        {
            var events = await LoadBackupEventsAsync();
            var lastBackup = events
                .Where(e => e.EventType == BackupEventType.BackupCompleted && e.Status == BackupEventStatus.Success)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();

            return lastBackup?.Timestamp ?? DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private async Task<StorageInfo> CheckBackupDirectorySpaceAsync()
    {
        try
        {
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), _backupSettings.BackupDirectory);
            var driveInfo = new DriveInfo(Path.GetPathRoot(backupDir) ?? "C:");
            
            var totalSizeGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            var availableSizeGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var usedSizeGB = totalSizeGB - availableSizeGB;
            var usagePercentage = (usedSizeGB / totalSizeGB) * 100;

            return await Task.FromResult(new StorageInfo
            {
                TotalGB = totalSizeGB,
                AvailableGB = availableSizeGB,
                UsedGB = usedSizeGB,
                UsagePercentage = usagePercentage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking backup directory space");
            return new StorageInfo();
        }
    }

    private async Task<IEnumerable<BackupEvent>> GetRecentBackupFailuresAsync()
    {
        try
        {
            var events = await LoadBackupEventsAsync();
            var recentFailures = events
                .Where(e => e.Status == BackupEventStatus.Failed && 
                           e.Timestamp >= DateTime.UtcNow.AddHours(-24))
                .ToList();

            return recentFailures;
        }
        catch
        {
            return Enumerable.Empty<BackupEvent>();
        }
    }

    private async Task<BackupMetrics> GetRecentMetricsAsync()
    {
        try
        {
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var metrics = await GetBackupMetricsAsync(yesterday, DateTime.UtcNow);
            return metrics.LastOrDefault() ?? new BackupMetrics { Date = DateTime.UtcNow.Date };
        }
        catch
        {
            return new BackupMetrics { Date = DateTime.UtcNow.Date };
        }
    }

    private async Task<List<BackupEvent>> LoadBackupEventsAsync()
    {
        try
        {
            if (!File.Exists(_eventsFilePath))
                return new List<BackupEvent>();

            var json = await File.ReadAllTextAsync(_eventsFilePath);
            return JsonSerializer.Deserialize<List<BackupEvent>>(json) ?? new List<BackupEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading backup events");
            return new List<BackupEvent>();
        }
    }

    private async Task SaveBackupEventsAsync(List<BackupEvent> events)
    {
        try
        {
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_eventsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving backup events");
        }
    }
}

// Supporting classes
public class StorageInfo
{
    public double TotalGB { get; set; }
    public double AvailableGB { get; set; }
    public double UsedGB { get; set; }
    public double UsagePercentage { get; set; }
}

public class BackupHealthStatus
{
    public BackupHealth OverallHealth { get; set; }
    public DateTime StatusDate { get; set; }
    public DateTime LastBackupTime { get; set; }
    public TimeSpan TimeSinceLastBackup { get; set; }
    public double AvailableStorageGB { get; set; }
    public double UsedStorageGB { get; set; }
    public int RecentFailures { get; set; }
    public List<string> HealthIssues { get; set; } = new();
    public BackupMetrics BackupMetrics { get; set; } = new();
}

public class BackupEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public BackupEventType EventType { get; set; }
    public BackupEventStatus Status { get; set; }
    public string? BackupFileName { get; set; }
    public long? BackupSizeBytes { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class BackupMetrics
{
    public DateTime Date { get; set; }
    public int TotalBackups { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public double AverageBackupDuration { get; set; } // in minutes
    public double TotalBackupSizeGB { get; set; }
    public double SuccessRate => TotalBackups > 0 ? (double)SuccessfulBackups / TotalBackups * 100 : 0;
}

public class BackupAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public BackupAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public BackupEvent? BackupEvent { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum BackupHealth
{
    Healthy,
    Warning,
    Critical
}

public enum BackupEventType
{
    BackupStarted,
    BackupCompleted,
    BackupScheduled,
    BackupCancelled,
    RestoreStarted,
    RestoreCompleted,
    CleanupCompleted
}

public enum BackupEventStatus
{
    Success,
    Failed,
    InProgress,
    Cancelled
}

public enum BackupAlertType
{
    BackupFailure,
    StorageWarning,
    ScheduleFailure,
    IntegrityFailure,
    PerformanceWarning
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}
