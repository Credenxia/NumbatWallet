using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Auth;

/// <summary>
/// Playwright tests for authenticated user flows including login, sidebar info, and logout.
/// </summary>
[Collection("Playwright")]
public class AuthenticatedFlowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public AuthenticatedFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Should NOT be on the login page
        page.Url.Should().NotContain("/login");
    }

    [Fact]
    public async Task Login_ShowsUserInfoInSidebar()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Sidebar footer should show user card with name and role
        var userCard = page.Locator(".ce-user-card");
        await userCard.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await userCard.IsVisibleAsync()).Should().BeTrue();

        var userName = page.Locator(".ce-user-name");
        (await userName.IsVisibleAsync()).Should().BeTrue();
        var nameText = await userName.TextContentAsync();
        nameText.Should().NotBeNullOrWhiteSpace();

        var userRole = page.Locator(".ce-user-role");
        (await userRole.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Logout_AfterLogin_RedirectsToLogin()
    {
        var page = await _fixture.LoginAsync();

        // Navigate to a protected page first to confirm auth works
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
        page.Url.Should().NotContain("/login");

        // Navigate to logout
        await page.GotoAsync($"{_fixture.BaseUrl}/auth/logout");
        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/login");
    }

    [Fact]
    public async Task Logout_ThenAccessProtectedPage_RedirectsToLogin()
    {
        var page = await _fixture.LoginAsync();

        // Confirm authenticated
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
        page.Url.Should().NotContain("/login");

        // Logout
        await page.GotoAsync($"{_fixture.BaseUrl}/auth/logout");
        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });

        // Try accessing protected page
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/login");
    }
}
