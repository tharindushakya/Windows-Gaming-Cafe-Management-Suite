using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Text;
using Hangfire;

namespace GamingCafe.API.Services;

public class BackupService : IBackupService
{
    private readonly GamingCafeContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;
    private readonly string _backupDirectory;
    private readonly string _connectionString;

    public BackupService(
        GamingCafeContext context, 
        IConfiguration configuration, 
        ILogger<BackupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                           ?? throw new InvalidOperationException("DefaultConnection not found");
        
        _backupDirectory = Path.Combine(
            Directory.GetCurrentDirectory(), 
            "Backups");
        
        // Ensure backup directory exists
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<bool> CreateBackupAsync(string backupName)
    {
        try
        {
            _logger.LogInformation("Starting backup creation: {BackupName}", backupName);
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{backupName}_{timestamp}.sql";
            var backupPath = Path.Combine(_backupDirectory, fileName);
            
            var connectionBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
            var databaseName = connectionBuilder.Database;
            var host = connectionBuilder.Host;
            var port = connectionBuilder.Port;
            var username = connectionBuilder.Username;
            var password = connectionBuilder.Password;

            // Use pg_dump to create backup
            var pgDumpPath = GetPgDumpPath();
            var arguments = $"-h {host} -p {port} -U {username} -d {databaseName} -f \"{backupPath}\" --verbose --clean --create";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set password environment variable
            processInfo.EnvironmentVariables["PGPASSWORD"] = password;

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start pg_dump process");
                return false;
            }

            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0 && File.Exists(backupPath))
            {
                var fileInfo = new FileInfo(backupPath);
                _logger.LogInformation(
                    "Backup created successfully: {BackupPath}, Size: {Size:N0} bytes", 
                    backupPath, 
                    fileInfo.Length);
                return true;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Backup failed. Exit code: {ExitCode}, Error: {Error}", 
                    process.ExitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup: {BackupName}", backupName);
            return false;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupName)
    {
        try
        {
            _logger.LogInformation("Starting backup restoration: {BackupName}", backupName);
            
            // Find the backup file
            var backupFiles = Directory.GetFiles(_backupDirectory, $"{backupName}_*.sql");
            if (!backupFiles.Any())
            {
                _logger.LogError("Backup file not found: {BackupName}", backupName);
                return false;
            }

            // Get the most recent backup if multiple exist
            var backupPath = backupFiles
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .First();

            var connectionBuilder = new NpgsqlConnectionStringBuilder(_connectionString);
            var databaseName = connectionBuilder.Database;
            var host = connectionBuilder.Host;
            var port = connectionBuilder.Port;
            var username = connectionBuilder.Username;
            var password = connectionBuilder.Password;

            // Use psql to restore backup
            var psqlPath = GetPsqlPath();
            var arguments = $"-h {host} -p {port} -U {username} -d {databaseName} -f \"{backupPath}\" --verbose";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = psqlPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set password environment variable
            processInfo.EnvironmentVariables["PGPASSWORD"] = password;

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start psql process");
                return false;
            }

            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Backup restored successfully: {BackupPath}", backupPath);
                return true;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Restore failed. Exit code: {ExitCode}, Error: {Error}", 
                    process.ExitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup: {BackupName}", backupName);
            return false;
        }
    }

    public async Task<IEnumerable<object>> GetAvailableBackupsAsync()
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "*.sql");
            var backups = new List<BackupInfoDto>();

            foreach (var filePath in backupFiles)
            {
                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                
                // Parse backup name from filename (remove timestamp)
                var parts = fileName.Split('_');
                var backupName = parts.Length > 2 
                    ? string.Join("_", parts.Take(parts.Length - 2))
                    : fileName;

                backups.Add(new BackupInfoDto
                {
                    Name = backupName,
                    FileName = fileInfo.Name,
                    CreatedDate = fileInfo.CreationTime,
                    FileSizeBytes = fileInfo.Length,
                    FileSizeFormatted = FormatFileSize(fileInfo.Length),
                    Type = BackupType.Full,
                    Status = BackupStatus.Completed,
                    Description = $"Database backup created on {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}"
                });
            }

            return await Task.FromResult(backups.OrderByDescending(b => b.CreatedDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available backups");
            return new List<BackupInfoDto>();
        }
    }

    public async Task<bool> DeleteBackupAsync(string backupName)
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, $"{backupName}_*.sql");
            
            if (!backupFiles.Any())
            {
                _logger.LogWarning("No backup files found for: {BackupName}", backupName);
                return false;
            }

            foreach (var filePath in backupFiles)
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted backup file: {FilePath}", filePath);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup: {BackupName}", backupName);
            return false;
        }
    }

    public async Task<bool> ScheduleBackupAsync(TimeSpan interval)
    {
        try
        {
            var jobId = "scheduled-backup";
            
            // Remove existing scheduled job
            RecurringJob.RemoveIfExists(jobId);
            
            // Schedule new backup job
            RecurringJob.AddOrUpdate(
                jobId,
                () => CreateScheduledBackupAsync(),
                Cron.Daily(2)); // Run daily at 2 AM

            _logger.LogInformation("Scheduled backup job created with interval: {Interval}", interval);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling backup");
            return false;
        }
    }

    public async Task CreateScheduledBackupAsync()
    {
        var backupName = $"scheduled_backup_{DateTime.UtcNow:yyyyMMdd}";
        var success = await CreateBackupAsync(backupName);
        
        if (success)
        {
            _logger.LogInformation("Scheduled backup completed successfully: {BackupName}", backupName);
            
            // Clean up old backups (keep last 30 days)
            await CleanupOldBackupsAsync(30);
        }
        else
        {
            _logger.LogError("Scheduled backup failed: {BackupName}", backupName);
        }
    }

    private async Task CleanupOldBackupsAsync(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var backupFiles = Directory.GetFiles(_backupDirectory, "*.sql");
            
            foreach (var filePath in backupFiles)
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted old backup: {FilePath}", filePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old backups");
        }
        
        await Task.CompletedTask;
    }

    private string GetPgDumpPath()
    {
        // Try to find pg_dump in common locations
        var possiblePaths = new[]
        {
            "pg_dump", // If in PATH
            @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            @"C:\PostgreSQL\bin\pg_dump.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path) || path == "pg_dump")
            {
                return path;
            }
        }

        throw new FileNotFoundException("pg_dump executable not found. Please ensure PostgreSQL client tools are installed.");
    }

    private string GetPsqlPath()
    {
        // Try to find psql in common locations
        var possiblePaths = new[]
        {
            "psql", // If in PATH
            @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\16\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\15\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\14\bin\psql.exe",
            @"C:\PostgreSQL\bin\psql.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path) || path == "psql")
            {
                return path;
            }
        }

        throw new FileNotFoundException("psql executable not found. Please ensure PostgreSQL client tools are installed.");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
