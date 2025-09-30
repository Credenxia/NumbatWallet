using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Wallets;

public record ActivateWalletCommand(
    Guid WalletId,
    string? Pin = null) : ICommand<WalletDto>;