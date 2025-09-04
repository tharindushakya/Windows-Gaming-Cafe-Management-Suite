namespace GamingCafe.Core.Configuration;

public class BackupSettings
{
    public string BackupDirectory { get; set; } = "Backups";
    public int RetentionDays { get; set; } = 30;
    public bool EnableScheduledBackups { get; set; } = true;
    public string ScheduleCron { get; set; } = "0 2 * * *"; // Daily at 2 AM
    public bool EnableCompressionGzip { get; set; } = true;
    public BackupStorageSettings Storage { get; set; } = new();
    public BackupMonitoringSettings Monitoring { get; set; } = new();
    public BackupSecuritySettings Security { get; set; } = new();
}

public class BackupStorageSettings
{
    public string? RemoteStoragePath { get; set; }
    public BackupStorageType StorageType { get; set; } = BackupStorageType.Local;
    public long MaxBackupSizeBytes { get; set; } = 10_737_418_240; // 10 GB
    public bool EnableEncryption { get; set; } = false;
    public string? EncryptionKey { get; set; }
}

public class BackupMonitoringSettings
{
    public bool EnableHealthChecks { get; set; } = true;
    public bool EnableAlerts { get; set; } = true;
    public string[] AlertEmails { get; set; } = Array.Empty<string>();
    public int HealthCheckIntervalMinutes { get; set; } = 60;
    public int AlertThresholdHours { get; set; } = 25; // Alert if no backup in 25 hours
}

public class BackupSecuritySettings
{
    public bool RequireDirectoryPermissions { get; set; } = true;
    public bool ValidateBackupIntegrity { get; set; } = true;
    public bool LogBackupOperations { get; set; } = true;
    public bool RequireConfirmationForRestore { get; set; } = true;
}

public enum BackupStorageType
{
    Local,
    NetworkShare,
    CloudStorage
}
