using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace NumbatWallet.Integration.Tests.TestHarness;

/// <summary>
/// POA-083: Integration test harness with TestContainers
/// Provides isolated test environment with real PostgreSQL database
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly Dictionary<string, string> _testConfiguration;
    private readonly string _testTenantId = Guid.NewGuid().ToString();

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("numbatwallet_test")
            .WithUsername("testuser")
            .WithPassword("Test123!@#")
            .WithPortBinding(5433, 5432)
            .WithCleanUp(true)
            .Build();

        _testConfiguration = new Dictionary<string, string>
        {
            ["Environment"] = "Testing",
            ["MultiTenancy:Enabled"] = "true",
            ["MultiTenancy:DefaultTenantId"] = _testTenantId,
            ["Azure:KeyVault:UseMockService"] = "true",
            ["Azure:Storage:UseMockService"] = "true",
            ["Authentication:UseMockService"] = "true",
            ["Jwt:SecretKey"] = "TestSecretKey123456789012345678901234567890",
            ["Jwt:Issuer"] = "https://test.numbatwallet.wa.gov.au",
            ["Serilog:MinimumLevel:Default"] = "Warning"
        };
    }

    public string TestTenantId => _testTenantId;
    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Update connection string after container starts
        _testConfiguration["ConnectionStrings:DefaultConnection"] = ConnectionString;
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(_testConfiguration);
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<NumbatWalletDbContext>>();

            // Add test DbContext with real PostgreSQL from container
            services.AddDbContext<NumbatWalletDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly("NumbatWallet.Infrastructure");
                    npgsqlOptions.CommandTimeout(60);
                });
                options.EnableSensitiveDataLogging();
            });

            // Replace external services with mocks
            services.AddSingleton<NumbatWallet.Application.Interfaces.IKeyVaultService, MockKeyVaultService>();
            services.AddSingleton<NumbatWallet.Application.Interfaces.IBlobStorageService, MockBlobStorageService>();
            services.AddSingleton<NumbatWallet.Application.Interfaces.IEmailService, MockEmailService>();
            services.AddSingleton<NumbatWallet.Application.Interfaces.INotificationService, MockNotificationService>();

            // Build service provider for initialization
            var sp = services.BuildServiceProvider();

            // Create and migrate database
            using (var scope = sp.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
                dbContext.Database.Migrate();

                // Seed test data
                var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
                seeder.SeedTestDataAsync().GetAwaiter().GetResult();
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
}

/// <summary>
/// Collection definition for integration tests
/// Ensures tests in same collection run sequentially
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}

/// <summary>
/// Mock Key Vault service for testing
/// </summary>
public class MockKeyVaultService : NumbatWallet.Application.Interfaces.IKeyVaultService
{
    private readonly Dictionary<string, string> _secrets = new();

    public Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _secrets.TryGetValue(secretName, out var value);
        return Task.FromResult(value);
    }

    public Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken = default)
    {
        _secrets[secretName] = secretValue;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _secrets.Remove(secretName);
        return Task.FromResult(true);
    }

    public Task<Dictionary<string, string>> GetSecretsAsync(IEnumerable<string> secretNames, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, string>();
        foreach (var name in secretNames)
        {
            if (_secrets.TryGetValue(name, out var value))
            {
                results[name] = value;
            }
        }
        return Task.FromResult(results);
    }

    public void ClearCache()
    {
        // No cache in mock
    }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_secrets.ContainsKey(secretName));
    }
}

/// <summary>
/// Mock Blob Storage service for testing
/// </summary>
public class MockBlobStorageService : NumbatWallet.Application.Interfaces.IBlobStorageService
{
    private readonly Dictionary<string, byte[]> _blobs = new();
    private readonly Dictionary<string, Dictionary<string, string>> _metadata = new();

    public Task<string> UploadAsync(Stream fileStream, string fileName, string? containerName = null,
        Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        fileStream.CopyTo(ms);
        var blobName = $"{containerName ?? "default"}/{fileName}";
        _blobs[blobName] = ms.ToArray();

        if (metadata != null)
        {
            _metadata[blobName] = metadata;
        }

        return Task.FromResult($"https://test.blob.core.windows.net/{blobName}");
    }

    public Task<Stream> DownloadAsync(string blobName, string? containerName = null, CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        if (_blobs.TryGetValue(fullName, out var data))
        {
            return Task.FromResult<Stream>(new MemoryStream(data));
        }
        throw new FileNotFoundException($"Blob not found: {fullName}");
    }

    public Task<bool> DeleteAsync(string blobName, string? containerName = null, CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        _blobs.Remove(fullName);
        _metadata.Remove(fullName);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string blobName, string? containerName = null, CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        return Task.FromResult(_blobs.ContainsKey(fullName));
    }

    public Task<string> GetBlobUrlAsync(string blobName, string? containerName = null, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        return Task.FromResult($"https://test.blob.core.windows.net/{fullName}");
    }

    public Task<IEnumerable<string>> ListBlobsAsync(string? prefix = null, string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        var container = containerName ?? "default";
        var blobs = _blobs.Keys
            .Where(k => k.StartsWith($"{container}/", StringComparison.Ordinal))
            .Select(k => k.Substring(container.Length + 1));

        if (!string.IsNullOrEmpty(prefix))
        {
            blobs = blobs.Where(b => b.StartsWith(prefix, StringComparison.Ordinal));
        }

        return Task.FromResult(blobs);
    }

    public Task<Dictionary<string, string>?> GetMetadataAsync(string blobName, string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        _metadata.TryGetValue(fullName, out var meta);
        return Task.FromResult(meta);
    }

    public Task<bool> SetMetadataAsync(string blobName, Dictionary<string, string> metadata,
        string? containerName = null, CancellationToken cancellationToken = default)
    {
        var fullName = $"{containerName ?? "default"}/{blobName}";
        _metadata[fullName] = metadata;
        return Task.FromResult(true);
    }
}

/// <summary>
/// Mock Email service for testing
/// </summary>
public class MockEmailService : NumbatWallet.Application.Interfaces.IEmailService
{
    private readonly List<(string To, string Subject, string Body)> _sentEmails = new();

    public IReadOnlyList<(string To, string Subject, string Body)> SentEmails => _sentEmails.AsReadOnly();

    public Task SendEmailAsync(string to, string subject, string body, bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        _sentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }

    public Task SendTemplatedEmailAsync(string to, string templateId, Dictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        var body = $"Template: {templateId}, Data: {string.Join(", ", templateData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
        _sentEmails.Add((to, templateId, body));
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        _sentEmails.Add((email, "Welcome", $"Welcome {name}"));
        return Task.CompletedTask;
    }

    public Task SendCredentialIssuedEmailAsync(string email, string credentialType, DateTime? expiryDate,
        CancellationToken cancellationToken = default)
    {
        var body = $"Credential issued: {credentialType}";
        if (expiryDate.HasValue)
            body += $" (expires: {expiryDate.Value}";
        _sentEmails.Add((email, "Credential Issued", body));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        _sentEmails.Add((email, "Password Reset", $"Reset link: {resetLink}"));
        return Task.CompletedTask;
    }

    public Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body,
        CancellationToken cancellationToken = default)
    {
        foreach (var recipient in recipients)
        {
            _sentEmails.Add((recipient, subject, body));
        }
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _sentEmails.Clear();
    }
}

/// <summary>
/// Mock Notification service for testing
/// </summary>
public class MockNotificationService : NumbatWallet.Application.Interfaces.INotificationService
{
    private readonly List<(string UserId, string Title, string Message)> _notifications = new();

    public IReadOnlyList<(string UserId, string Title, string Message)> Notifications => _notifications.AsReadOnly();

    public Task SendPushNotificationAsync(string userId, string title, string message,
        Dictionary<string, object>? data = null, CancellationToken cancellationToken = default)
    {
        _notifications.Add((userId, title, message));
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _notifications.Add((phoneNumber, "SMS", message));
        return Task.CompletedTask;
    }

    public Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> UnregisterDeviceTokenAsync(string deviceToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task SendNotificationAsync(Guid userId, string title, string message,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add((userId.ToString(), title, message));
        return Task.CompletedTask;
    }

    public Task SendUrgentNotificationAsync(Guid userId, string title, string message,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add((userId.ToString(), $"[URGENT] {title}", message));
        return Task.CompletedTask;
    }

    public Task NotifyOrganizationAsync(Guid organizationId, string title, string message,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add(($"org:{organizationId}", title, message));
        return Task.CompletedTask;
    }

    public Task ScheduleReminderAsync(Guid userId, string title, string message, DateTime scheduledAt,
        CancellationToken cancellationToken = default)
    {
        _notifications.Add((userId.ToString(), $"Reminder: {title}", $"{message} (scheduled: {scheduledAt})"));
        return Task.CompletedTask;
    }

    public Task SendBulkNotificationAsync(IEnumerable<Guid> userIds, string title, string message,
        CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            _notifications.Add((userId.ToString(), title, message));
        }
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _notifications.Clear();
    }
}