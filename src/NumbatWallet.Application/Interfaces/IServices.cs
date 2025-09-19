using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.Interfaces;

public interface IPersonService
{
    Task<IEnumerable<PersonDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PersonDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<object> VerifyIdentityAsync(Guid id, object request, CancellationToken cancellationToken);
}

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IWalletService
{
    Task<WalletDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WalletDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken);
    Task<IEnumerable<WalletDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<bool> UserHasAccessAsync(Guid walletId, string userId, CancellationToken cancellationToken);
    Task LockWalletAsync(Guid walletId, CancellationToken cancellationToken);
    Task<bool> UnlockWalletAsync(Guid walletId, string pin, CancellationToken cancellationToken);
    Task<object> GetWalletStatisticsAsync(Guid walletId, CancellationToken cancellationToken);
}

public interface ICredentialService
{
    Task<IEnumerable<CredentialDto>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken);
    Task<CredentialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<CredentialDto>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken);
    Task<IEnumerable<CredentialDto>> GetExpiredCredentialsAsync(Guid walletId, CancellationToken cancellationToken);
    Task<bool> UserHasAccessAsync(Guid credentialId, string userId, CancellationToken cancellationToken);
    Task<object> VerifyCredentialAsync(Guid credentialId, CancellationToken cancellationToken);
    Task<IEnumerable<object>> GetAvailableCredentialTypesAsync(CancellationToken cancellationToken);
    Task<object> ValidateJwtVcAsync(string token, CancellationToken cancellationToken);
}

public interface IStatisticsService
{
    Task<object> GetDashboardStatisticsAsync(CancellationToken cancellationToken);
    Task<IEnumerable<object>> GetIssuanceStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
}

public interface IHealthCheckService
{
    Task<object> GetHealthStatusAsync(CancellationToken cancellationToken);
}