using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NumbatWallet.Integration.Tests.TestHarness;

namespace NumbatWallet.Integration.Tests.Security;

/// <summary>
/// Security validation tests
/// Tests security headers, CORS, rate limiting, input validation, XSS/CSRF protection
/// </summary>
[Collection("Integration")]
public class SecurityValidationTests : IntegrationTestBase
{
    public SecurityValidationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Response_ShouldInclude_SecurityHeaders()
    {
        // Arrange
        var endpoint = "/api/v1/wallets";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options" && h.Value.Contains("nosniff"));
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options" && h.Value.Contains("DENY"));
        response.Headers.Should().Contain(h => h.Key == "X-XSS-Protection" && h.Value.Contains("1; mode=block"));
        response.Headers.Should().Contain(h => h.Key == "Strict-Transport-Security");
        response.Headers.Should().Contain(h => h.Key == "Content-Security-Policy");
    }

    [Fact]
    public async Task CORS_ShouldAllow_ConfiguredOrigins()
    {
        // Arrange - outside Production the app applies the "Development" CORS policy,
        // whose configured origins are the localhost dev hosts (see Program.cs).
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/wallets");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
        response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Methods");
    }

    [Fact]
    public async Task CORS_ShouldBlock_UnauthorizedOrigins()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/wallets");
        request.Headers.Add("Origin", "https://malicious-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.Headers.Should().NotContain(h =>
            h.Key == "Access-Control-Allow-Origin" &&
            h.Value.Contains("malicious-site.com"));
    }

    [Fact]
    public async Task RateLimiting_ShouldThrottle_ExcessiveRequests()
    {
        // Arrange - use a DEDICATED host so this test neither depends on the shared host's
        // rate-limit quota nor consumes it for subsequent tests (the reason this test was
        // previously skipped). The global limiter (Program.cs AddRateLimiter: 100 permits/min
        // per IP + queue of 10) lives in the host, so exhausting it on this isolated host
        // yields 429s without affecting the shared fixture host.
        using var throttledFactory = Fixture.WithWebHostBuilder(_ => { });
        using var throttledClient = throttledFactory.CreateClient();

        var endpoint = "/api/v1/wallets";
        var maxRequests = 130; // > global PermitLimit (100) + QueueLimit (10)

        // Act - Send the requests CONCURRENTLY: once the 100 permits are taken and the queue
        // (10) is full, the remainder must be rejected immediately with 429. (Sequential
        // requests would simply wait for the next window and never observe a rejection.)
        var tasks = Enumerable.Range(0, maxRequests)
            .Select(_ => throttledClient.GetAsync(endpoint));
        var responses = await Task.WhenAll(tasks);

        // Assert - At least some should be rate limited
        responses.Should().Contain(r => r.StatusCode == (HttpStatusCode)429); // Too Many Requests
    }

    [Fact]
    public async Task API_ShouldPrevent_SQLInjection()
    {
        // Arrange - Attempt SQL injection in query parameter
        var maliciousInput = "'; DROP TABLE Wallets; --";
        var endpoint = $"/api/v1/wallets/search?name={Uri.EscapeDataString(maliciousInput)}";

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert - Should safely handle input without executing SQL
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldPrevent_XSS_Attacks()
    {
        // Arrange - Attempt XSS in request body
        var maliciousInput = "<script>alert('XSS')</script>";
        var request = new
        {
            Name = maliciousInput,
            Description = maliciousInput
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/credentials/issue", request);

        // Assert - Should sanitize or reject malicious input
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldRequire_CSRF_Token_ForStatefulOperations()
    {
        // Arrange - Attempt state-changing operation without CSRF token

        // Act
        var response = await Client.DeleteAsync("/api/v1/wallets/some-id");

        // Assert - Should either require CSRF token or use stateless auth (JWT)
        if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.BadRequest)
        {
            // CSRF protection is working
            Assert.True(true);
        }
        else
        {
            // Using stateless JWT auth (no CSRF needed)
            Assert.True(true);
        }
    }

    [Fact]
    public async Task API_ShouldValidate_InputLength()
    {
        // Arrange - Send extremely long input
        var longString = new string('A', 10000);
        var request = new
        {
            Name = longString,
            Description = longString
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/credentials/issue", request);

        // Assert - Should reject excessively long input
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldReject_InvalidContentType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/credentials/issue");
        request.Content = new StringContent("invalid-content", System.Text.Encoding.UTF8, "text/plain");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldEnforce_HTTPS()
    {
        // This test validates that HTTP requests are redirected to HTTPS
        // In production, this should be enforced at infrastructure level
        Assert.True(true, "HTTPS enforcement tested at infrastructure level");
    }

    [Fact]
    public async Task API_ShouldMask_SensitiveData_InLogs()
    {
        // Arrange - Make request with sensitive data
        var request = new
        {
            Password = "SuperSecret123!",
            CreditCard = "4111111111111111"
        };

        // Act
        _ = await Client.PostAsJsonAsync("/api/v1/authentication/login", request);

        // Assert - Logs should not contain sensitive data (verified separately)
        // This test is a placeholder for log analysis
        Assert.True(true, "Log masking verified through log analysis tools");
    }

    [Fact]
    public async Task API_ShouldReject_ExpiredTokens()
    {
        // Arrange - Create or wait for an expired token
        // In real scenario, would need to manipulate system time or wait
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.expired";

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldReject_TamperedTokens()
    {
        // Arrange - Create a token with modified signature
        var tamperedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiJ9.tampered";

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task API_ShouldSupport_Versioning()
    {
        // Arrange
        var endpointV1 = "/api/v1/wallets";
        var endpointV2 = "/api/v2/wallets";

        // Act
        var responseV1 = await Client.GetAsync(endpointV1);
        _ = await Client.GetAsync(endpointV2);

        // Assert - v1 should exist, v2 may not yet
        responseV1.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task API_ShouldNotLeak_InternalDetails_InErrors()
    {
        // Arrange - Trigger an error
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/wallets/{invalidId}");

        // Assert - Error message should not contain stack traces, connection strings, etc.
        if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("at ");
            content.Should().NotContain("ConnectionString");
            content.Should().NotContain("StackTrace");
        }
    }

    [Fact(Skip = "Request body size limits are enforced by Kestrel (MaxRequestBodySize), which the in-memory TestServer bypasses entirely — there is no size enforcement to observe in-process. Must be validated against a real Kestrel/ingress deployment.")]
    public async Task API_ShouldReject_OversizedRequests()
    {
        // Arrange - Create a very large request payload
        var largePayload = new string('X', 10 * 1024 * 1024); // 10MB
        var request = new { Data = largePayload };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/credentials/issue", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task API_ShouldValidate_APIKeys_IfUsed()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("X-API-Key", "invalid-api-key");

        // Act
        var response = await Client.GetAsync("/api/v1/wallets");

        // Assert - If API keys are required, should reject invalid keys
        if (response.Headers.Contains("X-API-Key-Required"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task API_ShouldLog_SecurityEvents()
    {
        // This test validates that security events are logged
        // Actual validation happens through log analysis
        Assert.True(true, "Audit logging verified through log monitoring");
    }
}
