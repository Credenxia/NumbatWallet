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

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthenticationResultDto>
{
    private readonly IPersonRepository _personRepository;
    private readonly IEnumerable<IPasswordValidator> _passwordValidators;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IAccessTokenSigner _accessTokenSigner;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IPersonRepository personRepository,
        IEnumerable<IPasswordValidator> passwordValidators,
        IRefreshTokenStore refreshTokenStore,
        IAccessTokenSigner accessTokenSigner,
        ILogger<LoginCommandHandler> logger)
    {
        _personRepository = personRepository;
        _passwordValidators = passwordValidators;
        _refreshTokenStore = refreshTokenStore;
        _accessTokenSigner = accessTokenSigner;
        _logger = logger;
    }

    public async Task<AuthenticationResultDto> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        // For POA, we'll use a simple authentication check
        // In production, this would integrate with Azure AD or ServiceWA

        // Find person by email
        var person = await _personRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (person == null)
        {
            _logger.LogWarning("Login failed - person not found: {Email}", command.Email);
            throw new UnauthorizedException("Invalid credentials");
        }

        // Authentication via password validators
        // - AzureAdPasswordValidator: Government officers (@wa.gov.au)
        // - ServiceWaPasswordValidator: Citizens (other domains)
        // - TestPasswordValidator: Integration testing only

        string[] roles = Array.Empty<string>();
        bool isAuthenticated = false;

        // Find validator that supports this email
        foreach (var validator in _passwordValidators)
        {
            if (validator.SupportsEmail(command.Email))
            {
                _logger.LogDebug("Using {ValidatorType} for {Email}",
                    validator.GetType().Name, command.Email);

                roles = await validator.ValidateAsync(command.Email, command.Password, cancellationToken);

                if (roles.Length > 0)
                {
                    isAuthenticated = true;
                    _logger.LogInformation("Authentication successful for: {Email}", command.Email);
                    break;
                }
            }
        }

        if (!isAuthenticated)
        {
            _logger.LogWarning("Login failed - invalid credentials for: {Email}", command.Email);
            throw new UnauthorizedException("Invalid credentials");
        }

        // Generate the signed access token (HS256 or RS256 depending on the configured signer).
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token = _accessTokenSigner.CreateToken(
            BuildClaims(person.Id.ToString(), command.Email, roles, person.TenantId),
            expiresAt);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token for validation (rotated on refresh, revoked on logout).
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30); // 30 days expiry
        _refreshTokenStore.Store(refreshToken, person.Id.ToString(), roles, refreshTokenExpiry);

        _logger.LogInformation("Login successful for: {Email}", command.Email);

        return new AuthenticationResultDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 1 hour
            ExpiresAt = expiresAt,
            TokenType = "Bearer",
            UserId = person.Id.ToString(),
            Email = command.Email,
            Roles = roles,
            Claims = new Dictionary<string, string>
            {
                ["PersonId"] = person.Id.ToString(),
                ["TenantId"] = person.TenantId,
                ["FullName"] = $"{person.FirstName} {person.LastName}",
                ["EmailVerified"] = person.EmailVerificationStatus.ToString(),
                ["PhoneVerified"] = person.PhoneVerificationStatus.ToString()
            }
        };
    }

    private static List<Claim> BuildClaims(string userId, string email, string[] roles, string tenantId)
    {
        // CLAIM CONTRACT: the subject claims (NameIdentifier + OIDC "sub") MUST both carry the
        // PERSON's Guid — REST GetMyWallets, GraphQL myWallets and the wallet/credential
        // ownership (IDOR) checks all resolve the caller's person by parsing them as a Guid.
        // The email is carried separately in ClaimTypes.Email. Pinned by LoginCommandHandlerTests.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId),
            new Claim("user_id", userId)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
