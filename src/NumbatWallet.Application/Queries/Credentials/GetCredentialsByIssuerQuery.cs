using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Credentials;

public record GetCredentialsByIssuerQuery : IQuery<IEnumerable<CredentialDto>>
{
    public required Guid IssuerId { get; init; }
}

public class GetCredentialsByIssuerQueryHandler : IQueryHandler<GetCredentialsByIssuerQuery, IEnumerable<CredentialDto>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<GetCredentialsByIssuerQueryHandler> _logger;

    public GetCredentialsByIssuerQueryHandler(
        ICredentialRepository credentialRepository,
        IWalletRepository walletRepository,
        IIssuerRepository issuerRepository,
        ILogger<GetCredentialsByIssuerQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _walletRepository = walletRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<CredentialDto>> HandleAsync(
        GetCredentialsByIssuerQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving credentials for issuer {IssuerId}", query.IssuerId);

        // Get credentials issued by this issuer
        var credentials = await _credentialRepository.GetByIssuerIdAsync(query.IssuerId, cancellationToken);

        // Get issuer details once
        var issuer = await _issuerRepository.GetByIdAsync(query.IssuerId, cancellationToken);

        // Get unique wallet IDs
        var walletIds = credentials.Select(c => c.WalletId).Distinct().ToList();
        var wallets = new Dictionary<Guid, Domain.Aggregates.Wallet>();

        // Fetch wallets
        foreach (var walletId in walletIds)
        {
            var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken);
            if (wallet != null)
            {
                wallets[walletId] = wallet;
            }
        }

        // Map to DTOs
        return credentials.Select(c => MapToDto(c, wallets.GetValueOrDefault(c.WalletId), issuer));
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