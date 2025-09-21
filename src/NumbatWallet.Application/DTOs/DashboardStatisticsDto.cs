namespace NumbatWallet.Application.DTOs;

public class DashboardStatisticsDto
{
    public int TotalUsers { get; set; }
    public int TotalPersons { get; set; }
    public int ActiveWallets { get; set; }
    public int TotalWallets { get; set; }
    public int TotalCredentials { get; set; }
    public int ActiveCredentials { get; set; }
    public int CredentialsIssuedToday { get; set; }
    public int CredentialsExpiringThisWeek { get; set; }
    public Dictionary<string, int> CredentialsByType { get; set; } = new();
    public Dictionary<string, int> WalletsByStatus { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class IssuanceStatisticsDto
{
    public DateTime Date { get; set; }
    public int IssuedCount { get; set; }
    public int RevokedCount { get; set; }
    public int ExpiredCount { get; set; }
    public Dictionary<string, int> ByCredentialType { get; set; } = new();
}

public class TenantStatisticsDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int TotalWallets { get; set; }
    public int TotalCredentials { get; set; }
    public decimal StorageUsedGB { get; set; }
    public decimal ComputeHoursUsed { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class SystemMetricsDto
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public long DatabaseConnectionsActive { get; set; }
    public long CacheHitRatio { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public long RequestsPerSecond { get; set; }
    public Dictionary<string, long> ErrorCounts { get; set; } = new();
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}