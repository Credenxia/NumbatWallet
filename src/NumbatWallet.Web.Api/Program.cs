using Carter;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.DependencyInjection;
using NumbatWallet.Web.Api.DependencyInjection;
using NumbatWallet.Web.Api.Extensions;
using NumbatWallet.Web.Api.Middleware;
using Serilog;

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

    // Add GraphQL
    builder.Services.AddGraphQLServer(builder.Configuration);

    // Add Carter for REST endpoints with validation
    builder.Services.AddCarterWithValidation();

    // Add health checks
    builder.Services.AddCustomHealthChecks(builder.Configuration);

    // Add Swagger documentation
    builder.Services.AddSwaggerDocumentation();

    // Add custom authentication
    builder.Services.AddCustomAuthentication(builder.Configuration);

    var app = builder.Build();

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

    // Enable Swagger for all environments (can be restricted later)
    app.UseSwaggerDocumentation();

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowedOrigins");

    // Add custom middleware for tenant resolution
    app.UseTenantResolution();

    // Add API key authentication if enabled
    app.UseApiKeyAuthentication();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapControllers();
    app.MapGraphQL();
    app.MapCarterEndpoints(); // REST endpoints via Carter with metadata
    app.MapHealthChecks();
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
            await dbContext.Database.MigrateAsync();
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

// Make the Program class public for integration tests
public partial class Program { }