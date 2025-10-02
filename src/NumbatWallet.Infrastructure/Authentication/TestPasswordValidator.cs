using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Authentication;

/// <summary>
/// TEST ONLY: Password validator for integration testing
/// DO NOT USE IN PRODUCTION
/// Passwords configured via appsettings.Test.json
/// </summary>
public class TestPasswordValidator : IPasswordValidator
{
    private readonly ILogger<TestPasswordValidator> _logger;
    private readonly Dictionary<string, (string Password, string[] Roles)> _testAccounts;

    public TestPasswordValidator(ILogger<TestPasswordValidator> logger)
    {
        _logger = logger;

        // TEST ONLY: These accounts are for integration testing
        // In production, use AzureAdPasswordValidator or ServiceWaPasswordValidator
        _testAccounts = new Dictionary<string, (string, string[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin@numbatwallet.wa.gov.au"] = ("Test123!@#", new[] { "Admin", "Officer", "User" }),
            ["admin@example.com"] = ("Test123!@#", new[] { "Admin", "Officer", "User" }),
            ["officer@example.com"] = ("Test123!@#", new[] { "Officer", "User" }),
            ["citizen@example.com"] = ("Test123!@#", new[] { "User" }),
            ["test@example.com"] = ("Test123!@#", new[] { "User" }),
            ["tenant1@example.com"] = ("Test123!@#", new[] { "User" }),
            ["tenanta@example.com"] = ("Test123!@#", new[] { "User" }),
            ["john.doe@example.com"] = ("Test123!@#", new[] { "User" })
        };
    }

    public Task<string[]> ValidateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (_testAccounts.TryGetValue(email, out var account))
        {
            if (account.Password == password)
            {
                _logger.LogDebug("TEST: Authentication successful for {Email}", email);
                return Task.FromResult(account.Roles);
            }
        }

        _logger.LogDebug("TEST: Authentication failed for {Email}", email);
        return Task.FromResult(Array.Empty<string>());
    }

    public bool SupportsEmail(string email)
    {
        // Only support test accounts
        return _testAccounts.ContainsKey(email);
    }
}
