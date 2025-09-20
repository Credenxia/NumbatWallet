using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Infrastructure.DependencyInjection;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Web.Admin.Authentication;
using NumbatWallet.Web.Admin.Components;
using NumbatWallet.Web.Admin.Services;
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
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add Azure AD authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();

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

    // Add Authentication services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CustomAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
        provider.GetRequiredService<CustomAuthenticationStateProvider>());

    // Add Blazored services
    builder.Services.AddBlazoredSessionStorage();

    // Add API client with Polly policies
    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Add application services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();

    // Add health checks
    builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

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
    app.MapDefaultEndpoints();

    // Ensure database is created and migrations are applied
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        if (app.Environment.IsDevelopment())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            dbContext.Database.Migrate();
        }
    }

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
