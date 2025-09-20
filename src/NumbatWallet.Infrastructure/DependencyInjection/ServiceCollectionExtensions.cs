using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Interceptors;
using NumbatWallet.Infrastructure.Repositories;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;
using StackExchange.Redis;

namespace NumbatWallet.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register core services for interceptors
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // Register interceptors
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // Add DbContext
        services.AddDbContext<NumbatWalletDbContext>((serviceProvider, options) =>
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

            // Add interceptors with DI
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>(),
                serviceProvider.GetRequiredService<TenantInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>());

            // Enable sensitive data logging only in development
            if (configuration["EnableSensitiveDataLogging"] == "true")
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Generic Repository
        services.AddScoped(typeof(IRepository<,>), typeof(RepositoryBase<,>));

        // Register Specific Repositories
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();

        // Register Infrastructure Services
        services.AddScoped<Application.Interfaces.ITenantService, TenantService>();
        // Register adapter for SharedKernel.Interfaces.ITenantService
        services.AddScoped<SharedKernel.Interfaces.ITenantService, TenantServiceAdapter>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IWAIdXService, MockWAIdXService>();
        // services.AddScoped<IDateTimeService, DateTimeService>();

        // Protection and Security Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        // TODO: Add after fixing background jobs
        // services.AddScoped<Application.BackgroundJobs.IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        // TODO: Implement these services
        // services.AddScoped<IProtectionService, ProtectionService>();
        // services.AddScoped<ISearchTokenService, SearchTokenService>();
        // services.AddScoped<ISearchIndexingService, SearchIndexingService>();

        // Caching
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Add StackExchange.Redis IConnectionMultiplexer
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(redisConnectionString);
                config.AbortOnConnectFail = false; // Allow retry on initial connection failure
                return ConnectionMultiplexer.Connect(config);
            });

            // Add distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "NumbatWallet";
            });

            // Register Redis-specific cache service
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // Use in-memory cache as fallback
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();
        }

        // Azure Services (if configured)
        var keyVaultUrl = configuration["Azure:KeyVault:Url"];
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            services.AddSingleton<IKeyVaultService, AzureKeyVaultService>();
        }
        else
        {
            // Use mock service for development when Azure Key Vault is not configured
            services.AddSingleton<IKeyVaultService, MockKeyVaultService>();
        }

        // Crypto Services (Envelope Encryption)
        services.AddSingleton<Crypto.Interfaces.IKeyWrapProvider, Crypto.KeyVaultWrapProvider>();
        services.AddScoped<ICryptoService, Crypto.CryptoService>();

        // Blob Storage
        var storageConnectionString = configuration["Azure:Storage:ConnectionString"];
        var storageAccountName = configuration["Azure:Storage:AccountName"];
        if (!string.IsNullOrEmpty(storageConnectionString) || !string.IsNullOrEmpty(storageAccountName))
        {
            services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        }

        // External API Clients
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>("ExternalApi", client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"] ?? "https://api.example.com");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient<IIdentityVerificationApiClient, IdentityVerificationApiClient>("IdentityVerification", client =>
        {
            client.BaseAddress = new Uri(configuration["IdentityVerification:BaseUrl"] ?? "https://identity.wa.gov.au");
            client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for verification
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var apiKey = configuration["IdentityVerification:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        });

        // Database Migration and Seeding
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<MigrationHelper>();

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