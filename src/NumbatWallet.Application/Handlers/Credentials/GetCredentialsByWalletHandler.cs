using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.Application.Specifications;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Handlers.Credentials;

public class GetCredentialsByWalletHandler : IQueryHandler<GetCredentialsByWalletQuery, IEnumerable<CredentialDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<GetCredentialsByWalletHandler> _logger;

    public GetCredentialsByWalletHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        ILogger<GetCredentialsByWalletHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CredentialDto>> HandleAsync(
        GetCredentialsByWalletQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving credentials for wallet {WalletId}, tenant {TenantId}",
            query.WalletId, query.TenantId);

        // Verify wallet belongs to tenant
        var wallet = await _walletRepository.GetByIdAsync(query.WalletId, cancellationToken);
        if (wallet == null || wallet.TenantId != query.TenantId.ToString())
        {
            _logger.LogWarning("Wallet {WalletId} not found or belongs to different tenant", query.WalletId);
            return Enumerable.Empty<CredentialDto>();
        }

        // Build specification
        var spec = query.IncludeRevoked
            ? (ISpecification<Domain.Aggregates.Credential>)new CredentialByWalletSpecification(query.WalletId, query.TenantId)
            : new ActiveCredentialByWalletSpecification(query.WalletId, query.TenantId);

        var credentials = await _credentialRepository.FindAsync(spec, cancellationToken);

        return credentials.Select(c => new CredentialDto
        {
            Id = c.Id.ToString(),
            HolderId = c.WalletId.ToString(),
            IssuerId = c.IssuerId.ToString(),
            Type = c.CredentialType,
            CredentialSubject = new Dictionary<string, object>(c.Claims),
            IssuanceDate = c.IssuedAt.DateTime,
            ExpirationDate = c.ExpiresAt?.DateTime,
            Status = c.Status.ToString(),
            Proof = null,
            Metadata = null,
            IsRevoked = c.Status == CredentialStatus.Revoked,
            RevocationDate = c.RevokedAt?.DateTime,
            RevocationReason = c.RevocationReason
        }).ToList();
    }
}