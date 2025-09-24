using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthenticationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly IInputSanitizationService _sanitizationService;

    public AuthenticationController(
        IConfiguration configuration,
        ILogger<AuthenticationController> logger,
        ISecurityAuditService auditService,
        IInputSanitizationService sanitizationService)
    {
        _configuration = configuration;
        _logger = logger;
        _auditService = auditService;
        _sanitizationService = sanitizationService;
    }

    /// <summary>
    /// Authenticate a user and return a JWT token
    /// </summary>
    [HttpPost("login")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        // Validate input
        if (!_sanitizationService.IsValidEmail(request.Email))
        {
            await _auditService.LogSecurityEventAsync(
                HttpContext,
                SecurityEventType.InvalidInput,
                $"Invalid email format: {request.Email}",
                false);

            return BadRequest("Invalid email format");
        }

        // Log login attempt
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.LoginAttempt,
            $"Login attempt for user: {request.Email}");

        // TODO: Implement actual authentication logic with Azure AD / ServiceWA
        // For now, this is a placeholder implementation

        // Simulate authentication (replace with actual authentication)
        if (request.Email == "admin@numbatwallet.wa.gov.au" && request.Password == "Admin123!")
        {
            // Generate JWT token
            var token = GenerateJwtToken(request.Email, "Admin", Guid.NewGuid().ToString());

            // Log successful login
            await _auditService.LogSecurityEventAsync(
                HttpContext,
                SecurityEventType.LoginSuccess,
                $"Successful login for user: {request.Email}");

            return Ok(new AuthenticationResponseDto
            {
                Token = token,
                RefreshToken = GenerateRefreshToken(),
                ExpiresIn = 3600,
                TokenType = "Bearer",
                UserId = Guid.NewGuid().ToString(),
                Email = request.Email,
                Roles = new[] { "Admin" }
            });
        }

        // Log failed login
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.LoginFailed,
            $"Failed login attempt for user: {request.Email}",
            false);

        return Unauthorized("Invalid credentials");
    }

    /// <summary>
    /// Refresh an expired JWT token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        // Log token refresh attempt
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.TokenRefresh,
            "Token refresh requested");

        // TODO: Implement actual token refresh logic
        // Validate refresh token and issue new access token

        var newToken = GenerateJwtToken("user@example.com", "User", Guid.NewGuid().ToString());

        return Ok(new AuthenticationResponseDto
        {
            Token = newToken,
            RefreshToken = request.RefreshToken,
            ExpiresIn = 3600,
            TokenType = "Bearer"
        });
    }

    /// <summary>
    /// Logout and invalidate tokens
    /// </summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Log logout
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.LogoutSuccess,
            $"User logged out: {userId}");

        // TODO: Implement token blacklisting or revocation

        return NoContent();
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Log password change attempt
        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.PasswordChange,
            $"Password change attempted for user: {userId}");

        // TODO: Implement actual password change logic

        return NoContent();
    }

    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!_sanitizationService.IsValidEmail(request.Email))
        {
            return BadRequest("Invalid email format");
        }

        _logger.LogInformation("Password reset requested for: {Email}", request.Email);

        // TODO: Implement password reset logic
        // Send reset email with secure token

        return NoContent();
    }

    /// <summary>
    /// Validate authentication token
    /// </summary>
    [HttpGet("validate")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(TokenValidationResponseDto), StatusCodes.Status200OK)]
    public IActionResult ValidateToken()
    {
        var claims = User.Claims.Select(c => new ClaimDto
        {
            Type = c.Type,
            Value = c.Value
        }).ToList();

        return Ok(new TokenValidationResponseDto
        {
            IsValid = true,
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Claims = claims
        });
    }

    private string GenerateJwtToken(string email, string role, string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "ThisIsAVerySecureKeyForDevelopmentOnly123456789"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

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
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

// DTOs for authentication
public class LoginRequestDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? TwoFactorCode { get; set; }
}

public class AuthenticationResponseDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public required string TokenType { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string[]? Roles { get; set; }
}

public class RefreshTokenRequestDto
{
    public required string RefreshToken { get; set; }
}

public class ChangePasswordRequestDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class ForgotPasswordRequestDto
{
    public required string Email { get; set; }
}

public class TokenValidationResponseDto
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<ClaimDto>? Claims { get; set; }
}

public class ClaimDto
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}