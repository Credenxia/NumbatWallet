using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// ServiceWA password validator
/// For Western Australian citizens
/// PRODUCTION: Integrates with ServiceWA for citizen authentication
/// POA: Placeholder implementation - needs ServiceWA API integration
/// </summary>
public class ServiceWaPasswordValidator : IPasswordValidator
{
    private readonly ILogger<ServiceWaPasswordValidator> _logger;

    public ServiceWaPasswordValidator(ILogger<ServiceWaPasswordValidator> logger)
    {
        _logger = logger;
    }

    public async Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ServiceWA authentication attempt for: {Email}", email);

        // TODO POA: Implement ServiceWA authentication
        // 1. Integrate with ServiceWA OAuth/OIDC endpoints
        // 2. Validate user credentials via ServiceWA API
        // 3. Retrieve user identity verification level
        // 4. Return appropriate roles based on verification status

        // Example implementation (requires ServiceWA SDK/API client):
        /*
        try
        {
            var serviceWaClient = new ServiceWaClient(configuration);

            var authResult = await serviceWaClient.AuthenticateAsync(
                email,
                password,
                cancellationToken);

            if (authResult.IsSuccess)
            {
                // Assign roles based on ServiceWA verification level
                var roles = authResult.VerificationLevel switch
                {
                    VerificationLevel.Full => new[] { "Citizen", "Verified", "User" },
                    VerificationLevel.Basic => new[] { "Citizen", "User" },
                    _ => new[] { "User" }
                };

                return roles;
            }

            return Array.Empty<string>();
        }
        catch (ServiceWaException ex)
        {
            _logger.LogWarning(ex, "ServiceWA authentication failed for: {Email}", email);
            return Array.Empty<string>();
        }
        */

        // POA: Return empty array (authentication fails)
        // Integration tests use TestPasswordValidator instead
        _logger.LogWarning("ServiceWA integration not yet implemented - authentication denied for: {Email}", email);
        return await Task.FromResult(Array.Empty<string>());
    }

    public bool SupportsEmail(string email)
    {
        // Support citizen emails (not government domains)
        return !email.EndsWith("@numbatwallet.wa.gov.au", StringComparison.OrdinalIgnoreCase) &&
               !email.EndsWith("@wa.gov.au", StringComparison.OrdinalIgnoreCase);
    }
}
