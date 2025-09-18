using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Repositories;

namespace NumbatWallet.Application.Queries.Credentials;

public record GetCredentialQuery : IQuery<CredentialDto>
{
    public required Guid CredentialId { get; init; }
    public required Guid WalletId { get; init; }
}

public class GetCredentialQueryHandler : IQueryHandler<GetCredentialQuery, CredentialDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<GetCredentialQueryHandler> _logger;

    public GetCredentialQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<GetCredentialQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<CredentialDto> HandleAsync(
        GetCredentialQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving credential {CredentialId} for wallet {WalletId}",
            query.CredentialId, query.WalletId);

        var credential = await _credentialRepository.GetByIdAsync(query.CredentialId, cancellationToken);

        if (credential == null)
        {
            throw new EntityNotFoundException("Credential", query.CredentialId.ToString());
        }

        // Verify wallet ownership
        if (credential.WalletId != query.WalletId)
        {
            throw new UnauthorizedException($"Wallet {query.WalletId} does not own credential {query.CredentialId}");
        }

        // Get wallet details
        var wallet = await _walletRepository.GetByIdAsync(query.WalletId, cancellationToken);

        // Get issuer details
        var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);

        // Map to DTO
        return MapToDto(credential, wallet, issuer);
    }

    private static CredentialDto MapToDto(
        Domain.Aggregates.Credential credential,
        Domain.Aggregates.Wallet? wallet,
        Domain.Aggregates.Issuer? issuer)
    {
        return new CredentialDto
        {
            Id = credential.Id.ToString(),
            HolderId = wallet?.PersonId.ToString() ?? string.Empty,
            IssuerId = credential.IssuerId.ToString(),
            Type = credential.CredentialType,
            CredentialSubject = new Dictionary<string, object>
            {
                ["data"] = credential.CredentialData,
                ["schemaId"] = credential.SchemaId,
                ["type"] = credential.CredentialType
            },
            IssuanceDate = credential.IssuedAt.DateTime,
            ExpirationDate = credential.ExpiresAt?.DateTime,
            Status = credential.Status.ToString(),
            Proof = credential.Claims.Count > 0
                ? new Dictionary<string, object>(credential.Claims)
                : null,
            Metadata = new Dictionary<string, string>
            {
                ["walletId"] = credential.WalletId.ToString(),
                ["walletName"] = wallet?.Name ?? "Unknown",
                ["issuerName"] = issuer?.Name ?? "Unknown"
            },
            IsRevoked = credential.RevokedAt.HasValue,
            RevocationDate = credential.RevokedAt?.DateTime,
            RevocationReason = credential.RevocationReason
        };
    }
}