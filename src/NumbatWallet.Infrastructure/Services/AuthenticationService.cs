using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<AuthenticationService> logger)
    {
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsUserActiveAsync(string userId)
    {
        var user = await GetUserByIdAsync(userId);
        return user?.IsActive ?? false;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        return await _cache.GetOrCreateAsync($"user_{userId}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(15);

            // In production, this would query a database or external identity provider
            // For now, returning a mock user for testing
            await Task.Delay(1); // Simulate async operation

            return new UserDto
            {
                Id = userId,
                Email = $"{userId}@example.com",
                DisplayName = $"User {userId}",
                Roles = DetermineUserRoles(userId),
                IsActive = true,
                TenantId = null,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow
            };
        });
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return false;
        }

        // Check if API keys are configured
        var apiKeys = _configuration.GetSection("ApiKey:Keys").Get<Dictionary<string, string>>();
        if (apiKeys == null || apiKeys.Count == 0)
        {
            _logger.LogWarning("No API keys configured");
            return false;
        }

        // Validate the API key
        var isValid = apiKeys.ContainsValue(apiKey);

        if (isValid)
        {
            var keyName = apiKeys.FirstOrDefault(x => x.Value == apiKey).Key;
            _logger.LogInformation("API key validated successfully for: {KeyName}", keyName);
        }
        else
        {
            var maskedKey = apiKey.Length > 8 ? string.Concat(apiKey.AsSpan(0, 8), "...") : apiKey;
            _logger.LogWarning("Invalid API key attempted: {ApiKey}", maskedKey);
        }

        await Task.Delay(1); // Simulate async operation
        return isValid;
    }

    private List<string> DetermineUserRoles(string userId)
    {
        // In production, roles would come from claims or database
        // For testing, assign roles based on user ID patterns
        var roles = new List<string> { "Citizen" };

        if (userId.Contains("admin", StringComparison.OrdinalIgnoreCase))
        {
            roles.Add("Admin");
        }
        else if (userId.Contains("officer", StringComparison.OrdinalIgnoreCase))
        {
            roles.Add("Officer");
        }
        else if (userId.Contains("issuer", StringComparison.OrdinalIgnoreCase))
        {
            roles.Add("Issuer");
        }

        return roles;
    }
}