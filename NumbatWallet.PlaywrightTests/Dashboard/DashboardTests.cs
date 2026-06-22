using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Dashboard;

/// <summary>
/// Playwright tests for the Dashboard page content and widgets.
/// </summary>
[Collection("Playwright")]
public class DashboardTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public DashboardTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Dashboard_LoadsForAuthenticatedUser()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Should not be on login page
        page.Url.Should().NotContain("/login");

        // Should show the dashboard header
        var heading = page.Locator("h1:has-text('Dashboard')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Dashboard_ShowsMetricCards()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

        // Wait for Blazor circuit + API timeout + re-render (can take 10-15s in dev)
        var metricsGrid = page.Locator(".metrics-grid");
        await metricsGrid.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });

        var metricCards = page.Locator(".metric-card");
        var count = await metricCards.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(4, "dashboard should show at least 4 metric cards");
    }

    [Fact]
    public async Task Dashboard_ShowsRecentActivity()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

        // Wait for Blazor circuit + data load to complete before checking content
        var recentActivity = page.Locator("h5:has-text('Recent Activity')");
        await recentActivity.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
        (await recentActivity.IsVisibleAsync()).Should().BeTrue();
    }
}
