using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NumbatWallet.Integration.Tests.TestHarness;
using Xunit;

namespace NumbatWallet.Integration.Tests.Authentication;

/// <summary>
/// Integration tests for authentication flows
/// Tests login, logout, token refresh, password management
/// </summary>
[Collection("Integration")]
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    public AuthenticationIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "invalid-email",
            Password = "Test123!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "WrongPassword123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateToken_WithValidToken_ReturnsUserClaims()
    {
        // Arrange - First login
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var response = await Client.GetAsync("/api/v1/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var validation = await response.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);
        validation.Should().NotBeNull();
        validation!.IsValid.Should().BeTrue();
        validation.Claims.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateToken_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange - No token

        // Act
        var response = await Client.GetAsync("/api/v1/authentication/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ReturnsNewTokens()
    {
        // Arrange - First login to get refresh token
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        var refreshRequest = new RefreshTokenRequestDto
        {
            RefreshToken = authResult!.RefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.AccessToken.Should().NotBe(authResult.AccessToken); // Should be a new token
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        // Arrange - First login
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var response = await Client.PostAsync("/api/v1/authentication/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is invalidated
        var validateResponse = await Client.GetAsync("/api/v1/authentication/validate");
        validateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange - No token

        // Act
        var response = await Client.PostAsync("/api/v1/authentication/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_ReturnsNoContent()
    {
        // Arrange - First login
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        var changePasswordRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = "Test123!@#",
            NewPassword = "NewTest456!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange - First login
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        var changePasswordRequest = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewTest456!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsNoContent()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto
        {
            Email = "test@example.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto
        {
            Email = "invalid-email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsNoContent_ToPreventEnumeration()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", request);

        // Assert - Should return NoContent even if email doesn't exist to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Authentication_WithTenantId_IsolatesDataByTenant()
    {
        // Arrange - Login with tenant context
        var loginRequest = new LoginRequestDto
        {
            Email = "tenanta@example.com",
            Password = "Test123!@#"
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act - Validate token contains tenant claim
        var validateResponse = await Client.GetAsync("/api/v1/authentication/validate");
        var validation = await validateResponse.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);

        // Assert - JWT should contain tenant_id claim from person's TenantId
        validation!.Claims.Should().Contain(c =>
            c.Type.Contains("tenant", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public async Task Authentication_Flow_CompleteCycle_WorksCorrectly()
    {
        // 1. Login
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        // 2. Validate token
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);
        var validateResponse = await Client.GetAsync("/api/v1/authentication/validate");
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Refresh token
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/authentication/refresh", new RefreshTokenRequestDto
        {
            RefreshToken = authResult.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        // 4. Logout with new token
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAuth!.AccessToken);
        var logoutResponse = await Client.PostAsync("/api/v1/authentication/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify token is invalid after logout
        var postLogoutValidate = await Client.GetAsync("/api/v1/authentication/validate");
        postLogoutValidate.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JWT_Token_ContainsRequiredClaims()
    {
        // Arrange
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/authentication/login", new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Test123!@#"
        });
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponseDto>(JsonOptions);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult!.AccessToken);

        // Act
        var validateResponse = await Client.GetAsync("/api/v1/authentication/validate");
        var validation = await validateResponse.Content.ReadFromJsonAsync<TokenValidationResponseDto>(JsonOptions);

        // Assert - Check for required claims
        validation!.Claims.Should().Contain(c => c.Type.Contains("sub")); // Subject
        validation.Claims.Should().Contain(c => c.Type.Contains("email"));
        validation.Claims.Should().Contain(c => c.Type.Contains("tenant"));
    }

    [Fact(Skip = "Rate limiting test consumes rate limit quota affecting other tests. Run separately.")]
    public async Task RateLimiting_MultipleFailedLogins_GetsThrottled()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act - Attempt multiple failed logins
        // Testing environment has 200 requests/minute limit, so we need 201+ attempts
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 205; i++)
        {
            var response = await Client.PostAsJsonAsync("/api/v1/authentication/login", loginRequest);
            responses.Add(response);
        }

        // Assert - At least one should be rate limited (429 Too Many Requests)
        responses.Should().Contain(r => r.StatusCode == (HttpStatusCode)429);
    }
}

// DTOs for test requests
public record RefreshTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record ChangePasswordRequestDto
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public record ForgotPasswordRequestDto
{
    public string Email { get; init; } = string.Empty;
}
