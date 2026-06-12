using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Presentations;

public record GetPresentationByIdQuery(
    Guid PresentationId) : IQuery<PresentationSummaryDto?>;

public record GetPresentationsByWalletQuery(
    Guid WalletId) : IQuery<IEnumerable<PresentationSummaryDto>>;
