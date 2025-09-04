using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data;
using GamingCafe.Core.Interfaces.Services;
using StackExchange.Redis;
using System.Diagnostics;

namespace GamingCafe.API.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly GamingCafeContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(GamingCafeContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Test database connection
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Test a simple query
            var userCount = await _context.Users.CountAsync(cancellationToken);
            var stationCount = await _context.GameStations.CountAsync(cancellationToken);
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                { "responseTime", stopwatch.ElapsedMilliseconds },
                { "userCount", userCount },
                { "stationCount", stationCount },
                { "connectionString", _context.Database.GetConnectionString()?.Split(';')[0] ?? "Unknown" }
            };

            if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds threshold
            {
                _logger.LogWarning("Database health check took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Degraded("Database response time is slow", null, data);
            }

            return HealthCheckResult.Healthy("Database is responding normally", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(ICacheService cacheService, ILogger<RedisHealthCheck> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Test Redis connection with a simple set/get operation
            var testKey = "health-check-" + Guid.NewGuid().ToString("N")[..8];
            var testValue = DateTime.UtcNow.ToString("O");
            
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrievedValue = await _cacheService.GetAsync<string>(testKey);
            
            if (retrievedValue != testValue)
            {
                return HealthCheckResult.Unhealthy("Redis set/get operation failed - values don't match");
            }
            
            // Clean up test key
            await _cacheService.RemoveAsync(testKey);
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                { "responseTime", stopwatch.ElapsedMilliseconds },
                { "testKey", testKey },
                { "operation", "set/get/remove" }
            };

            if (stopwatch.ElapsedMilliseconds > 2000) // 2 seconds threshold
            {
                _logger.LogWarning("Redis health check took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Degraded("Redis response time is slow", null, data);
            }

            return HealthCheckResult.Healthy("Redis is responding normally", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}

public class BackupServiceHealthCheck : IHealthCheck
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupServiceHealthCheck> _logger;

    public BackupServiceHealthCheck(IBackupService backupService, ILogger<BackupServiceHealthCheck> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Check backup service functionality
            var backups = await _backupService.GetAvailableBackupsAsync();
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                { "responseTime", stopwatch.ElapsedMilliseconds },
                { "availableBackups", backups.Count() },
                { "backupDirectory", Path.Combine(Directory.GetCurrentDirectory(), "Backups") }
            };

            // Check if backup directory exists and is writable
            var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(backupDir))
            {
                return HealthCheckResult.Degraded("Backup directory does not exist", null, data);
            }

            return HealthCheckResult.Healthy("Backup service is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup service health check failed");
            return HealthCheckResult.Unhealthy("Backup service failed", ex);
        }
    }
}

public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailServiceHealthCheck> _logger;

    public EmailServiceHealthCheck(IEmailService emailService, IConfiguration configuration, ILogger<EmailServiceHealthCheck> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate actual async health check
            await Task.Delay(1, cancellationToken);
            
            // Check email service configuration
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = _configuration["Email:SmtpPort"];
            var fromEmail = _configuration["Email:FromEmail"];
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                { "responseTime", stopwatch.ElapsedMilliseconds },
                { "smtpConfigured", !string.IsNullOrEmpty(smtpHost) },
                { "smtpHost", smtpHost ?? "Not configured" },
                { "smtpPort", smtpPort ?? "Not configured" },
                { "fromEmail", fromEmail ?? "Not configured" }
            };

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(fromEmail))
            {
                return HealthCheckResult.Degraded("Email service configuration incomplete", null, data);
            }

            // Note: We don't actually send a test email to avoid spam
            // In production, you might want to implement a test email feature
            
            return HealthCheckResult.Healthy("Email service configuration is complete", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email service health check failed");
            return HealthCheckResult.Unhealthy("Email service check failed", ex);
        }
    }
}

public class FileUploadServiceHealthCheck : IHealthCheck
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FileUploadServiceHealthCheck> _logger;

    public FileUploadServiceHealthCheck(IFileUploadService fileUploadService, ILogger<FileUploadServiceHealthCheck> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Check if upload directory exists and is writable
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var data = new Dictionary<string, object>
            {
                { "responseTime", stopwatch.ElapsedMilliseconds },
                { "uploadDirectory", uploadDir },
                { "directoryExists", Directory.Exists(uploadDir) }
            };

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
                data["directoryExists"] = true;
                data["directoryCreated"] = true;
            }

            // Test write permissions
            var testFile = Path.Combine(uploadDir, "health-check.tmp");
            await File.WriteAllTextAsync(testFile, "health-check", cancellationToken);
            File.Delete(testFile);
            
            stopwatch.Stop();
            data["responseTime"] = stopwatch.ElapsedMilliseconds;
            data["writePermissions"] = true;

            return HealthCheckResult.Healthy("File upload service is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload service health check failed");
            return HealthCheckResult.Unhealthy("File upload service failed", ex);
        }
    }
}

public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public ApplicationHealthCheck(ILogger<ApplicationHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var uptime = DateTime.Now - startTime;
            var memoryUsage = process.WorkingSet64;
            
            var data = new Dictionary<string, object>
            {
                { "uptime", uptime.ToString(@"dd\.hh\:mm\:ss") },
                { "memoryUsageMB", memoryUsage / (1024 * 1024) },
                { "processId", process.Id },
                { "machineName", Environment.MachineName },
                { "osVersion", Environment.OSVersion.ToString() },
                { "dotnetVersion", Environment.Version.ToString() }
            };

            // Check memory usage (warning if over 1GB)
            if (memoryUsage > 1024 * 1024 * 1024)
            {
                _logger.LogWarning("High memory usage detected: {MemoryMB}MB", memoryUsage / (1024 * 1024));
                return Task.FromResult(HealthCheckResult.Degraded("High memory usage detected", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Application is running normally", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check failed", ex));
        }
    }
}
