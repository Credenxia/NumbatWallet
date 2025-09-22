namespace NumbatWallet.Application.DTOs;

/// <summary>
/// DTOs for bulk operation status and results
/// POA: Issue #187 - Bulk operations support
/// </summary>
public class OperationStatusDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OperationType { get; set; } = "BulkOperation"; // Added for UI
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double PercentComplete { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<string> Errors { get; set; } = new(); // Added for UI
}

public class OperationResultsDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<OperationResultItemDto> Items { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
}

public class OperationResultItemDto
{
    public string CredentialId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}