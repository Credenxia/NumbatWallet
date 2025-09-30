namespace NumbatWallet.Application.DTOs;

public record DetailedSystemMetricsDto(
    int ActiveWallets,
    int TotalCredentials,
    int CredentialsIssuedToday,
    int ActiveTenants,
    int TotalTenants,
    long ApiRequestsPerMinute,
    DateTime CollectedAt
);

public record DetailedSystemHealthDto(
    string ApiHealth,
    string DatabaseHealth,
    string CacheHealth,
    string StorageHealth,
    double ApiHealthScore,
    double DatabaseHealthScore,
    double CacheHealthScore,
    double StorageHealthScore,
    DateTime LastHealthCheck
);

public record PerformanceMetricsDto(
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    double P99ResponseTimeMs,
    double CpuUsagePercent,
    long MemoryUsageBytes,
    long DiskUsageBytes,
    int ActiveConnections,
    int ThreadCount,
    DateTime CollectedAt
);

public record ApiEndpointMetricsDto(
    string Endpoint,
    string Method,
    long RequestCount,
    double AverageResponseTimeMs,
    double MaxResponseTimeMs,
    int ErrorCount,
    double ErrorRate,
    DateTime PeriodStart,
    DateTime PeriodEnd
);

public record ResourceUsageDto(
    double CpuUsagePercent,
    long MemoryUsedBytes,
    long MemoryAvailableBytes,
    long DiskUsedBytes,
    long DiskAvailableBytes,
    double NetworkInMbps,
    double NetworkOutMbps,
    int ProcessCount,
    DateTime CollectedAt
);

public record DatabaseMetricsDto(
    int ActiveConnections,
    int IdleConnections,
    int MaxConnections,
    long QueryCount,
    double AverageQueryTimeMs,
    long SlowQueryCount,
    long DatabaseSizeBytes,
    double CacheHitRatio,
    DateTime CollectedAt
);

public record CacheMetricsDto(
    long TotalKeys,
    long MemoryUsedBytes,
    long HitCount,
    long MissCount,
    double HitRatio,
    long EvictedKeys,
    double AverageGetTimeMs,
    double AverageSetTimeMs,
    DateTime CollectedAt
);

public record SecurityMetricsDto(
    long TotalLoginAttempts,
    long FailedLoginAttempts,
    long BlockedRequests,
    long SuspiciousActivities,
    int ActiveSessions,
    int LockedAccounts,
    DateTime LastSecurityIncident,
    string ThreatLevel,
    DateTime CollectedAt
);

public record TenantMetricsDto(
    Guid TenantId,
    string TenantName,
    int WalletCount,
    int CredentialCount,
    int UserCount,
    long StorageUsedBytes,
    long ApiCallsToday,
    double ApiQuotaUsedPercent,
    bool IsActive,
    DateTime CreatedAt,
    DateTime LastActivityAt
);

public record MetricTimeSeriesDto(
    string MetricName,
    DateTime Timestamp,
    double Value,
    string Unit,
    Dictionary<string, string>? Tags
);

public record KeyMetricsDto(
    int ActiveKeys,
    int ExpiringKeys,
    int ExpiredKeys,
    int DaysUntilNextRotation,
    DateTime LastRotationDate,
    string PrimaryKeyAlgorithm,
    bool AutoRotationEnabled,
    int RotationFrequencyDays
);

public record ComplianceMetricsDto(
    bool TdifCompliant,
    bool Iso27001Compliant,
    bool FipsCompliant,
    bool GdprCompliant,
    DateTime LastAuditDate,
    DateTime NextAuditDate,
    int OpenIssues,
    int ComplianceScore
);

public record BackupMetricsDto(
    DateTime LastBackupDate,
    DateTime NextScheduledBackup,
    long LastBackupSizeBytes,
    int BackupRetentionDays,
    bool AutoBackupEnabled,
    string BackupStorageProvider,
    int SuccessfulBackups,
    int FailedBackups
);