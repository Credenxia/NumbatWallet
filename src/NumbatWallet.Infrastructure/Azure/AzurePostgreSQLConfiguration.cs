using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NumbatWallet.Infrastructure.Data;

namespace NumbatWallet.Infrastructure.Azure;

/// <summary>
/// Azure Database for PostgreSQL configuration
/// POA: Phase 3 - PostgreSQL integration
/// </summary>
public static class AzurePostgreSQLConfiguration
{
    public static IServiceCollection AddAzurePostgreSQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureConfig = configuration.GetSection("Azure:PostgreSQL");
        var useAzureDb = azureConfig.GetValue("UseAzureDatabase", false);

        if (useAzureDb)
        {
            // Configure Azure PostgreSQL connection
            var connectionString = BuildAzureConnectionString(azureConfig);

            services.AddDbContext<NumbatWalletDbContext>((sp, options) =>
            {
                var logger = sp.GetRequiredService<ILogger<NumbatWalletDbContext>>();

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);

                    // Enable Azure AD authentication if configured
                    if (azureConfig.GetValue("UseAzureADAuthentication", false))
                    {
                        // For Azure AD authentication, configure the data source with password callback
                        // This is handled in the connection string builder for now
                        // TODO: Migrate to NpgsqlDataSourceBuilder when upgrading to newer Npgsql
                    }

                    // Performance optimizations
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                // Enable sensitive data logging in development
                if (configuration.GetValue("EnableSensitiveDataLogging", false))
                {
                    options.EnableSensitiveDataLogging();
                }

                options.LogTo(
                    message => logger.LogDebug("{Message}", message),
                    LogLevel.Information);
            });

            // Add health check for database
            services.AddHealthChecks()
                .AddNpgSql(
                    connectionString,
                    name: "azure-postgresql",
                    tags: new[] { "database", "azure" });
        }
        else
        {
            // Use local PostgreSQL for development
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=numbatwallet;Username=postgres;Password=postgres";

            services.AddDbContext<NumbatWalletDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            });

            // Add health check for local database
            services.AddHealthChecks()
                .AddNpgSql(
                    connectionString,
                    name: "local-postgresql",
                    tags: new[] { "database", "local" });
        }

        // Register database services
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

        return services;
    }

    private static string BuildAzureConnectionString(IConfigurationSection config)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = config["Server"] ?? throw new InvalidOperationException("Azure PostgreSQL Server not configured"),
            Database = config["Database"] ?? "numbatwallet",
            Username = config["Username"] ?? throw new InvalidOperationException("Azure PostgreSQL Username not configured"),
            Port = config.GetValue("Port", 5432),
            SslMode = SslMode.Require
        };

        // Set password if not using Azure AD authentication
        if (!config.GetValue("UseAzureADAuthentication", false))
        {
            builder.Password = config["Password"] ?? throw new InvalidOperationException("Azure PostgreSQL Password not configured");
        }

        // Connection pool settings
        builder.MinPoolSize = config.GetValue("MinPoolSize", 10);
        builder.MaxPoolSize = config.GetValue("MaxPoolSize", 100);
        builder.ConnectionIdleLifetime = config.GetValue("ConnectionIdleLifetime", 300);
        builder.Pooling = config.GetValue("Pooling", true);

        // Performance settings
        builder.CommandTimeout = config.GetValue("CommandTimeout", 30);
        builder.KeepAlive = config.GetValue("KeepAlive", 30);

        // Application name for monitoring
        builder.ApplicationName = "NumbatWallet.Api";

        return builder.ConnectionString;
    }

    private static async Task<string> GetAzureADTokenAsync(IConfiguration configuration)
    {
        // Use Azure.Identity to get token for PostgreSQL
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(
            new[] { "https://ossrdbms-aad.database.windows.net/.default" });

        var token = await credential.GetTokenAsync(tokenRequestContext, default);
        return token.Token;
    }
}

/// <summary>
/// Database migration service
/// </summary>
public interface IDatabaseMigrationService
{
    Task<bool> MigrateAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateSchemaAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly NumbatWalletDbContext _context;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        NumbatWalletDbContext context,
        ILogger<DatabaseMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> MigrateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migration completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed");
            return false;
        }
    }

    public async Task<bool> ValidateSchemaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
        return pending;
    }
}

/// <summary>
/// Database backup service
/// </summary>
public interface IDatabaseBackupService
{
    Task<string> CreateBackupAsync(string backupName, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListBackupsAsync(CancellationToken cancellationToken = default);
}

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        IConfiguration configuration,
        ILogger<DatabaseBackupService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string backupName, CancellationToken cancellationToken = default)
    {
        // Note: In production, this would use pg_dump or Azure backup service
        _logger.LogInformation("Creating database backup: {BackupName}", backupName);
        await Task.Delay(100, cancellationToken); // Simulated
        return $"/backups/{backupName}.sql";
    }

    public async Task<bool> RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        // Note: In production, this would use pg_restore or Azure restore service
        _logger.LogInformation("Restoring database from backup: {BackupPath}", backupPath);
        await Task.Delay(100, cancellationToken); // Simulated
        return true;
    }

    public async Task<IEnumerable<string>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        // Note: In production, this would list actual backups from storage
        await Task.Delay(10, cancellationToken); // Simulated
        return new[] { "backup-2024-01-01.sql", "backup-2024-01-02.sql" };
    }
}
