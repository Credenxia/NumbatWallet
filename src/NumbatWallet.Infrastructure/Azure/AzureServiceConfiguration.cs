using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Azure;

/// <summary>
/// Configuration for Azure services integration
/// POA: Phase 3 - Azure services integration
/// </summary>
public static class AzureServiceConfiguration
{
    public static IServiceCollection AddAzureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureConfig = configuration.GetSection("Azure");
        var useRealAzure = azureConfig.GetValue("UseRealServices", false);

        if (useRealAzure)
        {
            // Configure Azure credential
            var credential = GetAzureCredential(configuration);
            services.AddSingleton(credential);

            // Add Azure Key Vault
            var keyVaultUrl = azureConfig["KeyVault:Url"];
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                services.AddSingleton(sp =>
                {
                    var cred = sp.GetRequiredService<TokenCredential>();
                    return new SecretClient(new Uri(keyVaultUrl), cred);
                });

                services.AddSingleton(sp =>
                {
                    var cred = sp.GetRequiredService<TokenCredential>();
                    return new KeyClient(new Uri(keyVaultUrl), cred);
                });

                services.AddScoped<IAzureKeyVaultService, AzureKeyVaultService>();
            }

            // Add Azure Storage
            var storageConnectionString = azureConfig["Storage:ConnectionString"];
            if (!string.IsNullOrEmpty(storageConnectionString))
            {
                services.AddSingleton(_ =>
                {
                    return new BlobServiceClient(storageConnectionString);
                });

                services.AddScoped<IAzureStorageService, AzureStorageService>();
            }

            // Add Azure Service Bus (for event messaging)
            var serviceBusConnection = azureConfig["ServiceBus:ConnectionString"];
            if (!string.IsNullOrEmpty(serviceBusConnection))
            {
                services.AddSingleton<IAzureServiceBusService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<AzureServiceBusService>>();
                    return new AzureServiceBusService(serviceBusConnection, logger);
                });
            }

            // Add Azure Application Insights
            var appInsightsConnection = azureConfig["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnection))
            {
                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = appInsightsConnection;
                });
            }
        }
        else
        {
            // Use mock services for development/POA
            services.AddScoped<IAzureKeyVaultService, MockAzureKeyVaultService>();
            services.AddScoped<IAzureStorageService, MockAzureStorageService>();
            services.AddSingleton<IAzureServiceBusService, MockAzureServiceBusService>();
        }

        return services;
    }

    private static TokenCredential GetAzureCredential(IConfiguration configuration)
    {
        var azureConfig = configuration.GetSection("Azure");
        var authMethod = azureConfig["AuthMethod"] ?? "ManagedIdentity";

        return authMethod switch
        {
            "ServicePrincipal" => new ClientSecretCredential(
                azureConfig["TenantId"],
                azureConfig["ClientId"],
                azureConfig["ClientSecret"]),
            "CLI" => new AzureCliCredential(),
            "VisualStudio" => new VisualStudioCredential(),
            "ManagedIdentity" => new ManagedIdentityCredential(azureConfig["ClientId"]),
            _ => new DefaultAzureCredential()
        };
    }
}

// Service Interfaces
public interface IAzureKeyVaultService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<string> SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default);
    Task<byte[]> SignDataAsync(string keyName, byte[] data, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(string keyName, byte[] data, byte[] signature, CancellationToken cancellationToken = default);
    Task<string> EncryptDataAsync(string keyName, string plaintext, CancellationToken cancellationToken = default);
    Task<string> DecryptDataAsync(string keyName, string ciphertext, CancellationToken cancellationToken = default);
}

public interface IAzureStorageService
{
    Task<string> UploadBlobAsync(string containerName, string blobName, byte[] data, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<bool> DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
}

public interface IAzureServiceBusService
{
    Task SendMessageAsync(string queueName, string message, CancellationToken cancellationToken = default);
    Task<string?> ReceiveMessageAsync(string queueName, CancellationToken cancellationToken = default);
    Task PublishEventAsync(string topicName, object eventData, CancellationToken cancellationToken = default);
}