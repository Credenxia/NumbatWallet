using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Presentations.Handlers;

public sealed class GetPresentationByIdQueryHandler : IQueryHandler<GetPresentationByIdQuery, PresentationSummaryDto?>
{
    private readonly IPresentationRepository _presentationRepository;
    private readonly ILogger<GetPresentationByIdQueryHandler> _logger;

    public GetPresentationByIdQueryHandler(
        IPresentationRepository presentationRepository,
        ILogger<GetPresentationByIdQueryHandler> logger)
    {
        _presentationRepository = presentationRepository;
        _logger = logger;
    }

    public async Task<PresentationSummaryDto?> HandleAsync(
        GetPresentationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving presentation {PresentationId}", query.PresentationId);

        var presentation = await _presentationRepository.GetByIdAsync(query.PresentationId, cancellationToken);
        return presentation is null ? null : PresentationSummaryMapper.ToSummaryDto(presentation);
    }
}

public sealed class GetPresentationsByWalletQueryHandler : IQueryHandler<GetPresentationsByWalletQuery, IEnumerable<PresentationSummaryDto>>
{
    private readonly IPresentationRepository _presentationRepository;
    private readonly ILogger<GetPresentationsByWalletQueryHandler> _logger;

    public GetPresentationsByWalletQueryHandler(
        IPresentationRepository presentationRepository,
        ILogger<GetPresentationsByWalletQueryHandler> logger)
    {
        _presentationRepository = presentationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<PresentationSummaryDto>> HandleAsync(
        GetPresentationsByWalletQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving presentations for wallet {WalletId}", query.WalletId);

        var presentations = await _presentationRepository.GetByWalletIdAsync(query.WalletId, cancellationToken);
        return presentations
            .OrderByDescending(p => p.PresentedAt)
            .Select(PresentationSummaryMapper.ToSummaryDto)
            .ToList();
    }
}

internal static class PresentationSummaryMapper
{
    public static PresentationSummaryDto ToSummaryDto(Presentation presentation)
    {
        return new PresentationSummaryDto
        {
            Id = presentation.Id,
            CredentialId = presentation.CredentialId,
            WalletId = presentation.WalletId,
            VerifierId = presentation.VerifierId,
            Purpose = presentation.Purpose,
            Status = presentation.Status.ToString(),
            PresentedAt = presentation.PresentedAt,
            ExpiresAt = presentation.ExpiresAt,
            VerifiedAt = presentation.VerifiedAt,
            VerificationCount = presentation.VerificationCount,
            DisclosedClaimNames = GetClaimNames(presentation.DisclosedClaimsJson)
        };
    }

    private static List<string> GetClaimNames(string disclosedClaimsJson)
    {
        // Claim NAMES only — never the values (presented PII stays out of summaries).
        try
        {
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(disclosedClaimsJson);
            return claims?.Keys.ToList() ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
