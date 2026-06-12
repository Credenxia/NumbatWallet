using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using NumbatWallet.Web.Admin.Authentication;
using NumbatWallet.Web.Admin.Components;
using NumbatWallet.Web.Admin.Services;
using NumbatWallet.Web.Admin.Models;
using NumbatWallet.Web.Admin.Hubs;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Blazored.SessionStorage;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NumbatWallet Admin Portal");

    var builder = WebApplication.CreateBuilder(args);

    // Add service defaults & Aspire components
    builder.AddServiceDefaults();

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console());

    // Add services to the container
    // Admin portal communicates through API only (Clean Architecture)
    // No Application or Infrastructure layer dependencies

    // Add authentication
    if (builder.Environment.IsDevelopment())
    {
        // Development: use cookie auth with local login endpoint
        builder.Services.AddAuthentication("NumbatWalletAuth")
            .AddCookie("NumbatWalletAuth", options =>
            {
                options.LoginPath = "/login";
            });

        builder.Services.AddControllersWithViews();
    }
    else
    {
        // Production: use Azure AD authentication
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();
    }

    // Credentry SSO federation (config-gated): adds a "Sign in with Credentry" OIDC option
    // (authorization code + PKCE against the Credentry IdP, client numbatwallet-admin) that
    // signs in to the SAME cookie session the existing logins use — the rest of the app is
    // unaffected. The existing dev cookie login remains the default path.
    // Contract: credentry/docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md.
    var credentryEnabled = builder.Configuration.GetValue<bool>("Credentry:Enabled");
    if (credentryEnabled)
    {
        var credentryCfg = builder.Configuration.GetSection("Credentry");
        var credentrySignInScheme = builder.Environment.IsDevelopment()
            ? "NumbatWalletAuth"
            : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;

        builder.Services.AddAuthentication()
            .AddOpenIdConnect("Credentry", "Sign in with Credentry", options =>
            {
                options.Authority = credentryCfg["Authority"];
                options.RequireHttpsMetadata = credentryCfg.GetValue("RequireHttpsMetadata", true);
                options.ClientId = credentryCfg["ClientId"] ?? "numbatwallet-admin";
                options.ClientSecret = credentryCfg["ClientSecret"];
                options.ResponseType = "code";
                options.UsePkce = true; // mandatory at the Credentry IdP
                options.CallbackPath = credentryCfg["CallbackPath"] ?? "/signin-credentry";
                options.SignedOutCallbackPath = credentryCfg["SignedOutCallbackPath"] ?? "/signout-callback-credentry";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.MapInboundClaims = false;
                // Reuse the app's existing cookie session — do NOT introduce a second cookie scheme.
                options.SignInScheme = credentrySignInScheme;

                options.Scope.Clear();
                var scopes = credentryCfg.GetSection("Scopes").Get<string[]>()
                    ?? ["openid", "profile", "email", "offline_access", "credentry.api", "credentry.roles", "numbatwallet.api"];
                foreach (var scope in scopes)
                {
                    options.Scope.Add(scope);
                }

                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is not ClaimsIdentity identity)
                        {
                            return Task.CompletedTask;
                        }

                        // Credentry sends role/tid/product claims in the ACCESS token only
                        // (the id_token carries core identity). The access token came straight
                        // from the token endpoint over this TLS exchange, so its claims are
                        // trustworthy here without re-validation.
                        var accessToken = context.TokenEndpointResponse?.AccessToken;
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                            foreach (var claim in jwt.Claims.Where(c =>
                                         c.Type is "role" or "tid" or "product" or "amr" or "email" or "name"))
                            {
                                if (!identity.HasClaim(claim.Type, claim.Value))
                                {
                                    identity.AddClaim(new Claim(claim.Type, claim.Value));
                                }
                            }

                            // Parity with the dev cookie login, which stores tokens as claims
                            // for downstream API calls.
                            identity.AddClaim(new Claim("access_token", accessToken));
                            var refreshToken = context.TokenEndpointResponse?.RefreshToken;
                            if (!string.IsNullOrEmpty(refreshToken))
                            {
                                identity.AddClaim(new Claim("refresh_token", refreshToken));
                            }
                        }

                        // NW.* convention → ASP.NET role claims (NW.Admin → Admin etc.), so the
                        // existing AdminOnly/OfficerOrAdmin policies apply. Credentry platform
                        // roles (SuperAdmin, TenantAdmin, …) are deliberately not mapped.
                        foreach (var nwRole in identity.FindAll("role")
                                     .Select(c => c.Value)
                                     .Where(v => v.StartsWith("NW.", StringComparison.Ordinal))
                                     .Select(v => v[3..])
                                     .Where(v => v.Length > 0)
                                     .Distinct(StringComparer.Ordinal)
                                     .ToList())
                        {
                            if (!identity.HasClaim(ClaimTypes.Role, nwRole))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, nwRole));
                            }
                        }

                        // Tenant translation table (Credentry tid → NumbatWallet tenant id);
                        // unmapped tenants fail closed (no tenant_id claim).
                        var tid = identity.FindFirst("tid")?.Value;
                        if (tid is not null && Guid.TryParse(tid, out var tidGuid))
                        {
                            var mapped = credentryCfg.GetSection("TenantMap")[tidGuid.ToString("D")];
                            if (!string.IsNullOrWhiteSpace(mapped) && identity.FindFirst("tenant_id") is null)
                            {
                                identity.AddClaim(new Claim("tenant_id", mapped));
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }

    builder.Services.AddAuthorizationCore(options =>
    {
        options.FallbackPolicy = options.DefaultPolicy;

        // Admin-only policy
        options.AddPolicy("AdminOnly", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Admin");
        });

        // Officer or Admin policy
        options.AddPolicy("OfficerOrAdmin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Officer", "Admin");
        });
    });

    builder.Services.AddCascadingAuthenticationState();

    // Add Blazor services
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Configure Blazor Server with error handling
    builder.Services.AddServerSideBlazor(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        // Set circuit options to handle errors gracefully
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.DisconnectedCircuitMaxRetained = 100;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    });

    // Add SignalR
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 102400; // 100KB
    });

    // Add Authentication services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CustomAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
        provider.GetRequiredService<CustomAuthenticationStateProvider>());

    // Add Blazored services
    builder.Services.AddBlazoredSessionStorage();

    // Add REST API client for file operations only (with Polly policies)
    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        // Use Aspire service discovery - "webapi" is the service name in AppHost
        var apiUrl = builder.Configuration["services:webapi:https:0"]
                    ?? builder.Configuration["services:webapi:http:0"]
                    ?? "http://localhost:5042";
        client.BaseAddress = new Uri(apiUrl);
        client.Timeout = TimeSpan.FromSeconds(5);  // Shorter timeout to prevent hanging
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Add API client for general API operations
    builder.Services.AddHttpClient("ApiClient", client =>
    {
        // Use Aspire service discovery - "webapi" is the service name in AppHost
        var apiUrl = builder.Configuration["services:webapi:https:0"]
                    ?? builder.Configuration["services:webapi:http:0"]
                    ?? "http://localhost:5042";
        client.BaseAddress = new Uri(apiUrl);
        client.Timeout = TimeSpan.FromSeconds(5);  // Shorter timeout to prevent hanging
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Typed GraphQL client for primary data operations (Dashboard/Tenants/Wallets/Credentials).
    // Uses Aspire service discovery for the base address and a configurable service-to-service
    // API key (AdminApi:ApiKey / AdminApi:TenantId in appsettings — never hardcoded).
    builder.Services.AddHttpClient<IAdminGraphQLClient, AdminGraphQLClient>(client =>
    {
        var apiUrl = builder.Configuration["services:webapi:https:0"]
                    ?? builder.Configuration["services:webapi:http:0"]
                    ?? "http://localhost:5042";
        client.BaseAddress = new Uri(apiUrl);
        client.Timeout = TimeSpan.FromSeconds(10);

        var apiKey = builder.Configuration["AdminApi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        var tenantId = builder.Configuration["AdminApi:TenantId"];
        if (!string.IsNullOrEmpty(tenantId))
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        }
    })
    .AddPolicyHandler(GetRetryPolicy());

    // Add file API client for file operations only
    builder.Services.AddHttpClient<IFileApiClient, FileApiClient>(client =>
    {
        // Use Aspire service discovery - "webapi" is the service name in AppHost
        var apiUrl = builder.Configuration["services:webapi:https:0"]
                    ?? builder.Configuration["services:webapi:http:0"]
                    ?? "http://localhost:5042";
        client.BaseAddress = new Uri(apiUrl);
        client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for file operations
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Add application services - using GraphQL/API-based implementations
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<ITenantService, GraphQLTenantService>();  // GraphQL-based instead of direct DB
    builder.Services.AddScoped<IAuditLogService, GraphQLAuditLogService>();  // GraphQL-based instead of direct DB
    builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();
    builder.Services.AddScoped<IWalletTemplateService, GraphQLWalletTemplateService>(); // GraphQL-based for Admin Portal

    // Wallet builders - stub implementations (to be completed via API calls)
    builder.Services.AddScoped<IAppleWalletBuilder, StubAppleWalletBuilder>();
    builder.Services.AddScoped<IGoogleWalletBuilder, StubGoogleWalletBuilder>();
    builder.Services.AddScoped<IWebWalletBuilder, StubWebWalletBuilder>();

    // Add health checks - only for admin portal itself, not database
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapStaticAssets()
        .AllowAnonymous();

    // Map endpoints BEFORE Blazor to ensure proper precedence
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive");  // Aspire liveness probe
    app.MapControllers();
    app.MapHub<DashboardHub>("/hubs/dashboard");

    // Map Blazor components last (includes root route /)
    // NOTE: Authorization is handled per-page via [Authorize] attributes
    // NOTE: Prerendering disabled to avoid session storage / JS interop issues during initial render
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode(options => options.DisableWebSocketCompression = false);

    // Credentry SSO: challenge endpoint for the "Sign in with Credentry" button. The OIDC
    // handler completes at CallbackPath and persists the session in the existing cookie scheme.
    if (credentryEnabled)
    {
        app.MapGet("/login/credentry", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl ?? "/dashboard" },
                ["Credentry"]))
            .AllowAnonymous();

        // Single logout: clears the local cookie session AND the Credentry IdP session
        // (RP-initiated logout with id_token_hint).
        app.MapGet("/logout/credentry", (HttpContext context) =>
        {
            var cookieScheme = app.Environment.IsDevelopment()
                ? "NumbatWalletAuth"
                : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/login" },
                [cookieScheme, "Credentry"]);
        }).AllowAnonymous();
    }

    // Server-side auth endpoints for cookie management (development mode)
    if (app.Environment.IsDevelopment())
    {
        app.MapPost("/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
        {
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var password = form["password"].ToString();

            try
            {
                var client = httpClientFactory.CreateClient("ApiClient");
                var response = await client.PostAsJsonAsync("/api/v1/authentication/login",
                    new { Email = email, Password = password });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthLoginResult>();
                    if (result is not null)
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(result.AccessToken);
                        var claims = jwt.Claims.ToList();

                        // Map role claims
                        var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)
                            ?? claims.FirstOrDefault(c => c.Type == "role");
                        if (roleClaim is not null && roleClaim.Type == "role")
                        {
                            claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
                        }

                        // Store tokens in claims
                        claims.Add(new Claim("access_token", result.AccessToken));
                        claims.Add(new Claim("refresh_token", result.RefreshToken ?? ""));

                        var identity = new ClaimsIdentity(claims, "NumbatWalletAuth");
                        var principal = new ClaimsPrincipal(identity);

                        await context.SignInAsync("NumbatWalletAuth", principal);
                        context.Response.Redirect("/dashboard");
                        return;
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Log.Warning("Login API returned {StatusCode} for {Email}: {Body}",
                        (int)response.StatusCode, email, errorBody);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Login API call failed for {Email}", email);
            }

            context.Response.Redirect($"/login?error={Uri.EscapeDataString("Invalid email or password")}");
        }).AllowAnonymous().DisableAntiforgery();

        app.MapGet("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync("NumbatWalletAuth");
            context.Response.Redirect("/login");
        }).AllowAnonymous().DisableAntiforgery();

        // Test-only endpoint: creates auth cookie directly without API validation
        // Used by Playwright tests to bypass API login issues in development
        app.MapPost("/auth/test-login", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var role = form["role"].ToString();
            if (string.IsNullOrEmpty(role)) role = "Admin";

            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Role, role),
                new("tenant_id", "00000000-0000-0000-0000-000000000001"),
                new("user_id", Guid.NewGuid().ToString())
            };

            if (role == "Admin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Officer"));
                claims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var identity = new ClaimsIdentity(claims, "NumbatWalletAuth");
            var principal = new ClaimsPrincipal(identity);
            await context.SignInAsync("NumbatWalletAuth", principal);
            context.Response.Redirect("/dashboard");
        }).AllowAnonymous().DisableAntiforgery();
    }

    // Database migration is handled by MigrationHelper hosted service
    // No need to manually ensure database creation here

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Polly policies
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} after {Timespan}s", retryCount, timespan.TotalSeconds);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (result, timespan) =>
            {
                Log.Error("Circuit breaker opened for {Timespan}s", timespan.TotalSeconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset");
            });
}

// Make the Program class public for integration tests
public partial class Program { }

// DTO for deserializing API login response
internal sealed record AuthLoginResult(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    DateTime ExpiresAt,
    string TokenType,
    string? UserId,
    string? Email,
    string[]? Roles);
