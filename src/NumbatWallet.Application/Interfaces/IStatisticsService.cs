using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface IStatisticsService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<IssuanceStatisticsDto>> GetIssuanceStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<TenantStatisticsDto> GetTenantStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<MetricsSnapshotDto> GetMetricsSnapshotAsync(DateTime from, DateTime until, CancellationToken cancellationToken = default);
    Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default);
}
