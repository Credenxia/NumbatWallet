using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Wallets;

/// <summary>
/// Query for retrieving all credentials in a wallet
/// POA: Wallet-specific credential retrieval
/// </summary>
public sealed record GetWalletCredentialsQuery : IQuery<IEnumerable<CredentialDto>>
{
    public Guid WalletId { get; init; }
    public bool ActiveOnly { get; init; } = false;
}

/// <summary>
/// Handler for retrieving all credentials for a specific wallet
/// POA: Implementation for wallet credential relationship
/// </summary>
public sealed class GetWalletCredentialsQueryHandler : IQueryHandler<GetWalletCredentialsQuery, IEnumerable<CredentialDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<GetWalletCredentialsQueryHandler> _logger;

    public GetWalletCredentialsQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<GetWalletCredentialsQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CredentialDto>> HandleAsync(
        GetWalletCredentialsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving credentials for wallet {WalletId}", query.WalletId);

        // Verify wallet exists
        var wallet = await _walletRepository.GetByIdAsync(query.WalletId, cancellationToken);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet {WalletId} not found", query.WalletId);
            throw new EntityNotFoundException("Wallet", query.WalletId.ToString());
        }

        // Get all credentials for this wallet
        var credentials = await _credentialRepository.FindAsync(
            new Domain.Specifications.CredentialByWalletSpecification(query.WalletId),
            cancellationToken);

        // Filter active only if requested
        if (query.ActiveOnly)
        {
            credentials = credentials.Where(c => c.IsActive()).ToList();
        }

        // Map to DTOs
        var credentialDtos = new List<CredentialDto>();
        foreach (var credential in credentials)
        {
            // Get issuer details
            var issuer = await _issuerRepository.GetByIdAsync(credential.IssuerId, cancellationToken);

            credentialDtos.Add(new CredentialDto
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
                    ["WalletId"] = wallet.Id.ToString(),
                    ["WalletName"] = wallet.Name,
                    ["TenantId"] = credential.TenantId,
                    ["SchemaId"] = credential.SchemaId,
                    ["IssuerName"] = issuer?.Name ?? "Unknown Issuer"
                }
            });
        }

        _logger.LogInformation("Retrieved {Count} credentials for wallet {WalletId}",
            credentialDtos.Count, query.WalletId);

        return credentialDtos;
    }
}
