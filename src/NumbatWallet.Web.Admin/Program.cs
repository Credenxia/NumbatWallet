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
    // REMOVED: Application layer requires repositories which Admin doesn't have
    // Admin should communicate through API only, not execute business logic directly
    // builder.Services.AddApplication();
    // builder.Services.AddInfrastructure(builder.Configuration);

    // Add authentication - use development auth in development mode
    if (builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Authentication:BypassInDevelopment", false))
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
        // Use service discovery to find the API endpoint
        client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("api") ?? "http://api");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Configure GraphQL client for primary data operations (uses service discovery)
    // Note: GraphQL client will be configured after Strawberry Shake code generation

    // Add file API client for file operations only
    builder.Services.AddHttpClient<IFileApiClient, FileApiClient>(client =>
    {
        // Use service discovery to find the API endpoint
        client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("api") ?? "http://api");
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
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .RequireAuthorization();

    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapHub<DashboardHub>("/hubs/dashboard");
    // MapDefaultEndpoints() is commented out to avoid endpoint ambiguity
    // The Admin portal already has its root page handled by Blazor components
    // app.MapDefaultEndpoints();

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
