using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Issuances;

/// <summary>
/// Query to get an issuance by ID
/// </summary>
public record GetIssuanceByIdQuery(Guid IssuanceId) : IQuery<IssuanceDto?>;

/// <summary>
/// Query to get issuances by status
/// </summary>
public record GetIssuancesByStatusQuery(
    string Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int? PageSize,
    int? PageNumber) : IQuery<IEnumerable<IssuanceDto>>;

/// <summary>
/// Query to get issuances by wallet
/// </summary>
public record GetIssuancesByWalletQuery(
    Guid WalletId,
    int? PageSize,
    int? PageNumber) : IQuery<IEnumerable<IssuanceDto>>;

/// <summary>
/// Query to get pending issuances for approval
/// </summary>
public record GetPendingIssuancesQuery(
    string? AssignedTo,
    int? PageSize,
    int? PageNumber) : IQuery<IEnumerable<IssuanceDto>>;