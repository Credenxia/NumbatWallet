namespace NumbatWallet.Application.DTOs;

/// <summary>
/// DTOs for bulk operation status and results
/// POA: Issue #187 - Bulk operations support
/// </summary>
public enum BulkOperationType
{
    Issue,
    Revoke,
    Refresh,
    Export,
    Delete
}

public class BulkOperationRequestDto
{
    public BulkOperationType Operation { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public List<Guid> EntityIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTimeOffset? ScheduledAt { get; set; }
    public string? NotificationEmail { get; set; }
}

public class PagedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

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