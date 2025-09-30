using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword) : ICommand<bool>;