using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthenticationResultDto>
{
    private readonly IPersonRepository _personRepository;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IAccessTokenSigner _accessTokenSigner;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IPersonRepository personRepository,
        IRefreshTokenStore refreshTokenStore,
        IAccessTokenSigner accessTokenSigner,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _personRepository = personRepository;
        _refreshTokenStore = refreshTokenStore;
        _accessTokenSigner = accessTokenSigner;
        _logger = logger;
    }

    public async Task<AuthenticationResultDto> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh requested");

        // Validate refresh token is not null/empty
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            _logger.LogWarning("Token refresh failed - empty refresh token");
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Validate the refresh token exists in our store (the store enforces expiry/TTL).
        var tokenData = _refreshTokenStore.Get(command.RefreshToken);
        if (tokenData is null)
        {
            _logger.LogWarning("Token refresh failed - refresh token not found or expired");
            throw new UnauthorizedException("Invalid refresh token");
        }

        var userId = tokenData.UserId;

        // Try to get person
        Domain.Aggregates.Person? person = null;
        if (Guid.TryParse(userId, out var personId))
        {
            person = await _personRepository.GetByIdAsync(personId, cancellationToken);
            if (person == null)
            {
                _logger.LogWarning("Token refresh failed - user not found: {UserId}", userId);
                throw new UnauthorizedException("User not found");
            }
        }

        // Preserve the roles captured at login so refresh doesn't silently drop privileges.
        var email = person?.Email.Value ?? "user@numbatwallet.wa.gov.au";
        var roles = tokenData.Roles.Count > 0 ? tokenData.Roles.ToArray() : new[] { "User" };

        // Generate new tokens via the configured signer (HS256 or RS256).
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var newAccessToken = _accessTokenSigner.CreateToken(claims, DateTimeOffset.UtcNow.AddHours(1));
        var newRefreshToken = GenerateRefreshToken();

        // Refresh-token rotation: revoke the used token, persist the new one (with roles).
        _refreshTokenStore.Revoke(command.RefreshToken);
        _refreshTokenStore.Store(newRefreshToken, userId, roles, DateTime.UtcNow.AddDays(30));

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

        return new AuthenticationResultDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            UserId = userId,
            Email = email,
            Roles = roles,
            Claims = person != null ? new Dictionary<string, string>
            {
                ["PersonId"] = person.Id.ToString(),
                ["TenantId"] = person.TenantId,
                ["FullName"] = $"{person.FirstName} {person.LastName}"
            } : new()
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}