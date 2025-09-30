using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Wallets;

public record GetWalletsByPersonQuery(
    Guid PersonId,
    bool IncludeInactive = false) : IQuery<IEnumerable<WalletDto>>;