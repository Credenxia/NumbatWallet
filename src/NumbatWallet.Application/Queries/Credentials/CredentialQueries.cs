using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.Queries.Credentials;

public record GetCredentialByIdQuery(Guid CredentialId) : IQuery<CredentialDto?>;

public record GetCredentialsByWalletQuery(
    Guid WalletId,
    bool? ActiveOnly = null) : IQuery<IEnumerable<CredentialDto>>;

public record SearchCredentialsQuery(
    string? SearchTerm,
    CredentialType? CredentialType,
    Guid? IssuerId,
    Guid? WalletId,
    bool? IsActive,
    DateTime? IssuedAfter,
    DateTime? IssuedBefore) : IQuery<IEnumerable<CredentialDto>>;

public record GetExpiredCredentialsQuery(Guid WalletId) : IQuery<IEnumerable<CredentialDto>>;

public record GetExpiringCredentialsQuery(
    Guid WalletId,
    int DaysAhead = 30) : IQuery<IEnumerable<CredentialDto>>;