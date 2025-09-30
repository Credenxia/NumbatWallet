namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for tracking and notifying progress of long-running operations
/// </summary>
public interface IProgressNotificationService
{
    /// <summary>
    /// Start tracking a new operation
    /// </summary>
    Task<string> StartOperationAsync(string operationName, int totalItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update operation progress
    /// </summary>
    Task UpdateProgressAsync(string operationId, int processedItems, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark operation as completed
    /// </summary>
    Task CompleteOperationAsync(string operationId, bool success, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current progress for an operation
    /// </summary>
    Task<ProgressUpdate?> GetProgressAsync(string operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to progress updates for an operation
    /// </summary>
    IAsyncEnumerable<ProgressUpdate> SubscribeToProgressAsync(string operationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress update information
/// </summary>
public class ProgressUpdate
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public double PercentComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
    public string? CurrentMessage { get; set; }
    public ProgressStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Status of a progress operation
/// </summary>
public enum ProgressStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
