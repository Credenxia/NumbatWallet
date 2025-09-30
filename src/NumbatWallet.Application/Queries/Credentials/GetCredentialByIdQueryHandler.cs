using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Credentials;

/// <summary>
/// Handler for retrieving a credential by ID
/// POA: Real implementation with full credential details
/// </summary>
public sealed class GetCredentialByIdQueryHandler : IQueryHandler<GetCredentialByIdQuery, CredentialDto?>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<GetCredentialByIdQueryHandler> _logger;

    public GetCredentialByIdQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<GetCredentialByIdQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<CredentialDto?> HandleAsync(
        GetCredentialByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving credential {CredentialId}", query.CredentialId);

        var credential = await _credentialRepository.GetByIdAsync(query.CredentialId, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Credential {CredentialId} not found", query.CredentialId);
            return null; // Return null instead of throwing, let controller handle 404
        }

        // Get wallet details
        var wallet = await _walletRepository.GetByIdAsync(credential.WalletId, cancellationToken);

        // Get issuer details
        var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);

        var credentialDto = new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = credential.WalletId.ToString(),
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            Status = credential.Status.ToString(),
            IsRevoked = credential.Status == SharedKernel.Enums.CredentialStatus.Revoked,
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason,
            CredentialSubject = credential.Claims.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value),
            Proof = null, // Would need to implement proof generation
            Metadata = new Dictionary<string, string>
            {
                ["WalletName"] = wallet?.Name ?? "Unknown",
                ["TenantId"] = credential.TenantId,
                ["SchemaId"] = credential.SchemaId,
                ["IssuerName"] = issuer?.Name ?? "Unknown Issuer"
            }
        };

        _logger.LogInformation("Retrieved credential {CredentialId} of type {Type} with status {Status}",
            query.CredentialId, credentialDto.Type, credentialDto.Status);

        return credentialDto;
    }
}
