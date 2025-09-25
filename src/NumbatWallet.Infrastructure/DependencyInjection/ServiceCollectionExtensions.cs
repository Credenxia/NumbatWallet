using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Domain.Services;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Infrastructure.Data.Interceptors;
using NumbatWallet.Infrastructure.Data.Repositories;
using NumbatWallet.Infrastructure.Repositories;
using NumbatWallet.Infrastructure.Security;
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
            // Use Aspire service discovery - connection name matches AddDatabase("numbatwallet") in AppHost
            var connectionString = configuration.GetConnectionString("numbatwallet")
                ?? configuration.GetConnectionString("DefaultConnection");

            // Use SQLite for development if connection string contains "Data Source"
            if (connectionString?.Contains("Data Source") == true)
            {
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.MigrationsAssembly(typeof(NumbatWalletDbContext).Assembly.FullName);
                });
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(NumbatWalletDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            }

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
        services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));

        // Register Specific Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ITenantCertificateRepository, TenantCertificateRepository>();
        services.AddScoped<ICertificateAuthorityRepository, CertificateAuthorityRepository>();
        services.AddScoped<ICertificateTrustStoreRepository, CertificateTrustStoreRepository>();
        services.AddScoped<IWalletTemplateRepository, WalletTemplateRepository>();
        services.AddScoped<IIssuanceRepository, IssuanceRepository>();

        // Register Domain Services
        services.AddHttpClient<ICertificateValidationService, CertificateValidationService>();
        services.AddScoped<IRevocationRegistryService, RevocationRegistryService>();

        // Register Security Services
        services.AddScoped<IRequestSigningService, RequestSigningService>();
        services.AddScoped<ISessionService, DistributedSessionService>();
        services.AddSingleton<IHsmService, HsmService>();
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<Application.Interfaces.IJwtSigningService, JwtSigningService>();
        services.AddSingleton<Application.Interfaces.IJsonLdContextService, JsonLdContextService>();

        // Register Infrastructure Services
        services.AddScoped<Application.Interfaces.ITenantService, TenantService>();
        // Register adapter for SharedKernel.Interfaces.ITenantService
        services.AddScoped<SharedKernel.Interfaces.ITenantService, TenantServiceAdapter>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register WA IdX Service - use real service if configured, otherwise mock
        var serviceWAClientId = configuration["ServiceWA:ClientId"];
        if (!string.IsNullOrEmpty(serviceWAClientId))
        {
            services.AddHttpClient<IWAIdXService, ServiceWAIdXService>("ServiceWAIdX", client =>
            {
                var apiBaseUrl = configuration["ServiceWA:ApiBaseUrl"] ?? "https://api.servicewa.wa.gov.au";
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        }
        else
        {
            services.AddScoped<IWAIdXService, MockWAIdXService>();
        }

        // Register CQRS Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Credentials.IssueCredentialCommand, Application.DTOs.CredentialDto>,
            Application.Commands.Credentials.Handlers.IssueCredentialCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Credentials.VerifyCredentialCommand, Application.DTOs.VerificationResultDto>,
            Application.Commands.Credentials.Handlers.VerifyCredentialCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Credentials.RevokeCredentialCommand, bool>,
            Application.Commands.Credentials.Handlers.RevokeCredentialCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Credentials.ShareCredentialCommand, Application.Commands.Credentials.ShareCredentialResult>,
            Application.Commands.Credentials.Handlers.ShareCredentialCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Credentials.RequestCredentialCommand, Application.Commands.Credentials.CredentialRequestDto>,
            Application.Commands.Credentials.Handlers.RequestCredentialCommandHandler>();

        // Register Batch Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Batch.BatchIssueCredentialsCommand, Application.Commands.Batch.BatchOperationResultDto<Application.DTOs.CredentialDto>>,
            Application.Commands.Batch.Handlers.BatchIssueCredentialsCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Batch.BatchVerifyCredentialsCommand, Application.Commands.Batch.BatchOperationResultDto<Application.DTOs.VerificationResultDto>>,
            Application.Commands.Batch.Handlers.BatchVerifyCredentialsCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Batch.BatchRevokeCredentialsCommand, Application.Commands.Batch.BatchOperationResultDto<bool>>,
            Application.Commands.Batch.Handlers.BatchRevokeCredentialsCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Batch.BatchApproveIssuancesCommand, Application.Commands.Batch.BatchOperationResultDto<Application.DTOs.IssuanceDto>>,
            Application.Commands.Batch.Handlers.BatchApproveIssuancesCommandHandler>();

        // Register Issuance Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Issuances.CreateIssuanceCommand, Application.DTOs.IssuanceDto>,
            Application.Commands.Issuances.Handlers.CreateIssuanceCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Issuances.ApproveIssuanceCommand, Application.DTOs.IssuanceDto>,
            Application.Commands.Issuances.Handlers.ApproveIssuanceCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Issuances.RejectIssuanceCommand, Application.DTOs.IssuanceDto>,
            Application.Commands.Issuances.Handlers.RejectIssuanceCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Issuances.CompleteIssuanceCommand, Application.DTOs.IssuanceDto>,
            Application.Commands.Issuances.Handlers.CompleteIssuanceCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Issuances.CancelIssuanceCommand, bool>,
            Application.Commands.Issuances.Handlers.CancelIssuanceCommandHandler>();

        // Register CQRS Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Wallets.GetWalletByIdQuery, Application.DTOs.WalletDto?>,
            Application.Queries.Wallets.GetWalletByIdQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Credentials.GetCredentialByIdQuery, Application.DTOs.CredentialDto?>,
            Application.Queries.Credentials.GetCredentialByIdQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Credentials.GetCredentialsByWalletQuery, System.Collections.Generic.IEnumerable<Application.DTOs.CredentialDto>>,
            Application.Handlers.Credentials.GetCredentialsByWalletHandler>();

        // Register Issuance Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Issuances.GetIssuanceByIdQuery, Application.DTOs.IssuanceDto?>,
            Application.Queries.Issuances.Handlers.GetIssuanceByIdHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Issuances.GetIssuancesByStatusQuery, System.Collections.Generic.IEnumerable<Application.DTOs.IssuanceDto>>,
            Application.Queries.Issuances.Handlers.GetIssuancesByStatusHandler>();

        // Register Wallet Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Wallets.Commands.CreateWallet.CreateWalletCommand, Application.DTOs.WalletDto>,
            Application.Wallets.Commands.CreateWallet.CreateWalletCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Wallets.UpdateWalletCommand, Application.DTOs.WalletDto>,
            Application.Commands.Wallets.UpdateWalletCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Wallets.ActivateWalletCommand, Application.DTOs.WalletDto>,
            Application.Commands.Wallets.Handlers.ActivateWalletCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Wallets.DeleteWalletCommand, bool>,
            Application.Commands.Wallets.Handlers.DeleteWalletCommandHandler>();

        // Register Wallet Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Wallets.GetWalletsByPersonQuery, System.Collections.Generic.IEnumerable<Application.DTOs.WalletDto>>,
            Application.Queries.Wallets.GetWalletsByPersonQueryHandler>();

        // Register Tenant Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetTenantByIdQuery, Application.DTOs.TenantDto?>,
            Application.Queries.Tenants.GetTenantByIdQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetAllTenantsQuery, System.Collections.Generic.IEnumerable<Application.DTOs.TenantDto>>,
            Application.Queries.Tenants.GetAllTenantsQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetTenantStatisticsQuery, Application.DTOs.TenantStatisticsDto>,
            Application.Queries.Tenants.Handlers.GetTenantStatisticsQueryHandler>();

        // Register Tenant Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Tenants.CreateTenantCommand, System.Guid>,
            Application.Commands.Tenants.CreateTenantCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Tenants.UpdateTenantCommand>,
            Application.Commands.Tenants.UpdateTenantCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Tenants.DeleteTenantCommand>,
            Application.Commands.Tenants.DeleteTenantCommandHandler>();

        // Register Authentication Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Authentication.LoginCommand, Application.DTOs.AuthenticationResultDto>,
            Application.Commands.Authentication.Handlers.LoginCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Authentication.RefreshTokenCommand, Application.DTOs.AuthenticationResultDto>,
            Application.Commands.Authentication.Handlers.RefreshTokenCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Authentication.LogoutCommand, bool>,
            Application.Commands.Authentication.Handlers.LogoutCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Authentication.ChangePasswordCommand, bool>,
            Application.Commands.Authentication.Handlers.ChangePasswordCommandHandler>();
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Authentication.ResetPasswordCommand, bool>,
            Application.Commands.Authentication.Handlers.ResetPasswordCommandHandler>();

        // Protection and Security Services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();

        // Verifiable Credentials Services
        services.AddScoped<Application.Services.IJsonLdService, Application.Services.JsonLdService>();
        services.AddScoped<Application.Services.ICredentialManifestService, Application.Services.CredentialManifestService>();
        // TODO: Add after fixing background jobs
        // services.AddScoped<Application.BackgroundJobs.IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        // TODO: Implement these services
        // services.AddScoped<IProtectionService, ProtectionService>();
        // services.AddScoped<ISearchTokenService, SearchTokenService>();
        // services.AddScoped<ISearchIndexingService, SearchIndexingService>();

        // Caching - use Aspire service discovery
        var redisConnectionString = configuration.GetConnectionString("redis")
            ?? configuration.GetConnectionString("Redis");
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

        // Health Check Service
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // System Metrics Service
        services.AddScoped<ISystemMetricsService, SystemMetricsService>();

        // Platform-specific Wallet Builders
        services.AddScoped<IAppleWalletBuilder, WalletBuilders.AppleWalletBuilder>();
        services.AddScoped<IGoogleWalletBuilder, WalletBuilders.GoogleWalletBuilder>();
        services.AddScoped<IWebWalletBuilder, WalletBuilders.WebWalletBuilder>();
        services.AddScoped<IPlatformWalletBuilder, WalletBuilders.PlatformWalletBuilder>();

        // Blob Storage
        var storageConnectionString = configuration["Azure:Storage:ConnectionString"];
        var storageAccountName = configuration["Azure:Storage:AccountName"];
        if (!string.IsNullOrEmpty(storageConnectionString) || !string.IsNullOrEmpty(storageAccountName))
        {
            services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        }
        else
        {
            // Use mock service for development when Azure Storage is not configured
            services.AddSingleton<IBlobStorageService, Services.Mocks.MockBlobStorageService>();
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

        // Only add MigrationHelper if not explicitly disabled
        var skipMigration = Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION");
        if (skipMigration != "true")
        {
            services.AddHostedService<MigrationHelper>();
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
