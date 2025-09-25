namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for signing and verifying JWT-VC credentials
/// </summary>
public interface IJwtSigningService
{
    /// <summary>
    /// Sign a credential using JWT-VC format
    /// </summary>
    Task<string> SignCredentialAsync(
        Dictionary<string, object> credentialData,
        string? keyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify a JWT-VC credential signature
    /// </summary>
    Task<bool> VerifyCredentialAsync(
        string jwt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decode a JWT-VC credential without verification
    /// </summary>
    Task<Dictionary<string, object>> DecodeCredentialAsync(
        string jwt,
        CancellationToken cancellationToken = default);
}