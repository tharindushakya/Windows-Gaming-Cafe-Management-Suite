using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.DTOs;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

            try
            {
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
            catch (Exception ex) when (ex is FileNotFoundException || ex is System.ComponentModel.Win32Exception)
            {
                _logger.LogWarning(ex, "pg_dump not found or cannot be started — falling back to programmatic backup.");
                return await ProgrammaticBackupAsync(backupPath);
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

            try
            {
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
            catch (FileNotFoundException fnf)
            {
                _logger.LogWarning(fnf, "psql not found — falling back to programmatic restore.");
                return await ProgrammaticRestoreAsync(backupPath);
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
        // Prefer explicit known installation paths first (Windows).
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pg_dump.exe" : "pg_dump";

        var explicitPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            @"C:\PostgreSQL\bin\pg_dump.exe"
        };

        foreach (var p in explicitPaths)
        {
            if (File.Exists(p))
                return p;
        }

        // Search PATH for the executable
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch { }
        }

        // Last resort: return the bare executable name and let Process.Start resolve it (may fail if not in PATH)
        return exeName;
    }

    private string GetPsqlPath()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "psql.exe" : "psql";

        var explicitPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\17\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\16\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\15\bin\psql.exe",
            @"C:\Program Files\PostgreSQL\14\bin\psql.exe",
            @"C:\PostgreSQL\bin\psql.exe"
        };

        foreach (var p in explicitPaths)
        {
            if (File.Exists(p))
                return p;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), exeName);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch { }
        }

        return exeName;
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

    // Fallback programmatic backup (simple INSERT-based dump). Works for development and small DBs.
    private async Task<bool> ProgrammaticBackupAsync(string backupPath)
    {
        try
        {
            _logger.LogInformation("Creating programmatic backup to {BackupPath}", backupPath);

            // We'll export schema-less data as INSERT statements. This is intentionally simple and
            // works best for small development databases. For production please use pg_dump.

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var tables = new List<string>();
            await using (var cmd = new NpgsqlCommand(@"SELECT tablename FROM pg_tables WHERE schemaname = 'public';", conn))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tbl = reader.IsDBNull(0) ? null : reader.GetString(0);
                    if (!string.IsNullOrEmpty(tbl)) tables.Add(tbl!);
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("BEGIN;");

            foreach (var table in tables)
            {
                sb.AppendLine($"-- Data for table {table}");

                // Read rows
                var selectSql = $"SELECT * FROM \"{table}\";";
                await using var selectCmd = new NpgsqlCommand(selectSql, conn);
                await using var r = await selectCmd.ExecuteReaderAsync();

                var columnNames = new List<string>();
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var name = r.GetName(i);
                    if (!string.IsNullOrEmpty(name)) columnNames.Add(name);
                }

                while (await r.ReadAsync())
                {
                    var values = new List<string>();
                    for (int i = 0; i < r.FieldCount; i++)
                    {
                        if (r.IsDBNull(i))
                        {
                            values.Add("NULL");
                            continue;
                        }

                        var val = r.GetValue(i);
                        // Basic quoting/escaping — not exhaustive but sufficient for dev data types
                        if (val is string s)
                        {
                            values.Add("'" + s.Replace("'", "''") + "'");
                        }
                        else if (val is DateTime dt)
                        {
                            values.Add("'" + dt.ToString("o") + "'");
                        }
                        else if (val is bool b)
                        {
                            values.Add(b ? "TRUE" : "FALSE");
                        }
                        else
                        {
                            values.Add(val?.ToString() ?? "NULL");
                        }
                    }

                    var cols = string.Join(", ", columnNames.Select(c => $"\"{c}\""));
                    var vals = string.Join(", ", values);
                    sb.AppendLine($"INSERT INTO \"{table}\" ({cols}) VALUES ({vals});");
                }
            }

            sb.AppendLine("COMMIT;");

            await File.WriteAllTextAsync(backupPath, sb.ToString());

            _logger.LogInformation("Programmatic backup written to {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Programmatic backup failed");
            return false;
        }
    }

    // Fallback programmatic restore — executes SQL file using Npgsql
    private async Task<bool> ProgrammaticRestoreAsync(string backupPath)
    {
        try
        {
            _logger.LogInformation("Starting programmatic restore from {BackupPath}", backupPath);

            var sql = await File.ReadAllTextAsync(backupPath);

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Execute as a single batch
            await using var cmd = new NpgsqlCommand(sql, conn) { CommandTimeout = 0 };
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Programmatic restore completed from {BackupPath}", backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Programmatic restore failed");
            return false;
        }
    }
}
