using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NumbatWallet.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NumbatWallet.Application.Interfaces;
using Moq;

namespace NumbatWallet.Web.Api.IntegrationTests.Fixtures;

public class NumbatWalletWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    public NumbatWalletWebApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("numbatwallet_test")
            .WithUsername("test_user")
            .WithPassword("Test123!")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                ["Azure:KeyVault:Url"] = "",
                ["Azure:Storage:ConnectionString"] = "",
                ["EnableSensitiveDataLogging"] = "true",
                ["SKIP_DB_MIGRATION"] = "false",
                ["Jwt:SecretKey"] = "TestSecretKeyThatIsLongEnoughForHS256Algorithm123!",
                ["Jwt:Issuer"] = "NumbatWallet.Test",
                ["Jwt:Audience"] = "NumbatWallet.Test",
                ["Jwt:ExpiryMinutes"] = "60"
            };

            config.AddInMemoryCollection(inMemorySettings);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NumbatWalletDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using test container connection string
            services.AddDbContext<NumbatWalletDbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
                options.EnableSensitiveDataLogging();
            });

            // Replace external services with mocks for testing
            services.RemoveAll<IEmailService>();
            services.AddScoped(_ => Mock.Of<IEmailService>());

            services.RemoveAll<INotificationService>();
            services.AddScoped(_ => Mock.Of<INotificationService>());

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
            context.Database.Migrate();
        });

        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

public class IntegrationTestBase : IClassFixture<NumbatWalletWebApplicationFactory>
{
    protected readonly NumbatWalletWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(NumbatWalletWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Set default headers
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", "test-tenant");
        Client.DefaultRequestHeaders.Add("X-Request-Id", Guid.NewGuid().ToString());
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await Client.PostAsync(url, content);
    }

    protected async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await Client.PutAsync(url, content);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task SeedDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Add seed data here if needed
        await context.SaveChangesAsync();
    }
}