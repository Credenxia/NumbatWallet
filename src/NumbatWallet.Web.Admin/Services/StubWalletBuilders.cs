using NumbatWallet.Web.Admin.Models;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// Stub implementation of Apple Wallet builder - to be implemented via API calls
/// </summary>
public class StubAppleWalletBuilder : IAppleWalletBuilder
{
    private readonly ILogger<StubAppleWalletBuilder> _logger;

    public StubAppleWalletBuilder(ILogger<StubAppleWalletBuilder> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> GeneratePkpassAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        AppleWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Apple Wallet generation not yet implemented - using stub");
        // TODO: Implement via API call to wallet generation endpoint
        return Task.FromResult(Array.Empty<byte>());
    }
}

/// <summary>
/// Stub implementation of Google Wallet builder - to be implemented via API calls
/// </summary>
public class StubGoogleWalletBuilder : IGoogleWalletBuilder
{
    private readonly ILogger<StubGoogleWalletBuilder> _logger;

    public StubGoogleWalletBuilder(ILogger<StubGoogleWalletBuilder> logger)
    {
        _logger = logger;
    }

    public Task<GoogleWalletPass> GenerateGooglePassAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        GoogleWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Google Wallet generation not yet implemented - using stub");
        // TODO: Implement via API call to wallet generation endpoint
        return Task.FromResult(new GoogleWalletPass
        {
            Id = Guid.NewGuid().ToString(),
            ClassId = options.ClassId,
            ObjectId = options.ObjectId
        });
    }

    public Task<string> CreateJwtAsync(GoogleWalletPass pass)
    {
        return Task.FromResult(string.Empty);
    }

    public string GetAddToWalletLink(string jwt)
    {
        return string.Empty;
    }
}

/// <summary>
/// Stub implementation of Web Wallet builder - to be implemented via API calls
/// </summary>
public class StubWebWalletBuilder : IWebWalletBuilder
{
    private readonly ILogger<StubWebWalletBuilder> _logger;

    public StubWebWalletBuilder(ILogger<StubWebWalletBuilder> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateWebWalletAsync(
        WalletTemplateDto walletTemplate,
        Dictionary<string, object> data,
        WebWalletOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Web Wallet generation not yet implemented - using stub");
        // TODO: Implement via API call to wallet generation endpoint
        return Task.FromResult(string.Empty);
    }
}
