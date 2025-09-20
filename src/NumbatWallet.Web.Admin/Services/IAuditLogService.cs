namespace NumbatWallet.Web.Admin.Services;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogEntry>> GetAuditLogsAsync(
        AuditLogFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AuditLogEntry?> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<byte[]> ExportLogsAsync(
        AuditLogFilter filter,
        string format = "csv",
        CancellationToken cancellationToken = default);

    Task<AuditLogStatistics> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}

public class AuditLogFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Severity { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? UserId { get; set; }
    public string? SearchTerm { get; set; }
}

public class AuditLogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = "Info";
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TenantId { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public Dictionary<string, object>? ChangedFields { get; set; }
}

public class AuditLogStatistics
{
    public int TotalLogs { get; set; }
    public Dictionary<string, int> LogsBySeverity { get; set; } = new();
    public Dictionary<string, int> LogsByAction { get; set; } = new();
    public Dictionary<string, int> LogsByEntityType { get; set; } = new();
    public Dictionary<string, int> LogsByUser { get; set; } = new();
    public List<TimeSeriesDataPoint> LogsOverTime { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}