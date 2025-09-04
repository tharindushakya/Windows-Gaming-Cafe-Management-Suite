namespace GamingCafe.Core.DTOs;

public class BackupInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public BackupStatus Status { get; set; }
}

public class CreateBackupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BackupType Type { get; set; } = BackupType.Full;
    public bool IncludeData { get; set; } = true;
    public bool IncludeSchema { get; set; } = true;
}

public class RestoreBackupRequest
{
    public string BackupName { get; set; } = string.Empty;
    public bool ConfirmRestore { get; set; }
    public bool DropExistingDatabase { get; set; }
}

public class BackupScheduleRequest
{
    public TimeSpan Interval { get; set; }
    public bool Enabled { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public BackupType Type { get; set; } = BackupType.Full;
    public int RetentionDays { get; set; } = 30;
}

public class BackupOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? BackupPath { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public long? FileSizeBytes { get; set; }
}

public enum BackupType
{
    Full,
    SchemaOnly,
    DataOnly
}

public enum BackupStatus
{
    Created,
    InProgress,
    Completed,
    Failed,
    Corrupted
}
