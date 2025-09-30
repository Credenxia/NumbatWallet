using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Credentials;

public record GetCredentialByIdQuery(
    Guid TenantId,
    Guid CredentialId) : IQuery<CredentialDto?>;

public record GetCredentialsByWalletQuery(
    Guid TenantId,
    Guid WalletId,
    bool IncludeRevoked = false) : IQuery<IEnumerable<CredentialDto>>;

public record SearchCredentialsQuery(
    Guid TenantId,
    string? SearchTerm,
    string? CredentialType,
    string? Status,
    int PageNumber,
    int PageSize,
    string? SortBy,
    bool SortDescending) : IQuery<PagedResultDto<CredentialDto>>;

public record GetExpiredCredentialsQuery(
    Guid TenantId,
    Guid WalletId) : IQuery<IEnumerable<CredentialDto>>;

public record GetExpiringCredentialsQuery(
    Guid TenantId,
    Guid WalletId,
    int DaysAhead = 30) : IQuery<IEnumerable<CredentialDto>>;
