using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// Azure Active Directory (Entra ID) password validator.
/// For government officers and administrators.
/// Validates against Azure AD using ROPC flow or token exchange.
/// </summary>
public class AzureAdPasswordValidator(
    ILogger<AzureAdPasswordValidator> logger,
    IConfiguration configuration) : IPasswordValidator
{
    public async Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        logger.LogInformation("Azure AD authentication attempt for: {Email}", email);

        var clientId = configuration["AzureAd:ClientId"];
        var tenantId = configuration["AzureAd:TenantId"];
        var authority = configuration["AzureAd:Authority"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(tenantId))
        {
            logger.LogError(
                "Azure AD authentication is not configured. " +
                "Set AzureAd:ClientId and AzureAd:TenantId in configuration. " +
                "Authentication denied for: {Email}", email);
            return []; // empty == failure
        }

        // Use MSAL token acquisition via ROPC (Resource Owner Password Credentials)
        // Note: ROPC is not recommended for production; prefer interactive auth or device code flow.
        // This is used only for server-to-server validation where users provide credentials directly.
        try
        {
            using var httpClient = new HttpClient();
            var tokenEndpoint = $"{authority ?? $"https://login.microsoftonline.com/{tenantId}"}/oauth2/v2.0/token";

            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = clientId,
                ["scope"] = configuration["AzureAd:Scope"] ?? $"api://{clientId}/.default",
                ["username"] = email,
                ["password"] = password
            });

            var response = await httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure AD authentication failed for: {Email}. Status: {StatusCode}", email, response.StatusCode);
                return []; // empty == failure
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(responseJson);

            // Extract roles from access token claims
            if (tokenResponse.TryGetProperty("access_token", out var accessToken))
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken.GetString());

                var roles = jwt.Claims
                    .Where(c => c.Type is "roles" or "role")
                    .Select(c => c.Value)
                    .ToArray();

                logger.LogInformation("Azure AD authentication successful for: {Email}. Roles: {Roles}", email, string.Join(", ", roles));

                // Validation succeeded — return the user's roles (non-empty signals success).
                // Default to "Officer" when Azure AD returns no role claims.
                return roles.Length > 0 ? roles : ["Officer"];
            }

            logger.LogWarning("Azure AD returned success but no access_token for: {Email}", email);
            return []; // empty == failure
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure AD authentication error for: {Email}", email);
            return []; // empty == failure
        }
    }

    public bool SupportsEmail(string email)
    {
        // Support government domain emails
        return email.EndsWith("@numbatwallet.wa.gov.au", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@wa.gov.au", StringComparison.OrdinalIgnoreCase);
    }
}
