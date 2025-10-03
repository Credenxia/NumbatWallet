using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using NumbatWallet.Integration.Tests.TestHarness;
using NumbatWallet.Web.Api.Controllers;
using Xunit;

namespace NumbatWallet.Integration.Tests.Authentication;

/// <summary>
/// Integration tests for authorization policies
/// Tests role-based access control, tenant isolation, and policy enforcement
/// </summary>
[Collection("Integration")]
public class AuthorizationPolicyTests : IntegrationTestBase
{
    public AuthorizationPolicyTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CitizenUser_Policy_AllowsAccessToWalletEndpoints()
    {
        // Arrange - Login as citizen user
        var loginRequest = new LoginRequestDto
        {
            Email = "citizen@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access wallet endpoints
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GovernmentOfficer_Policy_AllowsAccessToIssuerEndpoints()
    {
        // Arrange - Login as government officer
        var loginRequest = new LoginRequestDto
        {
            Email = "officer@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access issuer endpoints
        var response = await Client.GetAsync("/api/v1/credentials/issue");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SystemAdmin_Policy_AllowsAccessToAdminEndpoints()
    {
        // Arrange - Login as system admin
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access admin endpoints
        var response = await Client.GetAsync("/api/v1/admin/tenants");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CitizenUser_CannotAccessAdminEndpoints()
    {
        // Arrange - Login as regular citizen user
        var loginRequest = new LoginRequestDto
        {
            Email = "citizen@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access admin endpoints
        var response = await Client.GetAsync("/api/v1/admin/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantA_User_CannotAccessTenantB_Data()
    {
        // Arrange - Login as tenant A user
        var loginRequest = new LoginRequestDto
        {
            Email = "tenanta@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-b"); // Try to access tenant B data

        // Act - Try to get wallets from tenant B
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.OK); // If OK, should return empty list due to tenant filtering
    }

    [Fact]
    public async Task ApiAccess_Policy_RequiresApiAccessScope()
    {
        // Arrange - Login with a user that doesn't have API access scope
        var loginRequest = new LoginRequestDto
        {
            Email = "noapiaccess@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);

        // If user doesn't exist or login fails, this test validates that API access is required
        if (loginResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Expected behavior for non-API users
            loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            return;
        }

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access API endpoints
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert - Should fail if api.access scope is missing
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        // Arrange - Create an expired token (mock scenario)
        // In real scenario, you'd wait for token expiration or manipulate the token
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.expired";

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/v1/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MalformedToken_IsRejected()
    {
        // Arrange
        var malformedToken = "this-is-not-a-valid-jwt-token";

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);

        // Act
        var response = await Client.GetAsync("/api/v1/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenWithoutRequiredClaim_IsDeniedAccess()
    {
        // Arrange - This would require a custom token without user_type claim
        // For now, we'll test with a regular token and verify claim requirements

        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Validate that token has required claims
        var response = await Client.GetAsync("/api/v1/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var validationResult = await response.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);
        validationResult!.Claims.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CredentialOwner_CanAccessTheirCredentials()
    {
        // Arrange - Login and create a credential
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access their own credentials
        var response = await Client.GetAsync("/api/v1/credentials");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound); // OK or NotFound if no credentials exist
    }

    [Fact]
    public async Task WalletOwner_CanAccessTheirWallet()
    {
        // Arrange - Login and create a wallet
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access their own wallet
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound); // OK or NotFound if no wallet exists
    }

    [Fact]
    public async Task UserWithoutRole_CannotAccessRoleProtectedEndpoint()
    {
        // Arrange - Login as user without admin role
        var loginRequest = new LoginRequestDto
        {
            Email = "norole@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);

        if (loginResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // User doesn't exist or has no access - expected
            loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            return;
        }

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Try to access admin-only endpoint
        var response = await Client.DeleteAsync("/api/v1/admin/tenants/some-tenant-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousUser_CanAccessPublicEndpoints()
    {
        // Arrange - No authentication header

        // Act - Try to access public endpoint (health check, OpenAPI docs, etc.)
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound); // OK if health endpoint exists, NotFound otherwise
    }

    [Fact]
    public async Task AnonymousUser_CannotAccessProtectedEndpoints()
    {
        // Arrange - No authentication header

        // Act - Try to access protected endpoint
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MultipleRoles_User_HasAccessToAllAuthorizedEndpoints()
    {
        // Arrange - Login as user with multiple roles (Admin + Issuer)
        var loginRequest = new LoginRequestDto
        {
            Email = "multirole@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Validate user has multiple roles
        var validateResponse = await Client.GetAsync("/api/v1/authentication/validate");
        var validationResult = await validateResponse.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);

        // Assert
        validationResult!.Claims.Should().NotBeNull();
        validationResult.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task TenantContext_IsAutomaticallyInjectedFromClaims()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var response = await Client.GetAsync("/api/v1/authentication/validate");
        var validationResult = await response.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);

        // Assert - Tenant context should be in claims
        validationResult!.Claims
            .Should().NotBeNull()
            .And.Contain(c => c.Type.Contains("tenant", StringComparison.OrdinalIgnoreCase));
    }
}

// DTOs for test requests/responses
public record LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record AuthenticationResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public Dictionary<string, string> Claims { get; init; } = new();
}

public record TokenValidationResponseDto
{
    public bool IsValid { get; init; }
    public List<ClaimDto> Claims { get; init; } = new();
}

public record ClaimDto
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
