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
    private readonly IEnumerable<IPasswordValidator> _passwordValidators;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IPersonRepository personRepository,
        IAuthenticationService authenticationService,
        IConfiguration configuration,
        IEnumerable<IPasswordValidator> passwordValidators,
        ILogger<LoginCommandHandler> logger)
    {
        _personRepository = personRepository;
        _authenticationService = authenticationService;
        _configuration = configuration;
        _passwordValidators = passwordValidators;
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

        // Generate JWT token
        var token = GenerateJwtToken(person.Id.ToString(), command.Email, roles);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token for validation (POA implementation)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30); // 30 days expiry
        RefreshTokenCommandHandler.StoreRefreshToken(refreshToken, person.Id.ToString(), refreshTokenExpiry);

        _logger.LogInformation("Login successful for: {Email}", command.Email);

        var expiresAt = DateTime.UtcNow.AddHours(1);

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