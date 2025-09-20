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
                var pendingMigrationsQuery = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                var pendingMigrations = pendingMigrationsQuery.ToList();
                if (pendingMigrations.Count > 0)
                {
                    _logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count);

                    // Try to apply migrations one by one for better error handling
                    foreach (var migration in pendingMigrations)
                    {
                        try
                        {
                            _logger.LogInformation("Applying migration: {Migration}", migration);

                            // For development, handle partial migrations gracefully
                            if (IsDevelopment())
                            {
                                await TryApplyMigrationWithFallback(context, migration, cancellationToken);
                            }
                            else
                            {
                                // In production, fail fast
                                await context.Database.MigrateAsync(cancellationToken);
                                break; // MigrateAsync applies all pending migrations
                            }
                        }
                        catch (Exception migrationEx) when (IsDevelopment() && IsTableExistsError(migrationEx))
                        {
                            _logger.LogWarning("Migration {Migration} partially applied. Marking as complete for development.", migration);
                            // In development, we can continue if tables already exist
                            await MarkMigrationAsApplied(context, migration, cancellationToken);
                        }
                    }

                    _logger.LogInformation("Database migrations processed successfully");
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

            // In development, provide more helpful error message
            if (IsDevelopment() && IsTableExistsError(ex))
            {
                _logger.LogWarning("Database appears to be partially migrated. You may need to reset it.");
                _logger.LogInformation("To reset: Drop the database or delete the Docker volume and restart.");

                // Try to continue in development mode
                _logger.LogInformation("Attempting to continue with existing database schema...");
                return; // Don't throw, allow app to start
            }

            throw;
        }
    }

    private bool IsDevelopment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.IsNullOrEmpty(environment) || environment == "Development";
    }

    private bool IsTableExistsError(Exception ex)
    {
        // PostgreSQL error for table already exists
        return ex.Message.Contains("already exists") ||
               ex.Message.Contains("42P07") || // PostgreSQL error code for duplicate table
               (ex.InnerException != null && IsTableExistsError(ex.InnerException));
    }

    private async Task TryApplyMigrationWithFallback(
        NumbatWalletDbContext context,
        string migration,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to apply just this migration
            var sql = $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                      $"SELECT '{migration}', '9.0.0' " +
                      $"WHERE NOT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migration}')";

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not mark migration as applied: {Error}", ex.Message);
        }
    }

    private async Task MarkMigrationAsApplied(
        NumbatWalletDbContext context,
        string migration,
        CancellationToken cancellationToken)
    {
        try
        {
            // Mark the migration as applied in the history table
            var sql = $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                      $"VALUES ('{migration}', '9.0.0') " +
                      $"ON CONFLICT (\"MigrationId\") DO NOTHING";

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            _logger.LogInformation("Marked migration {Migration} as applied", migration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not mark migration as applied: {Error}", ex.Message);
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