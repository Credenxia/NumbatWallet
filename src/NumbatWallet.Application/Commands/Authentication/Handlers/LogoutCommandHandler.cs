using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Services;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        ITokenBlacklistService tokenBlacklistService,
        ILogger<LogoutCommandHandler> logger)
    {
        _tokenBlacklistService = tokenBlacklistService;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout requested for user: {UserId}", command.UserId);

        // Blacklist the access token
        if (!string.IsNullOrWhiteSpace(command.Token))
        {
            _tokenBlacklistService.BlacklistToken(command.Token);
            _logger.LogInformation("Access token blacklisted for user: {UserId}", command.UserId);

            // Also revoke refresh token if it's a refresh token
            // (Token parameter could be either access token or refresh token)
            RefreshTokenCommandHandler.RevokeRefreshToken(command.Token);
        }

        await Task.CompletedTask; // Simulate async work

        _logger.LogInformation("User logged out successfully: {UserId}", command.UserId);

        return true;
    }
}