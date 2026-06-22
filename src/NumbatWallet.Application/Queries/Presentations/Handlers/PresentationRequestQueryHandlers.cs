using System.Text.Json;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Queries.Presentations.Handlers;

public sealed class GetPresentationRequestByIdQueryHandler
    : IQueryHandler<GetPresentationRequestByIdQuery, PresentationRequestDto?>
{
    private readonly IPresentationRequestRepository _presentationRequestRepository;
    private readonly ILogger<GetPresentationRequestByIdQueryHandler> _logger;

    public GetPresentationRequestByIdQueryHandler(
        IPresentationRequestRepository presentationRequestRepository,
        ILogger<GetPresentationRequestByIdQueryHandler> logger)
    {
        _presentationRequestRepository = presentationRequestRepository;
        _logger = logger;
    }

    public async Task<PresentationRequestDto?> HandleAsync(
        GetPresentationRequestByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving presentation request {RequestId}", query.RequestId);

        var request = await _presentationRequestRepository.GetByIdAsync(query.RequestId, cancellationToken);
        return request is null ? null : ToDto(request);
    }

    private static PresentationRequestDto ToDto(PresentationRequest request) => new()
    {
        RequestId = request.Id,
        VerifierId = request.VerifierId,
        Purpose = request.Purpose,
        RequestedCredentialType = request.RequestedCredentialType,
        RequestedClaims = JsonSerializer.Deserialize<List<string>>(request.RequestedClaimsJson) ?? new List<string>(),
        PresentationDefinition = request.PresentationDefinitionJson,
        Nonce = request.Nonce,
        Status = request.Status.ToString(),
        ExpiresAt = request.ExpiresAt,
        FulfilledAt = request.FulfilledAt
    };
}
