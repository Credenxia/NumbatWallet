using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Wallets;

public sealed class GetWalletByIdQuery : IQuery<WalletDto>
{
    public Guid WalletId { get; }

    public GetWalletByIdQuery(Guid walletId)
    {
        WalletId = walletId;
    }
}