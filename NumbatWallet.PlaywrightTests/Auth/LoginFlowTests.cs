using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Auth;

/// <summary>
/// Playwright functional tests for NumbatWallet Admin login/logout flow.
/// </summary>
[Collection("Playwright")]
public class LoginFlowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public LoginFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/dashboard")]
    [InlineData("/wallets")]
    [InlineData("/certificates")]
    public async Task UnauthenticatedAccess_RedirectsToLogin(string path)
    {
        var page = await _fixture.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}{path}");

        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/login");
    }

    [Fact]
    public async Task LoginPage_ShowsCredentialFields()
    {
        var page = await _fixture.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/login");

        var emailInput = page.Locator("input[type='email'], input[name='email'], input#email");
        var passwordInput = page.Locator("input[type='password'], input[name='password'], input#password");

        await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        (await emailInput.IsVisibleAsync()).Should().BeTrue();
        (await passwordInput.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        var page = await _fixture.NewPageAsync();

        // Use direct API POST to bypass Blazor enhanced navigation
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var formData = apiContext.CreateFormData();
        formData.Set("email", "wrong@test.com");
        formData.Set("password", "WrongPassword!");

        var loginResponse = await page.APIRequest.PostAsync(
            $"{_fixture.BaseUrl}/auth/login",
            new APIRequestContextOptions
            {
                Form = formData,
                IgnoreHTTPSErrors = true,
                MaxRedirects = 0
            });

        // Should redirect to login with error
        var location = loginResponse.Headers.GetValueOrDefault("location") ?? "";
        location.Should().Contain("/login");
        location.Should().Contain("error=");

        // Navigate to the error URL and verify the error is displayed
        var redirectUrl = location.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? location
            : $"{_fixture.BaseUrl}{location}";
        await page.GotoAsync(redirectUrl);

        var errorElement = page.Locator(".alert-danger, .text-danger, [role='alert']");
        await errorElement.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        (await errorElement.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Logout_RedirectsToLogin()
    {
        var page = await _fixture.NewPageAsync();

        // Navigate to logout endpoint directly
        await page.GotoAsync($"{_fixture.BaseUrl}/auth/logout");

        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/login");
    }
}
