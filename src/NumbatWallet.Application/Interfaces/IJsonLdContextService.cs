namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for handling JSON-LD contexts in Verifiable Credentials
/// </summary>
public interface IJsonLdContextService
{
    /// <summary>
    /// Retrieve a JSON-LD context from URL or cache
    /// </summary>
    Task<Dictionary<string, object>> GetContextAsync(
        string contextUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add JSON-LD context to a credential
    /// </summary>
    Task<string> AddContextToCredentialAsync(
        Dictionary<string, object> credential,
        List<string> contextUrls,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JSON-LD context in a credential
    /// </summary>
    Task<bool> ValidateContextAsync(
        string jsonLd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expand a JSON-LD credential
    /// </summary>
    Task<Dictionary<string, object>> ExpandCredentialAsync(
        string jsonLd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compact a JSON-LD credential
    /// </summary>
    Task<string> CompactCredentialAsync(
        Dictionary<string, object> expanded,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);
}