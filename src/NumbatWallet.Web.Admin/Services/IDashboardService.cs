namespace NumbatWallet.Web.Admin.Services;

public interface IDashboardService
{
    Task<DashboardMetrics> GetMetricsAsync(string timeRange = "24h", CancellationToken cancellationToken = default);
    Task<List<TimeSeriesDataPoint>> GetCredentialTrendAsync(string timeRange = "7d", CancellationToken cancellationToken = default);
    Task<List<ChartDataPoint>> GetCredentialTypeDistributionAsync(CancellationToken cancellationToken = default);
    Task<List<ActivityLogEntry>> GetRecentActivityAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<SystemHealthStatus> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<List<TimeSeriesDataPoint>> GetPerformanceMetricsAsync(string timeRange = "1h", CancellationToken cancellationToken = default);
}

public class DashboardMetrics
{
    public int TotalWallets { get; set; }
    public int ActiveWallets { get; set; }
    public int TotalCredentials { get; set; }
    public int ActiveCredentials { get; set; }
    public int ExpiredCredentials { get; set; }
    public int RevokedCredentials { get; set; }
    public int TotalPersons { get; set; }
    public int VerifiedPersons { get; set; }
    public int VerificationsToday { get; set; }
    public int VerificationsThisWeek { get; set; }
    public int VerificationsThisMonth { get; set; }
    public double SystemHealthScore { get; set; }
    public string WalletGrowth { get; set; } = "+0%";
    public string CredentialGrowth { get; set; } = "+0%";
    public string VerificationChange { get; set; } = "+0%";
    public DateTime LastUpdated { get; set; }
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string? Label { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Color { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ActivityLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? EntityId { get; set; }
    public string Severity { get; set; } = "Info";
    public Dictionary<string, object>? Details { get; set; }
}

public class SystemHealthStatus
{
    public bool IsHealthy { get; set; }
    public double HealthScore { get; set; }
    public DateTime LastChecked { get; set; }
    public List<HealthCheckResult> Checks { get; set; } = new();
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
}

public class HealthCheckResult
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public string? Description { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}