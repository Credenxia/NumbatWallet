using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Credentials;

/// <summary>
/// Playwright tests for the Credentials management page.
/// </summary>
[Collection("Playwright")]
public class CredentialPageTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public CredentialPageTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CredentialsPage_LoadsForAuthenticatedUser()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/credentials");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        page.Url.Should().NotContain("/login");

        var heading = page.Locator("h1:has-text('Credential Management')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task CredentialsPage_ShowsFilterControls()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/credentials");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Wait for content to load
        var filterBar = page.Locator(".filter-bar");
        await filterBar.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await filterBar.IsVisibleAsync()).Should().BeTrue();

        // Type, Status, and Issuer filter dropdowns
        var filterSelects = page.Locator(".filter-group select");
        var selectCount = await filterSelects.CountAsync();
        selectCount.Should().BeGreaterThanOrEqualTo(3, "should have type, status, and issuer filter dropdowns");
    }

    [Fact]
    public async Task CredentialsPage_HasIssueButton()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/credentials");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        var issueBtn = page.Locator("button:has-text('Issue Credential')");
        await issueBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await issueBtn.IsVisibleAsync()).Should().BeTrue();
    }
}
