using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Tenants;

/// <summary>
/// Playwright tests for the Tenants management page.
/// </summary>
[Collection("Playwright")]
public class TenantPageTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public TenantPageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TenantsPage_LoadsForAuthenticatedUser()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/tenants");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        page.Url.Should().NotContain("/login");

        var heading = page.Locator("h1:has-text('Tenant Management')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task TenantsPage_ShowsSearchAndFilters()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/tenants");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Wait for content to load
        var filterBar = page.Locator(".filter-bar");
        await filterBar.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await filterBar.IsVisibleAsync()).Should().BeTrue();

        // Search input
        var searchInput = page.Locator(".search-box input[type='text']");
        (await searchInput.IsVisibleAsync()).Should().BeTrue();

        // Status and Type filter dropdowns
        var filterSelects = page.Locator(".filter-group select");
        var selectCount = await filterSelects.CountAsync();
        selectCount.Should().BeGreaterThanOrEqualTo(2, "should have status and type filter dropdowns");
    }

    [Fact]
    public async Task TenantsPage_HasCreateButton()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/tenants");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        var createBtn = page.Locator("button:has-text('New Tenant')");
        await createBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await createBtn.IsVisibleAsync()).Should().BeTrue();
    }
}
