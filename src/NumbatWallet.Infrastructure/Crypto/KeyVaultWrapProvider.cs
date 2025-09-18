using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Crypto.Interfaces;

namespace NumbatWallet.Infrastructure.Crypto;

/// <summary>
/// Azure Key Vault implementation of key wrapping provider for envelope encryption
/// </summary>
public class KeyVaultWrapProvider : IKeyWrapProvider
{
    private readonly KeyClient _keyClient;
    private readonly ILogger<KeyVaultWrapProvider> _logger;
    private readonly string _keyVaultUri;

    public KeyVaultWrapProvider(
        IConfiguration configuration,
        ILogger<KeyVaultWrapProvider> logger)
    {
        _logger = logger;
        _keyVaultUri = configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri configuration is required");

        _keyClient = new KeyClient(new Uri(_keyVaultUri), new DefaultAzureCredential());
    }

    public async Task<byte[]> WrapAsync(byte[] plainDek, string kekId, string tenantId)
    {
        try
        {
            var keyName = GetKeyName(tenantId, kekId);
            var key = await _keyClient.GetKeyAsync(keyName);

            var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
            var wrapResult = await cryptoClient.WrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, plainDek);

            _logger.LogInformation("DEK wrapped successfully for tenant {TenantId} using KEK {KekId}",
                tenantId, kekId);

            return wrapResult.EncryptedKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wrap DEK for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<byte[]> UnwrapAsync(byte[] wrappedDek, string kekId, string tenantId)
    {
        try
        {
            var keyName = GetKeyName(tenantId, kekId);
            var key = await _keyClient.GetKeyAsync(keyName);

            var cryptoClient = new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
            var unwrapResult = await cryptoClient.UnwrapKeyAsync(KeyWrapAlgorithm.RsaOaep256, wrappedDek);

            _logger.LogDebug("DEK unwrapped successfully for tenant {TenantId}", tenantId);

            return unwrapResult.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unwrap DEK for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<string> CreateKekAsync(string tenantId, KekProperties properties)
    {
        try
        {
            var keyName = GetKeyName(tenantId, Guid.NewGuid().ToString());

            var keyOptions = new CreateRsaKeyOptions(keyName)
            {
                KeySize = properties.KeySize,
                ExpiresOn = properties.ExpiresOn,
                Enabled = true,
                NotBefore = DateTimeOffset.UtcNow
            };

            // Set key operations
            keyOptions.KeyOperations.Add(KeyOperation.WrapKey);
            keyOptions.KeyOperations.Add(KeyOperation.UnwrapKey);

            // Add tags
            foreach (var tag in properties.Tags)
            {
                keyOptions.Tags.Add(tag.Key, tag.Value);
            }
            keyOptions.Tags.Add("TenantId", tenantId);
            keyOptions.Tags.Add("Purpose", "KEK");
            keyOptions.Tags.Add("CreatedAt", DateTimeOffset.UtcNow.ToString("O"));

            var key = await _keyClient.CreateRsaKeyAsync(keyOptions);

            _logger.LogInformation("Created new KEK {KeyName} for tenant {TenantId}",
                keyName, tenantId);

            return key.Value.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create KEK for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task RotateKekAsync(string oldKekId, string newKekId, string tenantId)
    {
        try
        {
            // Create new KEK
            var newKekName = await CreateKekAsync(tenantId, new KekProperties());

            // Mark old KEK for deletion (soft delete)
            var oldKeyName = GetKeyName(tenantId, oldKekId);
            var deleteOperation = await _keyClient.StartDeleteKeyAsync(oldKeyName);

            _logger.LogInformation(
                "Initiated KEK rotation for tenant {TenantId}. Old: {OldKek}, New: {NewKek}",
                tenantId, oldKekId, newKekName);

            // Note: Actual re-wrapping of DEKs would be handled by a background job
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate KEK for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private string GetKeyName(string tenantId, string kekId)
    {
        // Format: kek-{tenantId}-{kekId}
        return $"kek-{tenantId}-{kekId}".Replace("_", "-").ToLowerInvariant();
    }
}