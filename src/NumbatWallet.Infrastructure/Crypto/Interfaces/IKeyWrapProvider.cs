namespace NumbatWallet.Infrastructure.Crypto.Interfaces;

/// <summary>
/// Provides key wrapping and unwrapping functionality for envelope encryption
/// </summary>
public interface IKeyWrapProvider
{
    /// <summary>
    /// Wraps a Data Encryption Key using a Key Encryption Key
    /// </summary>
    Task<byte[]> WrapAsync(byte[] plainDek, string kekId, string tenantId);

    /// <summary>
    /// Unwraps a Data Encryption Key using a Key Encryption Key
    /// </summary>
    Task<byte[]> UnwrapAsync(byte[] wrappedDek, string kekId, string tenantId);

    /// <summary>
    /// Creates a new Key Encryption Key for a tenant
    /// </summary>
    Task<string> CreateKekAsync(string tenantId, KekProperties properties);

    /// <summary>
    /// Rotates a Key Encryption Key
    /// </summary>
    Task RotateKekAsync(string oldKekId, string newKekId, string tenantId);
}

/// <summary>
/// Properties for creating a Key Encryption Key
/// </summary>
public class KekProperties
{
    public string KeyType { get; set; } = "RSA";
    public int KeySize { get; set; } = 3072;
    public bool Exportable { get; set; } = false;
    public DateTimeOffset? ExpiresOn { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}