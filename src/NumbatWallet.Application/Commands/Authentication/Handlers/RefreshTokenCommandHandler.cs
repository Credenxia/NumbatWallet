using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthenticationResultDto>
{
    private readonly IPersonRepository _personRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    // In production, we'd store refresh tokens in cache or database
    private static readonly Dictionary<string, (string UserId, DateTime Expiry)> _refreshTokens = new();

    public RefreshTokenCommandHandler(
        IPersonRepository personRepository,
        IConfiguration configuration,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _personRepository = personRepository;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Store a refresh token for later validation (POA implementation)
    /// In production, this would be stored in Redis/database
    /// </summary>
    public static void StoreRefreshToken(string refreshToken, string userId, DateTime expiry)
    {
        _refreshTokens[refreshToken] = (userId, expiry);
    }

    /// <summary>
    /// Remove a refresh token (used during logout)
    /// </summary>
    public static void RevokeRefreshToken(string refreshToken)
    {
        _refreshTokens.Remove(refreshToken);
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

        // Validate refresh token exists in our store
        if (!_refreshTokens.TryGetValue(command.RefreshToken, out var tokenData))
        {
            _logger.LogWarning("Token refresh failed - refresh token not found");
            throw new UnauthorizedException("Invalid refresh token");
        }

        // Check if token has expired
        if (tokenData.Expiry < DateTime.UtcNow)
        {
            // Remove expired token
            _refreshTokens.Remove(command.RefreshToken);
            _logger.LogWarning("Token refresh failed - refresh token expired for user: {UserId}", tokenData.UserId);
            throw new UnauthorizedException("Refresh token has expired");
        }

        // Get user from the stored token data
        string userId = tokenData.UserId;

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

        // Default values for POA
        var email = person?.Email.Value ?? "user@numbatwallet.wa.gov.au";
        var roles = new[] { "User" };

        // Generate new tokens
        var newAccessToken = GenerateJwtToken(userId, email, roles);
        var newRefreshToken = GenerateRefreshToken();

        // Remove old refresh token and store new one (refresh token rotation)
        _refreshTokens.Remove(command.RefreshToken);
        _refreshTokens[newRefreshToken] = (userId, DateTime.UtcNow.AddDays(30));

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

    private string GenerateJwtToken(string userId, string email, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? _configuration["Jwt:SecretKey"] ?? "ThisIsADevelopmentSecretKeyThatIs256BitsLong!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "https://numbatwallet.wa.gov.au",
            audience: _configuration["Jwt:Audience"] ?? "https://api.numbatwallet.wa.gov.au",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}