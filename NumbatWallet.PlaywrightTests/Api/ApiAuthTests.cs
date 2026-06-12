using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Api;

/// <summary>
/// Playwright tests for API authentication endpoints.
/// Tests basic API availability and auth enforcement.
/// </summary>
[Collection("Playwright")]
public class ApiAuthTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public ApiAuthTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ApiHealth_ReturnsHealthy()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/health");
        response.Status.Should().Be(200);

        var body = await response.TextAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ApiLogin_WithInvalidCredentials_Returns401Or500()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.PostAsync($"{_fixture.ApiUrl}/api/v1/authentication/login", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = "nonexistent@example.com",
                password = "WrongPassword1!"
            }
        });

        // Should not return 200 (successful auth)
        response.Status.Should().NotBe(200);
        // Expect 401 (unauthorized) or 500 (person not found error in development)
        response.Status.Should().BeOneOf(401, 500);
    }

    [Fact]
    public async Task ApiProtectedEndpoint_WithoutToken_Returns401()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/api/v1/authentication/validate");
        response.Status.Should().Be(401);
    }

    [Fact]
    public async Task ApiLoginEndpoint_RejectsGetMethod()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/api/v1/authentication/login");
        response.Status.Should().NotBe(200);
    }
}
