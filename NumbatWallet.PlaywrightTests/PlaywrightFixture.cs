using Microsoft.Playwright;

namespace NumbatWallet.PlaywrightTests;

/// <summary>
/// Shared Playwright fixture for NumbatWallet browser lifecycle management.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public string BaseUrl => Environment.GetEnvironmentVariable("NUMBATWALLET_WEB_URL")
        ?? "https://localhost:7275";

    public string ApiUrl => Environment.GetEnvironmentVariable("NUMBATWALLET_API_URL")
        ?? "https://localhost:7190";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") != "false"
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    public async Task<IPage> NewPageAsync()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Logs in via the test-login endpoint which creates an auth cookie directly
    /// without requiring API validation. Uses .NET HttpClient to POST and transfers
    /// cookies to a new Playwright browser context.
    /// </summary>
    public async Task<IPage> LoginAsync(string email = "admin@example.com", string role = "Admin")
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        // Use .NET HttpClient to POST test-login (bypasses Blazor enhanced navigation)
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            AllowAutoRedirect = false,
            UseCookies = false
        };
        using var httpClient = new HttpClient(handler);

        var formContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("role", role)
        ]);

        var response = await httpClient.PostAsync($"{BaseUrl}/auth/test-login", formContent);

        var uri = new Uri(BaseUrl);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var header in setCookieHeaders)
            {
                var nameValue = header.Split(';')[0];
                var eqIndex = nameValue.IndexOf('=');
                if (eqIndex > 0)
                {
                    await context.AddCookiesAsync([new Cookie
                    {
                        Name = nameValue[..eqIndex].Trim(),
                        Value = nameValue[(eqIndex + 1)..],
                        Domain = uri.Host,
                        Path = "/",
                        Secure = true,
                        HttpOnly = true,
                        SameSite = SameSiteAttribute.Lax
                    }]);
                }
            }
        }

        return page;
    }
}
