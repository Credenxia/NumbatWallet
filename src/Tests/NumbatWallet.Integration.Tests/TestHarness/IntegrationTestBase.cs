using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Infrastructure.Data;

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
    protected NumbatWalletDbContext Context => GetDbContext();

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
        TestData = new TestDataHelper(fixture.Services);

        // Set default headers with valid test tenant ID
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Fixture.TestTenantId);

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
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
    /// Generate a real JWT token for testing with proper claims
    /// </summary>
    protected string GenerateMockToken(string userId = "test-user", string[] roles = null!)
    {
        roles ??= new[] { "User" };

        // Use the same secret key as the application
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "TestSecretKey123456789012345678901234567890")); // Must be 256+ bits
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Create claims matching production JWT structure
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, $"{userId}@example.com"),
            new Claim(JwtRegisteredClaimNames.Sub, $"{userId}@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add all roles as separate claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add tenant context for multi-tenancy tests (use fixture's valid test tenant ID)
        claims.Add(new Claim("tenant_id", Fixture.TestTenantId));
        claims.Add(new Claim("user_id", userId));

        // Create token with 1 hour expiry
        var token = new JwtSecurityToken(
            issuer: "https://test.numbatwallet.wa.gov.au",
            audience: "https://api.numbatwallet.wa.gov.au",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

        // Log error details for debugging
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).\nError: {errorContent}");
        }

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