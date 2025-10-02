using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Common.Exceptions;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NumbatWallet.Application.Commands.Authentication.Handlers;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthenticationResultDto>
{
    private readonly IPersonRepository _personRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IPersonRepository personRepository,
        IAuthenticationService authenticationService,
        IConfiguration configuration,
        ILogger<LoginCommandHandler> logger)
    {
        _personRepository = personRepository;
        _authenticationService = authenticationService;
        _configuration = configuration;
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

        // For POA, we're not storing passwords in Person entity
        // In production, this would validate against Azure AD or ServiceWA
        // For now, check against a hardcoded admin account or use mock validation

        bool isAuthenticated = false;
        string[] roles = Array.Empty<string>();

        // POA Mock Authentication:
        // In production, this would validate against Azure AD or ServiceWA
        // For testing, we use hardcoded passwords to enable proper test coverage

        var testPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin@numbatwallet.wa.gov.au"] = "Test123!@#",
            ["admin@example.com"] = "Test123!@#",
            ["officer@example.com"] = "Test123!@#",
            ["citizen@example.com"] = "Test123!@#",
            ["test@example.com"] = "Test123!@#",
            ["tenant1@example.com"] = "Test123!@#",
            ["john.doe@example.com"] = "Test123!@#"
        };

        // Check if this is a test account with a known password
        if (testPasswords.TryGetValue(command.Email, out var expectedPassword))
        {
            // Validate password matches expected value
            if (command.Password == expectedPassword)
            {
                isAuthenticated = true;

                // Assign roles based on email
                if (command.Email.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    roles = new[] { "Admin", "Officer", "User" };
                }
                else if (command.Email.Contains("officer", StringComparison.OrdinalIgnoreCase))
                {
                    roles = new[] { "Officer", "User" };
                }
                else
                {
                    roles = new[] { "User" };
                }
            }
        }
        else
        {
            // For other users, check if they're verified
            var validStatuses = new[] {
                SharedKernel.Enums.PersonStatus.Active,
                SharedKernel.Enums.PersonStatus.Verified,
                SharedKernel.Enums.PersonStatus.PendingVerification
            };

            if (person.IsVerified && validStatuses.Contains(person.Status) && !string.IsNullOrEmpty(command.Password))
            {
                // In production, validate password against identity provider
                // For POA, accept any password for verified users (not in test list)
                isAuthenticated = true;
                roles = new[] { "User" };
            }
        }

        if (!isAuthenticated)
        {
            _logger.LogWarning("Login failed - invalid credentials for: {Email}", command.Email);
            throw new UnauthorizedException("Invalid credentials");
        }

        // Generate JWT token
        var token = GenerateJwtToken(person.Id.ToString(), command.Email, roles);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token for validation (POA implementation)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30); // 30 days expiry
        RefreshTokenCommandHandler.StoreRefreshToken(refreshToken, person.Id.ToString(), refreshTokenExpiry);

        _logger.LogInformation("Login successful for: {Email}", command.Email);

        return new AuthenticationResultDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 1 hour
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