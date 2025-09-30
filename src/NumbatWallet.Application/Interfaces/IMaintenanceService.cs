namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for system maintenance operations
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Enable maintenance mode
    /// </summary>
    Task<bool> EnableMaintenanceModeAsync(MaintenanceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable maintenance mode
    /// </summary>
    Task<bool> DisableMaintenanceModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if system is in maintenance mode
    /// </summary>
    Task<MaintenanceStatus> GetMaintenanceStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule maintenance window
    /// </summary>
    Task<string> ScheduleMaintenanceAsync(MaintenanceWindow window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel scheduled maintenance
    /// </summary>
    Task<bool> CancelScheduledMaintenanceAsync(string maintenanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run database optimization
    /// </summary>
    Task<OptimizationResult> OptimizeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old logs
    /// </summary>
    Task<CleanupResult> CleanupLogsAsync(int daysToKeep, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up temporary files
    /// </summary>
    Task<CleanupResult> CleanupTempFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run system health checks
    /// </summary>
    Task<HealthCheckResult> RunHealthChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run database maintenance tasks
    /// </summary>
    Task<MaintenanceResult> RunDatabaseMaintenanceAsync(MaintenanceOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run database maintenance with default options
    /// </summary>
    Task<OptimizationResult> RunDatabaseMaintenanceAsync(CancellationToken cancellationToken = default);
}

public class MaintenanceOptions
{
    public string Reason { get; set; } = string.Empty;
    public DateTime? EstimatedEndTime { get; set; }
    public string MaintenanceMessage { get; set; } = string.Empty;
    public bool AllowAdminAccess { get; set; } = true;
}

public class MaintenanceStatus
{
    public bool IsInMaintenance { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MaintenanceWindow
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;
    public MaintenanceType Type { get; set; }
    public bool SendNotifications { get; set; } = true;
    public int NotifyMinutesBefore { get; set; } = 30;
}

public enum MaintenanceType
{
    Scheduled,
    Emergency,
    Update,
    Backup,
    Optimization
}

public class OptimizationResult
{
    public bool Success { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public long SpaceReclaimed { get; set; }
    public int TablesOptimized { get; set; }
    public int IndexesRebuilt { get; set; }
    public string[] Messages { get; set; } = Array.Empty<string>();
}

public class CleanupResult
{
    public bool Success { get; set; }
    public int FilesDeleted { get; set; }
    public long SpaceFreed { get; set; }
    public DateTime CompletedAt { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public HealthCheckItem[] Items { get; set; } = Array.Empty<HealthCheckItem>();
}

public class HealthCheckItem
{
    public string Component { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

public class MaintenanceResult
{
    public bool Success { get; set; }
    public int TablesOptimized { get; set; }
    public int IndexesRebuilt { get; set; }
    public long SpaceReclaimed { get; set; }
    public TimeSpan Duration { get; set; }
}
