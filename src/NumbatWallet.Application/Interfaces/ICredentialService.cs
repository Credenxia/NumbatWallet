using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface ICredentialService
{
    Task<CredentialDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialDto>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialDto>> GetActiveCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialDto>> GetExpiredCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialDto>> GetRevokedCredentialsAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<CredentialDto> IssueAsync(IssueCredentialDto dto, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<VerificationResultDto> VerifyAsync(Guid id, VerificationOptionsDto options, CancellationToken cancellationToken = default);
    Task<PresentationDto> CreatePresentationAsync(CreatePresentationDto dto, CancellationToken cancellationToken = default);
}