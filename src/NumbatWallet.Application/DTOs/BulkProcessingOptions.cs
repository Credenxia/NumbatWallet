namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Options for bulk processing operations
/// </summary>
public class BulkProcessingOptions
{
    /// <summary>
    /// Number of items to process in parallel
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Whether to continue processing on individual item failure
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts for failed items
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Timeout for individual operations in seconds
    /// </summary>
    public int OperationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to validate all items before processing
    /// </summary>
    public bool PreValidate { get; set; } = true;

    /// <summary>
    /// Whether to generate detailed audit logs
    /// </summary>
    public bool EnableDetailedAudit { get; set; } = true;

    /// <summary>
    /// User-defined metadata for the bulk operation
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Request for bulk credential operations
/// </summary>
public class BulkCredentialRequest
{
    public string CredentialId { get; set; } = string.Empty;
    public string WalletId { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public Dictionary<string, object> Claims { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
