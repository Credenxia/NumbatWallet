using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// ServiceWA password validator for Western Australian citizens.
/// Performs OAuth 2.0 token exchange with the ServiceWA identity provider.
/// </summary>
public class ServiceWaPasswordValidator(
    ILogger<ServiceWaPasswordValidator> logger,
    IConfiguration configuration) : IPasswordValidator
{
    public async Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        logger.LogInformation("ServiceWA authentication attempt for: {Email}", email);

        var tokenEndpoint = configuration["ServiceWA:TokenEndpoint"];
        var clientId = configuration["ServiceWA:ClientId"];
        var clientSecret = configuration["ServiceWA:ClientSecret"];

        if (string.IsNullOrEmpty(tokenEndpoint) || string.IsNullOrEmpty(clientId))
        {
            logger.LogError(
                "ServiceWA authentication is not configured. " +
                "Set ServiceWA:TokenEndpoint, ServiceWA:ClientId, and ServiceWA:ClientSecret. " +
                "Authentication denied for: {Email}", email);
            return []; // empty == failure
        }

        try
        {
            using var httpClient = new HttpClient();

            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["scope"] = configuration["ServiceWA:Scope"] ?? "openid profile email",
                ["username"] = email,
                ["password"] = password
            });

            var response = await httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ServiceWA authentication failed for: {Email}. Status: {StatusCode}", email, response.StatusCode);
                return []; // empty == failure
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);

            // Extract verification level and assign roles
            if (tokenResponse.TryGetProperty("access_token", out var accessToken))
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken.GetString());

                // Check for verification level claim
                var verificationLevel = jwt.Claims
                    .FirstOrDefault(c => c.Type is "verification_level" or "vl")?.Value;

                logger.LogInformation(
                    "ServiceWA authentication successful for: {Email}. VerificationLevel: {Level}",
                    email, verificationLevel ?? "unknown");

                // Validation succeeded — return the citizen role (non-empty signals success).
                return ["User"];
            }

            logger.LogWarning("ServiceWA returned success but no access_token for: {Email}", email);
            return []; // empty == failure
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ServiceWA authentication error for: {Email}", email);
            return []; // empty == failure
        }
    }

    public bool SupportsEmail(string email)
    {
        // Support citizen emails (not government domains)
        return !email.EndsWith("@numbatwallet.wa.gov.au", StringComparison.OrdinalIgnoreCase) &&
               !email.EndsWith("@wa.gov.au", StringComparison.OrdinalIgnoreCase);
    }
}
