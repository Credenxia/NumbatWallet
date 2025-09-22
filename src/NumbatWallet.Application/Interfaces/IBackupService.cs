using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    public bool IncludeData { get; set; } = true;
    public bool IncludeConfiguration { get; set; } = true;
    public bool IncludeSecrets { get; set; } = false;
    public bool CompressBackup { get; set; } = true;
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
    public bool CreateBackupBeforeRestore { get; set; } = true;
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