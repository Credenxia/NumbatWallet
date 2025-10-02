using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Test authentication handler for DEVELOPMENT ONLY
/// Validates real JWT tokens but provides fallback for testing
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsPrincipal? principal = null;

        // Check for Authorization header with Bearer token
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "");
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    // Parse JWT token to extract claims
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = System.Text.Encoding.UTF8.GetBytes(
                        _configuration["Jwt:SecretKey"] ?? "TestSecretKey123456789012345678901234567890");

                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false, // Allow any issuer for testing
                        ValidateAudience = false, // Allow any audience for testing
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                }
                catch
                {
                    // JWT parsing failed - use default claims
                    principal = null;
                }
            }
        }

        // If no valid JWT token, check if endpoint allows anonymous
        if (principal == null)
        {
            var endpoint = Context.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null;

            if (allowAnonymous)
            {
                // For anonymous endpoints, return default test claims
                var claims = new[]
                {
                    new Claim("user_id", "test-user"),
                    new Claim("tenant_id", "test-tenant"),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.NameIdentifier, "test-user")
                };

                var identity = new ClaimsIdentity(claims, "Test");
                principal = new ClaimsPrincipal(identity);

                var ticket = new AuthenticationTicket(principal, "Test");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            else
            {
                // For non-anonymous endpoints without token, return authentication failure (401)
                return Task.FromResult(AuthenticateResult.Fail("No authentication token provided"));
            }
        }

        // Valid JWT token - return success
        var successTicket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(successTicket));
    }
}
