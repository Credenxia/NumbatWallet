using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Data;

public class MigrationHelper : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationHelper> _logger;

    public MigrationHelper(
        IServiceProvider serviceProvider,
        ILogger<MigrationHelper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database migration");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        try
        {
            if (context.Database.IsRelational())
            {
                // Get pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                    await context.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                }
            }
            else
            {
                // For non-relational databases, just ensure created
                await context.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("Database created successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public static class MigrationExtensions
{
    public static IServiceCollection AddDatabaseMigration(this IServiceCollection services)
    {
        services.AddHostedService<MigrationHelper>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    public static async Task<IHost> MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<NumbatWalletDbContext>();
            var logger = services.GetRequiredService<ILogger<MigrationHelper>>();

            logger.LogInformation("Migrating database...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrated successfully");

            // Optionally seed data
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<MigrationHelper>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return host;
    }
}