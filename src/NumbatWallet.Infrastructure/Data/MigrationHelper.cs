using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
        var services = scope.ServiceProvider;

        // In Development, use EnsureCreated to build schema from the model
        // (avoids migration issues with pending model changes and seed data JSON errors)
        if (IsDevelopment())
        {
            try
            {
                var created = await context.Database.EnsureCreatedAsync(cancellationToken);
                if (created)
                {
                    _logger.LogInformation("Database schema created via EnsureCreated (Development mode)");
                }
                else
                {
                    // EnsureCreated is a no-op when the database already exists — but an
                    // orchestrator (e.g. Aspire) may provision an EMPTY database, leaving no tables.
                    // Create the schema explicitly in that case so Development starts cleanly.
                    var creator = context.Database.GetService<IRelationalDatabaseCreator>();
                    if (!await creator.HasTablesAsync(cancellationToken))
                    {
                        await creator.CreateTablesAsync(cancellationToken);
                        _logger.LogInformation("Database existed but was empty; schema created via CreateTables (Development mode)");
                    }
                    else
                    {
                        _logger.LogInformation("Database schema already present (Development mode)");
                    }
                }

                // Seed data
                var seeder = services.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/seeding database in Development mode");
            }
            return;
        }

        // Non-Development (Testing/Staging/Production): REAL EF Core migrations only.
        // MigrateAsync applies all pending migrations and can ALTER an existing database —
        // unlike EnsureCreated/CreateTables, which only work on an empty one. Fail fast on error.
        try
        {
            if (context.Database.IsRelational())
            {
                var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pendingMigrations.Count > 0)
                {
                    _logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                        pendingMigrations.Count, string.Join(", ", pendingMigrations));
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

        // Seed data after successful migration
        try
        {
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            if (!IsDevelopment())
                throw;
        }
    }

    private bool IsDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.IsNullOrEmpty(environment) || environment == "Development";
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
        var logger = services.GetRequiredService<ILogger<MigrationHelper>>();

        try
        {
            var context = services.GetRequiredService<NumbatWalletDbContext>();

            logger.LogInformation("Migrating database...");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(environment) || environment == "Development";

            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully");
            }
            catch (Exception migrationEx) when (isDevelopment && migrationEx.Message.Contains("already exists"))
            {
                logger.LogWarning("Database appears to be partially migrated. Attempting to continue...");

                // In development, try to mark all migrations as applied
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                foreach (var migration in pendingMigrations)
                {
                    try
                    {
                        var sql = $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                                  $"VALUES ('{migration}', '9.0.0') " +
                                  $"ON CONFLICT (\"MigrationId\") DO NOTHING";
                        await context.Database.ExecuteSqlRawAsync(sql);
                        logger.LogInformation("Marked migration {Migration} as applied", migration);
                    }
                    catch
                    {
                        // Ignore errors marking migrations
                    }
                }
            }

            // Optionally seed data
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(environment) || environment == "Development";

            if (isDevelopment)
            {
                logger.LogWarning("Continuing despite migration error in development mode");
                // Don't throw in development to allow the app to start
            }
            else
            {
                throw;
            }
        }

        return host;
    }
}
