namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for backup and restore operations
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Create a full system backup
    /// </summary>
    Task<BackupResult> CreateBackupAsync(BackupOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore from a backup
    /// </summary>
    Task<RestoreResult> RestoreFromBackupAsync(string backupId, RestoreOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// List available backups
    /// </summary>
    Task<BackupInfo[]> ListBackupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a backup
    /// </summary>
    Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a backup
    /// </summary>
    Task<BackupValidationResult> ValidateBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule automated backup
    /// </summary>
    Task<string> ScheduleBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export backup to external storage
    /// </summary>
    Task<Stream> ExportBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backup history
    /// </summary>
    Task<BackupHistory> GetBackupHistoryAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backup status
    /// </summary>
    Task<BackupStatus> GetBackupStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a backup operation (for GraphQL compatibility)
    /// </summary>
    Task<BackupJob> StartBackupAsync(BackupOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a restore operation (for GraphQL compatibility)
    /// </summary>
    Task<RestoreJob> StartRestoreAsync(string backupId, RestoreOptions? options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backup history as DTOs
    /// </summary>
    Task<List<BackupDto>> GetBackupHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get backup status by ID
    /// </summary>
    Task<BackupStatusDto?> GetBackupStatusAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Backup operation result
/// </summary>
public class BackupResult
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeInBytes { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public BackupStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Restore operation result
/// </summary>
public class RestoreResult
{
    public bool Success { get; set; }
    public DateTime RestoredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public RestoreStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Backup information
/// </summary>
public class BackupInfo
{
    public string BackupId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeInBytes { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Backup validation result
/// </summary>
public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Backup options
/// </summary>
public class BackupOptions
{
    public BackupType Type { get; set; } = BackupType.Full;
    public bool IncludeData { get; set; } = true;
    public bool IncludeConfiguration { get; set; } = true;
    public bool IncludeSecrets { get; set; } = false;
    public bool IncludeMedia { get; set; } = false;
    public bool CompressBackup { get; set; } = true;
    public bool Compress => CompressBackup; // Alias for GraphQL
    public bool EncryptBackup { get; set; } = true;
    public string? Description { get; set; }
    public int RetentionDays { get; set; } = 30;
}

/// <summary>
/// Restore options
/// </summary>
public class RestoreOptions
{
    public bool RestoreData { get; set; } = true;
    public bool RestoreConfiguration { get; set; } = true;
    public bool RestoreSecrets { get; set; } = false;
    public bool ValidateBeforeRestore { get; set; } = true;
    public bool ValidateIntegrity => ValidateBeforeRestore; // Alias for GraphQL
    public bool CreateBackupBeforeRestore { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
    public List<string>? IncludeTables { get; set; }
    public List<string>? ExcludeTables { get; set; }
}

/// <summary>
/// Backup schedule
/// </summary>
public class BackupSchedule
{
    public string ScheduleName { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public BackupOptions Options { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Backup statistics
/// </summary>
public class BackupStatistics
{
    public int TablesBackedUp { get; set; }
    public long RecordsBackedUp { get; set; }
    public int FilesBackedUp { get; set; }
    public long BytesProcessed { get; set; }
}

/// <summary>
/// Restore statistics
/// </summary>
public class RestoreStatistics
{
    public int TablesRestored { get; set; }
    public long RecordsRestored { get; set; }
    public int FilesRestored { get; set; }
    public long BytesProcessed { get; set; }
}

/// <summary>
/// Backup history
/// </summary>
public class BackupHistory
{
    public BackupInfo[] Backups { get; set; } = Array.Empty<BackupInfo>();
    public int TotalCount { get; set; }
    public DateTime LastBackupAt { get; set; }
    public DateTime? NextScheduledBackupAt { get; set; }
}

/// <summary>
/// Backup status
/// </summary>
public class BackupStatus
{
    public bool IsBackupInProgress { get; set; }
    public string? CurrentOperation { get; set; }
    public double? ProgressPercent { get; set; }
    public DateTime? LastSuccessfulBackup { get; set; }
    public DateTime? LastFailedBackup { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Backup job (for GraphQL operations)
/// </summary>
public class BackupJob
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public BackupType Type { get; set; }
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Restore job (for GraphQL operations)
/// </summary>
public class RestoreJob
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Backup type enum
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential
}

/// <summary>
/// Backup DTO for GraphQL
/// </summary>
public class BackupDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Backup status DTO for GraphQL
/// </summary>
public class BackupStatusDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PercentComplete { get; set; }
    public string? CurrentOperation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
}
