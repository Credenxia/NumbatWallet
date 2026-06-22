using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Placeholder;

/// <summary>
/// Playwright tests for placeholder/stub pages that load without errors.
/// </summary>
[Collection("Playwright")]
public class PlaceholderPageTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public PlaceholderPageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/wallet-designer")]
    [InlineData("/certificates")]
    [InlineData("/api-keys")]
    [InlineData("/metrics")]
    [InlineData("/reports")]
    [InlineData("/integrations")]
    [InlineData("/webhooks")]
    [InlineData("/settings")]
    public async Task PlaceholderPages_LoadWithoutErrors(string path)
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}{path}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Should not redirect to login (authenticated)
        page.Url.Should().NotContain("/login");

        // Should not show the Blazor error UI
        var blazorError = page.Locator("#blazor-error-ui");
        var isErrorVisible = await blazorError.IsVisibleAsync();
        isErrorVisible.Should().BeFalse("the page should load without Blazor errors");

        // Should have a page header h1
        var heading = page.Locator("h1");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Theory]
    [InlineData("/wallet-designer")]
    [InlineData("/certificates")]
    public async Task PlaceholderPages_ShowInfoAlert(string path)
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}{path}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // These pages show an alert-info with a "coming soon" or descriptive message
        var infoAlert = page.Locator(".alert-info");
        await infoAlert.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await infoAlert.IsVisibleAsync()).Should().BeTrue();
    }
}
