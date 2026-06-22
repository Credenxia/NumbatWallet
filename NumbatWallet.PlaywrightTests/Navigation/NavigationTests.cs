using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Navigation;

/// <summary>
/// Playwright tests for sidebar navigation and header elements.
/// </summary>
[Collection("Playwright")]
public class NavigationTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public NavigationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sidebar_ShowsAllNavSections()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Verify all nav section titles are present
        var sectionTitles = page.Locator(".ce-nav-section-title");
        await sectionTitles.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

        var count = await sectionTitles.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(5, "sidebar should have Main, Wallet Management, Security & Compliance, Analytics, and System sections");

        // Check specific sections exist
        (await page.Locator(".ce-nav-section-title:has-text('Main')").IsVisibleAsync()).Should().BeTrue();
        (await page.Locator(".ce-nav-section-title:has-text('Wallet Management')").IsVisibleAsync()).Should().BeTrue();
        (await page.Locator(".ce-nav-section-title:has-text('Security')").IsVisibleAsync()).Should().BeTrue();
        (await page.Locator(".ce-nav-section-title:has-text('Analytics')").IsVisibleAsync()).Should().BeTrue();
        (await page.Locator(".ce-nav-section-title:has-text('System')").IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Sidebar_NavLinksAreClickable()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Click on Wallets nav link
        var walletsLink = page.Locator(".ce-nav-item:has-text('Wallets')").First;
        await walletsLink.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        await walletsLink.ClickAsync();

        // Verify navigation occurred
        await page.WaitForURLAsync(url => url.Contains("/wallets"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/wallets");
    }

    [Fact]
    public async Task Header_ShowsNotificationAndThemeButtons()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Notification bell button
        var notificationBtn = page.Locator(".ce-header-btn[title='Notifications']");
        await notificationBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await notificationBtn.IsVisibleAsync()).Should().BeTrue();

        // Theme toggle button
        var themeToggle = page.Locator(".ce-theme-toggle");
        (await themeToggle.IsVisibleAsync()).Should().BeTrue();
    }
}
