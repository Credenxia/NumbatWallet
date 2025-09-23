using Carter;
using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Infrastructure.DependencyInjection;
using NumbatWallet.Web.Api.DependencyInjection;
using NumbatWallet.Web.Api.Extensions;
using NumbatWallet.Web.Api.Hubs;
using Serilog;
using Asp.Versioning.ApiExplorer;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NumbatWallet Web API");

    var builder = WebApplication.CreateBuilder(args);

    // Add service defaults & Aspire components
    builder.AddServiceDefaults();

    // Logging the connection string for debugging
    var connString = builder.Configuration.GetConnectionString("numbatwallet");
    Log.Information("Connection string from config: {ConnectionString}", connString ?? "Not configured");

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console());

    // Add services to the container using our extension methods
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddWebApi(builder.Configuration);

    // Add API versioning (must be before Swagger)
    builder.Services.AddApiVersioningConfiguration(builder.Configuration);

    // Add GraphQL
    builder.Services.AddGraphQLServer(builder.Configuration);

    // Add Carter for REST endpoints with validation
    builder.Services.AddCarterWithValidation();

    // Add health checks
    builder.Services.AddCustomHealthChecks(builder.Configuration);

    // Add versioned Swagger documentation
    builder.Services.AddVersionedSwagger(builder.Configuration);

    // Add custom authentication
    builder.Services.AddCustomAuthentication(builder.Configuration);

    // Add enhanced rate limiting
    builder.Services.AddEnhancedRateLimiting(builder.Configuration);

    // Add security headers
    builder.Services.AddSecurityHeaders(builder.Configuration);

    // Add SignalR for real-time updates
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

    // Register progress notification service
    builder.Services.AddSingleton<IProgressNotificationService, SignalRProgressNotificationService>();

    var app = builder.Build();

    // Get API version description provider for Swagger
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Enable versioned Swagger
    app.UseVersionedSwagger(apiVersionDescriptionProvider);

    app.UseHttpsRedirection();

    // Add security headers early in the pipeline
    app.UseSecurityHeaders();

    app.UseSerilogRequestLogging();
    app.UseCors("AllowedOrigins");

    // Add custom middleware for tenant resolution
    app.UseTenantResolution();

    // Add security middleware
    app.UseMiddleware<MutualTlsMiddleware>();
    app.UseMiddleware<RequestSignatureMiddleware>();

    // Add API key authentication if enabled
    app.UseApiKeyAuthentication();

    app.UseAuthentication();
    app.UseAuthorization();

    // Add enhanced rate limiting with blocking capabilities
    app.UseEnhancedRateLimiting();

    // Add API versioning middleware
    app.UseMiddleware<ApiVersioningMiddleware>();

    // Map endpoints
    app.MapControllers();
    app.MapCarter(); // Map Carter endpoints first
    app.MapGraphQL();
    app.MapHealthChecks();
    app.MapHub<ProgressHub>("/hubs/progress"); // SignalR hub for progress tracking
    app.MapDefaultEndpoints();

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

// Make the Program class public for integration tests
public partial class Program { }
