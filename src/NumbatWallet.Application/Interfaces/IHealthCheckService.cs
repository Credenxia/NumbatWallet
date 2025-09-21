using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface IHealthCheckService
{
    Task<HealthStatusDto> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckDatabaseAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckCacheAsync(CancellationToken cancellationToken = default);
    Task<bool> CheckStorageAsync(CancellationToken cancellationToken = default);
}