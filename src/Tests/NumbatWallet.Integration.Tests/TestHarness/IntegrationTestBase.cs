using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Infrastructure.Data;
using Xunit;

namespace NumbatWallet.Integration.Tests.TestHarness;

/// <summary>
/// Base class for integration tests
/// Provides common utilities and helpers
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>
{
    protected IntegrationTestFixture Fixture { get; }
    protected HttpClient Client { get; }
    protected JsonSerializerOptions JsonOptions { get; }
    protected TestDataHelper TestData { get; }

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
        TestData = new TestDataHelper(fixture.Services);

        // Set default headers
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", fixture.TestTenantId);

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Get a scoped service from the test server
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        using var scope = Fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get the test database context
    /// </summary>
    protected NumbatWalletDbContext GetDbContext()
    {
        return GetService<NumbatWalletDbContext>();
    }

    /// <summary>
    /// Add JWT bearer token to requests
    /// </summary>
    protected void SetBearerToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Generate a mock JWT token for testing
    /// </summary>
    protected string GenerateMockToken(string userId = "test-user", string[] roles = null!)
    {
        roles ??= new[] { "User" };

        // This would normally use the JWT service
        // For testing, we return a mock token
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJyb2xlcyI6WyJVc2VyIl0sImlhdCI6MTUxNjIzOTAyMn0.mock";
    }

    /// <summary>
    /// Make a GET request and deserialize response
    /// </summary>
    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// Make a POST request and deserialize response
    /// </summary>
    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
    }

    /// <summary>
    /// Make a PUT request and deserialize response
    /// </summary>
    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PutAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions);
    }

    /// <summary>
    /// Make a DELETE request
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var response = await Client.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>
    /// Assert that a database entity exists
    /// </summary>
    protected async Task AssertEntityExistsAsync<TEntity>(Guid id) where TEntity : class
    {
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var entity = await dbContext.Set<TEntity>().FindAsync(id);
        Assert.NotNull(entity);
    }

    /// <summary>
    /// Assert that a database entity does not exist
    /// </summary>
    protected async Task AssertEntityNotExistsAsync<TEntity>(Guid id) where TEntity : class
    {
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var entity = await dbContext.Set<TEntity>().FindAsync(id);
        Assert.Null(entity);
    }

    /// <summary>
    /// Clean up test data for a specific tenant
    /// </summary>
    protected async Task CleanupTestDataAsync()
    {
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Clean up test-specific data
        // Note: Be careful not to delete seed data
        await dbContext.SaveChangesAsync();
    }
}