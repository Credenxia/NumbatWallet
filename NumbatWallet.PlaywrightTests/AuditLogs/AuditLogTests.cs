using FluentAssertions;
using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.AuditLogs;

/// <summary>
/// Playwright tests for the Audit Logs page (Admin-only).
/// </summary>
[Collection("Playwright")]
public class AuditLogTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public AuditLogTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuditPage_LoadsForAdmin()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/audit");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        page.Url.Should().NotContain("/login");

        var heading = page.Locator("h1:has-text('Audit Logs')");
        await heading.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await heading.IsVisibleAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task AuditPage_ShowsFilterControls()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/audit");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        // Date range inputs
        var dateInputs = page.Locator("input[type='datetime-local']");
        var dateCount = await dateInputs.CountAsync();
        dateCount.Should().BeGreaterThanOrEqualTo(2, "should have start and end date inputs");

        // Severity filter
        var severitySelect = page.Locator("select").Filter(new LocatorFilterOptions { HasText = "Debug" });
        (await severitySelect.CountAsync()).Should().BeGreaterThanOrEqualTo(1, "should have severity filter");

        // Entity Type filter
        var entityTypeSelect = page.Locator("select").Filter(new LocatorFilterOptions { HasText = "Credential" });
        (await entityTypeSelect.CountAsync()).Should().BeGreaterThanOrEqualTo(1, "should have entity type filter");

        // Action filter
        var actionSelect = page.Locator("select").Filter(new LocatorFilterOptions { HasText = "Create" });
        (await actionSelect.CountAsync()).Should().BeGreaterThanOrEqualTo(1, "should have action filter");
    }

    [Fact]
    public async Task AuditPage_HasExportButton()
    {
        var page = await _fixture.LoginAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/audit");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

        var exportBtn = page.Locator("button:has-text('Export')");
        await exportBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        (await exportBtn.IsVisibleAsync()).Should().BeTrue();
    }
}
