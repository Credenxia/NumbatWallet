using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Wallets.DTOs;

namespace NumbatWallet.Application.Wallets.Commands.CreateWallet;

public sealed class CreateWalletCommand : ICommand<WalletDto>
{
    public required Guid PersonId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}