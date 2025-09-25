using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Authentication;

public record RefreshTokenCommand(
    string RefreshToken,
    string? UserId = null) : ICommand<AuthenticationResultDto>;