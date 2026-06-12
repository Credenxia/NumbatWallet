using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication;

public record LogoutCommand(
    string UserId,
    string? Token = null,
    string? RefreshToken = null) : ICommand<bool>;