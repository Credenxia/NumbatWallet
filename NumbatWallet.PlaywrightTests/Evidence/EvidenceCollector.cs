using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests.Evidence;

/// <summary>
/// Collects UX screenshots and API response evidence for test documentation.
/// Run with: dotnet test --filter "EvidenceCollector"
/// </summary>
[Collection("Playwright")]
public class EvidenceCollector : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private readonly string _evidenceDir;

    public EvidenceCollector(PlaywrightFixture fixture)
    {
        _fixture = fixture;
        _evidenceDir = Path.Combine(
            Path.GetDirectoryName(typeof(EvidenceCollector).Assembly.Location)!,
            "..", "..", "..", "..", "Documentation", "Testing", "Evidence");
        Directory.CreateDirectory(_evidenceDir);
    }

    [Fact]
    public async Task EVD_UX_001_LoginPage()
    {
        var page = await _fixture.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        // Wait for form fields to render
        await page.Locator("input[type='email'], input[name='email']").First.WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10000, State = WaitForSelectorState.Visible });
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-001-LoginPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_002_LoginPageWithError()
    {
        var page = await _fixture.NewPageAsync();
        // Submit invalid login to trigger error
        await page.GotoAsync($"{_fixture.BaseUrl}/login?error=Invalid+email+or+password");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(1000); // Allow error message to render
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-002-LoginPageWithError.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_003_DashboardFull()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        // Wait for metric cards to render (Blazor circuit + data load)
        var metricsGrid = page.Locator(".metrics-grid");
        try
        {
            await metricsGrid.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
        }
        catch { /* Continue even if metrics don't load */ }
        await Task.Delay(2000); // Allow full render
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-003-DashboardFull.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_004_DashboardMetricCards()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        var metricsGrid = page.Locator(".metrics-grid");
        try
        {
            await metricsGrid.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
            await metricsGrid.ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-004-DashboardMetricCards.png")
            });
        }
        catch
        {
            // Fallback: screenshot the whole page
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-004-DashboardMetricCards.png"),
                FullPage = true
            });
        }
    }

    [Fact]
    public async Task EVD_UX_005_SidebarNavigation()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        var sidebar = page.Locator(".ce-sidebar, .sidebar, nav").First;
        try
        {
            await sidebar.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await sidebar.ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-005-SidebarNavigation.png")
            });
        }
        catch
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-005-SidebarNavigation.png"),
                FullPage = true
            });
        }
    }

    [Fact]
    public async Task EVD_UX_006_HeaderBar()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        var header = page.Locator(".ce-header, .top-bar, header").First;
        try
        {
            await header.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await header.ScreenshotAsync(new LocatorScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-006-HeaderBar.png")
            });
        }
        catch
        {
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(_evidenceDir, "EVD-UX-006-HeaderBar.png"),
                FullPage = true
            });
        }
    }

    [Fact]
    public async Task EVD_UX_007_WalletsPage()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-007-WalletsPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_008_CredentialsPage()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/credentials");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-008-CredentialsPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_009_TenantsPage()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tenants");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-009-TenantsPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_010_AuditLogsPage()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/audit");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-010-AuditLogsPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_011_PlaceholderPage()
    {
        var page = await _fixture.LoginAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/wallet-designer");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(2000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-011-PlaceholderPage.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_012_LogoutFlow()
    {
        var page = await _fixture.LoginAsync();
        // First verify we're logged in
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        // Now logout
        await page.GotoAsync($"{_fixture.BaseUrl}/auth/logout");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(1000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-012-LogoutFlow.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_UX_013_AuthRedirect()
    {
        var page = await _fixture.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/wallets");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });
        await Task.Delay(1000);
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_evidenceDir, "EVD-UX-013-AuthRedirect.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task EVD_API_001_HealthCheck()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/health");
        var body = await response.TextAsync();

        var healthResult = response.Status == 200 && body.Contains("Healthy") ? "PASS" : "FAIL";
        var evidence = $"""
            === API Evidence: Health Check ===
            Endpoint: GET {_fixture.ApiUrl}/health
            Status Code: {response.Status}
            Response Body: {body}
            Timestamp: {DateTime.UtcNow:O}
            Result: {healthResult}
            """;
        await File.WriteAllTextAsync(Path.Combine(_evidenceDir, "EVD-API-001-HealthCheck.txt"), evidence);
    }

    [Fact]
    public async Task EVD_API_002_InvalidLogin()
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
        var body = await response.TextAsync();

        var requestBody = @"{""email"":""nonexistent@example.com"",""password"":""WrongPassword1!""}";
        var bodySnippet = body.Length > 500 ? body[..500] : body;
        var result = response.Status != 200 ? "PASS" : "FAIL";
        var evidence = $"""
            === API Evidence: Invalid Login Rejection ===
            Endpoint: POST {_fixture.ApiUrl}/api/v1/authentication/login
            Request Body: {requestBody}
            Status Code: {response.Status}
            Response Body: {bodySnippet}
            Timestamp: {DateTime.UtcNow:O}
            Expected: 401 or 500 (not 200)
            Result: {result}
            """;
        await File.WriteAllTextAsync(Path.Combine(_evidenceDir, "EVD-API-002-InvalidLogin.txt"), evidence);
    }

    [Fact]
    public async Task EVD_API_003_ProtectedEndpointNoToken()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/api/v1/authentication/validate");
        var body = await response.TextAsync();

        var protectedBodySnippet = body.Length > 500 ? body[..500] : body;
        var protectedResult = response.Status == 401 ? "PASS" : "FAIL";
        var evidence = $"""
            === API Evidence: Protected Endpoint Without Token ===
            Endpoint: GET {_fixture.ApiUrl}/api/v1/authentication/validate
            Authorization Header: None
            Status Code: {response.Status}
            Response Body: {protectedBodySnippet}
            Timestamp: {DateTime.UtcNow:O}
            Expected: 401
            Result: {protectedResult}
            """;
        await File.WriteAllTextAsync(Path.Combine(_evidenceDir, "EVD-API-003-ProtectedEndpointNoToken.txt"), evidence);
    }

    [Fact]
    public async Task EVD_API_004_LoginRejectsGet()
    {
        var apiContext = await _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });

        var response = await apiContext.GetAsync($"{_fixture.ApiUrl}/api/v1/authentication/login");
        var body = await response.TextAsync();

        var getBodySnippet = body.Length > 500 ? body[..500] : body;
        var getResult = response.Status != 200 ? "PASS" : "FAIL";
        var evidence = $"""
            === API Evidence: Login Endpoint Rejects GET ===
            Endpoint: GET {_fixture.ApiUrl}/api/v1/authentication/login
            Method: GET (should be POST only)
            Status Code: {response.Status}
            Response Body: {getBodySnippet}
            Timestamp: {DateTime.UtcNow:O}
            Expected: Non-200
            Result: {getResult}
            """;
        await File.WriteAllTextAsync(Path.Combine(_evidenceDir, "EVD-API-004-LoginRejectsGet.txt"), evidence);
    }
}
