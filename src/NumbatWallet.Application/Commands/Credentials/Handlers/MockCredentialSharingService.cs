using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class MockCredentialSharingService : ICredentialSharingService
{
    private readonly ILogger<MockCredentialSharingService> _logger;

    public MockCredentialSharingService(ILogger<MockCredentialSharingService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateShareLinkAsync(
        Guid credentialId,
        string shareCode,
        DateTime expiresAt,
        bool requirePin,
        string? pin,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock: Creating share link for credential {CredentialId} with code {Code}",
            credentialId, shareCode);

        // Generate a mock share link
        var link = $"https://share.numbatwallet.local/c/{shareCode}";
        return Task.FromResult(link);
    }
}