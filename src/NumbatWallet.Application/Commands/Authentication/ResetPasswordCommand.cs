using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication;

public record ResetPasswordCommand(
    string Email,
    string? ResetToken = null,
    string? NewPassword = null) : ICommand<bool>;