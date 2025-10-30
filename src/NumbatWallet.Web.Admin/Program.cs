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

    // Add authentication - use development auth ONLY in development mode AND when explicitly enabled
    if (builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Authentication:BypassInDevelopment", false) &&
        !builder.Environment.IsProduction()) // Extra safety: never bypass in production
    {
        // Use development authentication handler
        builder.Services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationHandler.SchemeName, null);

        builder.Services.AddControllersWithViews();
    }
    else
    {
        // Use Azure AD authentication in production
        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();
    }

    builder.Services.AddAuthorization(options =>
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

    // Configure GraphQL client for primary data operations (uses service discovery)
    // Note: GraphQL client will be configured after Strawberry Shake code generation

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

    app.MapStaticAssets();

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
