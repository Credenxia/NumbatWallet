using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Wallets;

/// <summary>
/// Playwright tests for the Wallets management page.
/// </summary>
[Collection("Playwright")]
public class WalletPageTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public WalletPageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WalletsPage_LoadsForAuthenticatedUser()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        page.Url.Should().NotContain("/login");

        var heading = page.Locator("h1:has-text('Wallet Management')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task WalletsPage_ShowsSearchAndFilters()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Wait for content to load (filter bar appears after loading)
        var filterBar = page.Locator(".filter-bar");
        await filterBar.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await filterBar.IsVisibleAsync()).Should().BeTrue();

        // Search input
        var searchInput = page.Locator(".search-box input[type='text']");
        (await searchInput.IsVisibleAsync()).Should().BeTrue();

        // Status and Tenant filter dropdowns
        var filterSelects = page.Locator(".filter-group select");
        var selectCount = await filterSelects.CountAsync();
        selectCount.Should().BeGreaterThanOrEqualTo(2, "should have status and tenant filter dropdowns");
    }

    [Fact]
    public async Task WalletsPage_HasCreateButton()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        var createBtn = page.Locator("button:has-text('Create Wallet')");
        await createBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await createBtn.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task WalletsPage_RedirectsToLoginWhenUnauthenticated()
    {
        var page = await _fixture.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForURLAsync(url => url.Contains("/login"), new PageWaitForURLOptions { Timeout = 10000 });
        page.Url.Should().Contain("/login");
    }
}
