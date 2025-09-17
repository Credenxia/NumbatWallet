using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Repositories;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<NumbatWalletDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(NumbatWalletDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Add interceptors
            options.AddInterceptors(
                new Data.Interceptors.AuditInterceptor(),
                new Data.Interceptors.ProtectionInterceptor());

            // Enable sensitive data logging only in development
            if (configuration.GetValue<bool>("EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NumbatWalletDbContext>());

        // Register Generic Repository
        services.AddScoped(typeof(IRepository<,>), typeof(RepositoryBase<,>));

        // Register Specific Repositories
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();

        // Register Infrastructure Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Protection and Security Services
        services.AddScoped<IProtectionService, ProtectionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISearchTokenService, SearchTokenService>();
        services.AddScoped<ISearchIndexingService, SearchIndexingService>();

        // Caching
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "NumbatWallet";
        });

        // Azure Services (if configured)
        var useAzureKeyVault = configuration.GetValue<bool>("UseAzureKeyVault");
        if (useAzureKeyVault)
        {
            services.AddSingleton<IKeyVaultService, KeyVaultService>();
        }

        // Blob Storage
        var useBlobStorage = configuration.GetValue<bool>("UseBlobStorage");
        if (useBlobStorage)
        {
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }

        return services;
    }

    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        healthChecksBuilder.AddNpgSql(
            configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            tags: new[] { "db", "sql", "postgresql" });

        // Redis health check
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecksBuilder.AddRedis(
                redisConnection,
                name: "redis",
                tags: new[] { "cache", "redis" });
        }

        return services;
    }
}