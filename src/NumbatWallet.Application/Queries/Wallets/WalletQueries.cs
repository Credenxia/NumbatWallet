using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Wallets;

public record GetMyWalletsQuery(string UserId) : IQuery<IEnumerable<WalletDto>>;

public record GetWalletByIdQuery(Guid WalletId) : IQuery<WalletDto?>;

public record GetWalletByPersonIdQuery(Guid PersonId) : IQuery<WalletDto?>;

public record GetWalletStatisticsQuery(Guid WalletId) : IQuery<object>;

public record SearchWalletsQuery(
    string? SearchTerm,
    string? UserId,
    Guid? PersonId,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<IEnumerable<WalletDto>>;