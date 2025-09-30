using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Credentials;
using NumbatWallet.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Handlers.Credentials;

public class GetCredentialByIdHandler : IQueryHandler<GetCredentialByIdQuery, CredentialDto?>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ILogger<GetCredentialByIdHandler> _logger;

    public GetCredentialByIdHandler(
        ICredentialRepository credentialRepository,
        ILogger<GetCredentialByIdHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _logger = logger;
    }

    public async Task<CredentialDto?> HandleAsync(GetCredentialByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving credential {CredentialId} for tenant {TenantId}", query.CredentialId, query.TenantId);

        var credential = await _credentialRepository.GetByIdAsync(query.CredentialId, cancellationToken);

        if (credential == null)
        {
            _logger.LogWarning("Credential {CredentialId} not found", query.CredentialId);
            return null;
        }

        if (credential.TenantId != query.TenantId.ToString())
        {
            _logger.LogWarning("Credential {CredentialId} belongs to different tenant", query.CredentialId);
            return null;
        }

        return new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = credential.WalletId.ToString(),
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            CredentialSubject = new Dictionary<string, object>(credential.Claims),
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            Proof = null,
            Metadata = null,
            IsRevoked = credential.Status == CredentialStatus.Revoked,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason
        };
    }
}