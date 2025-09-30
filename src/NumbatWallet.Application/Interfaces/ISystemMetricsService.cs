using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface ISystemMetricsService
{
    Task<DetailedSystemMetricsDto> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
    Task<DetailedSystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
    Task<List<ApiEndpointMetricsDto>> GetApiMetricsAsync(TimeSpan period, CancellationToken cancellationToken = default);
    Task<ResourceUsageDto> GetResourceUsageAsync(CancellationToken cancellationToken = default);
    Task<DatabaseMetricsDto> GetDatabaseMetricsAsync(CancellationToken cancellationToken = default);
    Task<CacheMetricsDto> GetCacheMetricsAsync(CancellationToken cancellationToken = default);
    Task<SecurityMetricsDto> GetSecurityMetricsAsync(CancellationToken cancellationToken = default);
    Task<TenantMetricsDto> GetTenantMetricsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<List<MetricTimeSeriesDto>> GetTimeSeriesMetricsAsync(string metricName, DateTime from, DateTime toDate, CancellationToken cancellationToken = default);
}