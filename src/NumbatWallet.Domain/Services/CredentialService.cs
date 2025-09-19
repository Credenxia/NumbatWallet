using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Utilities;

namespace NumbatWallet.Domain.Services;

public interface ICredentialDomainService
{
    Task<bool> CanIssueCredentialAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default);
    Task<bool> ValidateCredentialForIssuanceAsync(Credential credential, Issuer issuer, CancellationToken cancellationToken = default);
    Task<bool> ValidateCredentialStatusTransitionAsync(Credential credential, CredentialStatus newStatus, CancellationToken cancellationToken = default);
    Task<bool> IsCredentialExpiredAsync(Credential credential, CancellationToken cancellationToken = default);
    Task<bool> CanRevokeCredentialAsync(Credential credential, Guid issuerId, CancellationToken cancellationToken = default);
    Task<string> GenerateCredentialDidAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default);
}

public class CredentialDomainService : ICredentialDomainService
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly IWalletRepository _walletRepository;

    public CredentialDomainService(
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository,
        IWalletRepository walletRepository)
    {
        _credentialRepository = credentialRepository;
        _issuerRepository = issuerRepository;
        _walletRepository = walletRepository;
    }

    public async Task<bool> CanIssueCredentialAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default)
    {
        var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
        if (issuer == null)
        {
            return false;
        }

        // Check if issuer is active and trusted
        if (issuer.Status != IssuerStatus.Active)
        {
            return false;
        }

        if (!issuer.IsTrusted)
        {
            return false;
        }

        // Check if issuer supports this credential type
        return await _issuerRepository.CanIssueCredentialTypeAsync(issuerId, credentialType, cancellationToken);
    }

    public async Task<bool> ValidateCredentialForIssuanceAsync(Credential credential, Issuer issuer, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(credential, nameof(credential));
        Guard.AgainstNull(issuer, nameof(issuer));

        // Validate issuer can issue this credential type
        var canIssue = await CanIssueCredentialAsync(issuer.Id, credential.CredentialType, cancellationToken);
        if (!canIssue)
        {
            return false;
        }

        // Validate wallet exists and is active
        var wallet = await _walletRepository.GetByIdAsync(credential.WalletId, cancellationToken);
        if (wallet == null || wallet.Status != WalletStatus.Active)
        {
            return false;
        }

        // Validate credential is not already issued to this wallet
        var existingCredentials = await _credentialRepository.GetByWalletIdAsync(credential.WalletId, cancellationToken);
        var hasDuplicate = existingCredentials.Any(c =>
            c.CredentialType == credential.CredentialType &&
            c.IssuerId == issuer.Id &&
            c.Status == CredentialStatus.Active);

        return !hasDuplicate;
    }

    public Task<bool> ValidateCredentialStatusTransitionAsync(Credential credential, CredentialStatus newStatus, CancellationToken cancellationToken = default)
    {
        var currentStatus = credential.Status;

        var isValid = (currentStatus, newStatus) switch
        {
            // Active transitions
            (CredentialStatus.Active, CredentialStatus.Suspended) => true,
            (CredentialStatus.Active, CredentialStatus.Revoked) => true,
            (CredentialStatus.Active, CredentialStatus.Expired) => true,

            // Suspended transitions
            (CredentialStatus.Suspended, CredentialStatus.Active) => true,
            (CredentialStatus.Suspended, CredentialStatus.Revoked) => true,
            (CredentialStatus.Suspended, CredentialStatus.Expired) => true,

            // Expired can be renewed (creates new credential)
            (CredentialStatus.Expired, CredentialStatus.Active) => false, // Must create new credential

            // Revoked is terminal
            (CredentialStatus.Revoked, _) => false,

            // Same status is not a transition
            _ when currentStatus == newStatus => false,

            // All other transitions are invalid
            _ => false
        };

        return Task.FromResult(isValid);
    }

    public Task<bool> IsCredentialExpiredAsync(Credential credential, CancellationToken cancellationToken = default)
    {
        if (credential.ExpiresAt == null)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(credential.ExpiresAt.Value <= DateTimeOffset.UtcNow);
    }

    public async Task<bool> CanRevokeCredentialAsync(Credential credential, Guid issuerId, CancellationToken cancellationToken = default)
    {
        // Only the issuer can revoke their own credentials
        if (credential.IssuerId != issuerId)
        {
            return false;
        }

        // Can't revoke already revoked credentials
        if (credential.Status == CredentialStatus.Revoked)
        {
            return false;
        }

        // Verify issuer still exists and is active
        var issuer = await _issuerRepository.GetByIdAsync(issuerId, cancellationToken);
        return issuer != null && issuer.Status == IssuerStatus.Active;
    }

    public Task<string> GenerateCredentialDidAsync(Guid issuerId, string credentialType, CancellationToken cancellationToken = default)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var typeSlug = credentialType.ToLowerInvariant().Replace(" ", "-");
        var did = $"did:cred:{typeSlug}:{uniqueId}";

        return Task.FromResult(did);
    }
}