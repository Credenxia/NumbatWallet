using System.Text;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Azure;

/// <summary>
/// Azure Key Vault service implementation
/// POA: Phase 3 - Key Vault integration
/// </summary>
public class AzureKeyVaultService : IAzureKeyVaultService
{
    private readonly SecretClient _secretClient;
    private readonly KeyClient _keyClient;
    private readonly ILogger<AzureKeyVaultService> _logger;
    private readonly Dictionary<string, CryptographyClient> _cryptoClients = new();

    public AzureKeyVaultService(
        SecretClient secretClient,
        KeyClient keyClient,
        ILogger<AzureKeyVaultService> logger)
    {
        _secretClient = secretClient;
        _keyClient = keyClient;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully retrieved secret {SecretName}", secretName);
            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found", secretName);
            throw new KeyNotFoundException($"Secret {secretName} not found", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get secret {SecretName}", secretName);
            throw;
        }
    }

    public async Task<string> SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = new KeyVaultSecret(secretName, value)
            {
                Properties =
                {
                    ExpiresOn = DateTimeOffset.UtcNow.AddYears(1),
                    ContentType = "text/plain"
                }
            };

            var response = await _secretClient.SetSecretAsync(secret, cancellationToken);
            _logger.LogInformation("Successfully set secret {SecretName}", secretName);
            return response.Value.Properties.Version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName}", secretName);
            throw;
        }
    }

    public async Task<byte[]> SignDataAsync(string keyName, byte[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);
            var signResult = await cryptoClient.SignDataAsync(SignatureAlgorithm.RS256, data, cancellationToken);

            _logger.LogInformation("Successfully signed data with key {KeyName}", keyName);
            return signResult.Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign data with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<bool> VerifySignatureAsync(
        string keyName,
        byte[] data,
        byte[] signature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);
            var verifyResult = await cryptoClient.VerifyDataAsync(
                SignatureAlgorithm.RS256,
                data,
                signature,
                cancellationToken);

            _logger.LogInformation("Signature verification with key {KeyName}: {IsValid}", keyName, verifyResult.IsValid);
            return verifyResult.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify signature with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<string> EncryptDataAsync(string keyName, string plaintext, CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            var encryptResult = await cryptoClient.EncryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                plaintextBytes,
                cancellationToken);

            _logger.LogInformation("Successfully encrypted data with key {KeyName}", keyName);
            return Convert.ToBase64String(encryptResult.Ciphertext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data with key {KeyName}", keyName);
            throw;
        }
    }

    public async Task<string> DecryptDataAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default)
    {
        try
        {
            var cryptoClient = await GetCryptographyClientAsync(keyName, cancellationToken);
            var ciphertextBytes = Convert.FromBase64String(ciphertext);

            var decryptResult = await cryptoClient.DecryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                ciphertextBytes,
                cancellationToken);

            _logger.LogInformation("Successfully decrypted data with key {KeyName}", keyName);
            return Encoding.UTF8.GetString(decryptResult.Plaintext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data with key {KeyName}", keyName);
            throw;
        }
    }

    private async Task<CryptographyClient> GetCryptographyClientAsync(string keyName, CancellationToken cancellationToken)
    {
        if (_cryptoClients.TryGetValue(keyName, out var client))
        {
            return client;
        }

        // Get or create the key
        try
        {
            var key = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
            client = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Create new key if not exists
            var createKeyOptions = new CreateRsaKeyOptions(keyName)
            {
                KeySize = 2048,
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(2)
            };

            var key = await _keyClient.CreateRsaKeyAsync(createKeyOptions, cancellationToken);
            client = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());

            _logger.LogInformation("Created new RSA key {KeyName}", keyName);
        }

        _cryptoClients[keyName] = client;
        return client;
    }
}

/// <summary>
/// Mock implementation for development/testing
/// </summary>
public class MockAzureKeyVaultService : IAzureKeyVaultService
{
    private readonly Dictionary<string, string> _secrets = new();
    private readonly Dictionary<string, byte[]> _keys = new();

    public MockAzureKeyVaultService(ILogger<MockAzureKeyVaultService> logger)
    {
    }

    public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(secretName, out var secret))
        {
            return Task.FromResult(secret);
        }
        throw new KeyNotFoundException($"Secret {secretName} not found");
    }

    public Task<string> SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        _secrets[secretName] = value;
        return Task.FromResult($"mock-version-{Guid.NewGuid()}");
    }

    public Task<byte[]> SignDataAsync(string keyName, byte[] data, CancellationToken cancellationToken = default)
    {
        // Mock signature
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return Task.FromResult(hash);
    }

    public Task<bool> VerifySignatureAsync(string keyName, byte[] data, byte[] signature, CancellationToken cancellationToken = default)
    {
        // Mock verification
        var hash = System.Security.Cryptography.SHA256.HashData(data);
        return Task.FromResult(hash.SequenceEqual(signature));
    }

    public Task<string> EncryptDataAsync(string keyName, string plaintext, CancellationToken cancellationToken = default)
    {
        // Mock encryption (just base64 encode)
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }

    public Task<string> DecryptDataAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default)
    {
        // Mock decryption (just base64 decode)
        var bytes = Convert.FromBase64String(ciphertext);
        return Task.FromResult(Encoding.UTF8.GetString(bytes));
    }
}