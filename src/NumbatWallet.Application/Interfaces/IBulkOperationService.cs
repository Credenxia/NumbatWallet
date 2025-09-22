using NumbatWallet.Application.Commands.Credentials;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing bulk operations
/// POA: Issue #187 - Bulk operations management
/// </summary>
public interface IBulkOperationService
{
    /// <summary>
    /// Get the status of a bulk operation
    /// </summary>
    Task<BulkOperationStatusDto?> GetOperationStatusAsync(string operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running bulk operation
    /// </summary>
    Task<bool> CancelOperationAsync(string operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operation history with filtering
    /// </summary>
    Task<List<BulkOperationSummaryDto>> GetOperationHistoryAsync(
        DateTime from,
        DateTime to,
        string? operationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export operation results
    /// </summary>
    Task<byte[]?> ExportOperationResultsAsync(
        string operationId,
        string format,
        CancellationToken cancellationToken = default);
}

public class BulkOperationStatusDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BulkOperationSummaryDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}