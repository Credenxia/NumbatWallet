namespace NumbatWallet.SharedKernel.Models;

/// <summary>
/// Result of bulk operations
/// </summary>
public class BulkOperationResult<T>
{
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<T> SuccessfulItems { get; set; } = new();
    public List<BulkOperationError> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }

    public bool IsSuccess => FailureCount == 0;
    public bool IsPartialSuccess => SuccessCount > 0 && FailureCount > 0;
}

public class BulkOperationError
{
    public string? ItemId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
}