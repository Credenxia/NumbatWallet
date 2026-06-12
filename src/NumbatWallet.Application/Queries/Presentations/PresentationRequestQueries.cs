using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Presentations;

/// <summary>
/// EXPERIMENTAL (minimal OID4VP subset): resolves a presentation request by id — the target
/// of the request URI a wallet receives.
/// </summary>
public record GetPresentationRequestByIdQuery(
    Guid RequestId) : IQuery<PresentationRequestDto?>;
