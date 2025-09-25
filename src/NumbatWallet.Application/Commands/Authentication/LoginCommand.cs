using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Authentication;

public record LoginCommand(
    string Email,
    string Password,
    string? TwoFactorCode = null) : ICommand<AuthenticationResultDto>;