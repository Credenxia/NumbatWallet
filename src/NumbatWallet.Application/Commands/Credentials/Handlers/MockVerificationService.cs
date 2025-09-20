using Microsoft.Extensions.Logging;

namespace NumbatWallet.Application.Commands.Credentials.Handlers;

public class MockVerificationService : IVerificationService
{
    private readonly ILogger<MockVerificationService> _logger;

    public MockVerificationService(ILogger<MockVerificationService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreatePresentationTokenAsync(
        Guid credentialId,
        string verifierId,
        string purpose,
        Dictionary<string, object> disclosedClaims,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock: Creating presentation token for credential {CredentialId}", credentialId);

        // Generate a mock presentation token
        var token = $"mock-presentation-{Guid.NewGuid():N}";
        return Task.FromResult(token);
    }

    public Task<string> CreateVerificationUrlAsync(
        string presentationToken,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mock: Creating verification URL for token {Token}", presentationToken);

        // Generate a mock verification URL
        var url = $"https://verify.numbatwallet.local/{presentationToken}";
        return Task.FromResult(url);
    }
}