using Microsoft.EntityFrameworkCore;
using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.DependencyInjection;
using NumbatWallet.Web.Api.DependencyInjection;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NumbatWallet Web API");

    var builder = WebApplication.CreateBuilder(args);

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
    builder.Services.AddGraphQL(builder.Configuration);

    // Add health checks
    builder.Services.AddInfrastructureHealthChecks(builder.Configuration);
    // TODO: Add HealthChecksUI when package is installed
    // builder.Services.AddHealthChecksUI(setup =>
    // {
    //     setup.SetEvaluationTimeInSeconds(30);
    //     setup.MaximumHistoryEntriesPerEndpoint(50);
    // }).AddInMemoryStorage();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "NumbatWallet API v1");
            c.RoutePrefix = "swagger";
        });
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowedOrigins");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapControllers();
    app.MapGraphQL();
    app.MapHealthChecks("/health");
    // TODO: Add HealthChecksUI endpoint when package is installed
    // app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

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