namespace NumbatWallet.Domain.Credentials;

/// <summary>
/// Interface for credential format handlers supporting W3C Verifiable Credentials
/// </summary>
public interface ICredentialFormat
{
    /// <summary>
    /// Serializes credential data to the specific format
    /// </summary>
    string SerializeCredential(Dictionary<string, object> credentialData, string? signingKey = null);

    /// <summary>
    /// Deserializes credential from the specific format
    /// </summary>
    Dictionary<string, object> DeserializeCredential(string serializedCredential);

    /// <summary>
    /// Validates the format of a serialized credential
    /// </summary>
    bool IsValidFormat(string serializedCredential);
}