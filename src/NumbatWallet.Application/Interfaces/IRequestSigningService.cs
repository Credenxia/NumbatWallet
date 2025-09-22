using NumbatWallet.Domain.ValueObjects;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for signing and verifying HTTP request signatures
/// </summary>
public interface IRequestSigningService
{
    /// <summary>
    /// Signs a request using the specified algorithm and private key
    /// </summary>
    Task<string> SignRequestAsync(
        string method,
        string path,
        string? body,
        Dictionary<string, string>? headers,
        string privateKey,
        string algorithm = "SHA256");

    /// <summary>
    /// Verifies a request signature using the public key
    /// </summary>
    Task<bool> VerifyRequestSignatureAsync(
        RequestSignature signature,
        string method,
        string path,
        string? body,
        string publicKey);

    /// <summary>
    /// Generates a cryptographically secure nonce
    /// </summary>
    string GenerateNonce();

    /// <summary>
    /// Validates that a nonce hasn't been used before
    /// </summary>
    Task<bool> ValidateNonceAsync(string nonce);

    /// <summary>
    /// Marks a nonce as used to prevent replay attacks
    /// </summary>
    Task MarkNonceAsUsedAsync(string nonce);

    /// <summary>
    /// Parses the signature header into a RequestSignature object
    /// </summary>
    RequestSignature? ParseSignatureHeader(string headerValue);
}