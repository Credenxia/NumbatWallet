using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Application.Services;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, bool>
{
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        ITokenBlacklistService tokenBlacklistService,
        IRefreshTokenStore refreshTokenStore,
        ILogger<LogoutCommandHandler> logger)
    {
        _tokenBlacklistService = tokenBlacklistService;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logout requested for user: {UserId}", command.UserId);

        // Blacklist the access token so it can no longer authenticate.
        if (!string.IsNullOrWhiteSpace(command.Token))
        {
            _tokenBlacklistService.BlacklistToken(command.Token);
            _logger.LogInformation("Access token blacklisted for user: {UserId}", command.UserId);
        }

        // Revoke the refresh token (a distinct opaque value) so it can't mint new access tokens.
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            _refreshTokenStore.Revoke(command.RefreshToken);
            _logger.LogInformation("Refresh token revoked for user: {UserId}", command.UserId);
        }

        await Task.CompletedTask; // Simulate async work

        _logger.LogInformation("User logged out successfully: {UserId}", command.UserId);

        return true;
    }
}