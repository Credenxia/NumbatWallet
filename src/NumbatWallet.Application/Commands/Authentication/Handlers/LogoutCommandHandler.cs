using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly ILogger<LogoutCommandHandler> _logger;
    // In production, we'd maintain a token blacklist in cache

    public LogoutCommandHandler(ILogger<LogoutCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout requested for user: {UserId}", command.UserId);

        // In production, we would:
        // 1. Add the token to a blacklist in Redis/cache
        // 2. Clear any server-side session data
        // 3. Revoke refresh tokens

        // For POA, we'll just log the action
        await Task.CompletedTask; // Simulate async work

        _logger.LogInformation("User logged out successfully: {UserId}", command.UserId);

        return true;
    }
}