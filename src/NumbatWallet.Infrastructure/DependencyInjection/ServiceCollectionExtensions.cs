using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Application.DomainServices;
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
        services.AddScoped<SearchTokenInterceptor>();

        // Deterministic HMAC search tokens for encrypted PII (email/phone lookup columns).
        services.AddScoped<IHmacSearchTokenService, Services.HmacSearchTokenService>();

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
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<SearchTokenInterceptor>());

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
        services.AddScoped<IPresentationRepository, PresentationRepository>();
        services.AddScoped<IPresentationRequestRepository, PresentationRequestRepository>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ITenantCertificateRepository, TenantCertificateRepository>();
        services.AddScoped<ICertificateAuthorityRepository, CertificateAuthorityRepository>();
        services.AddScoped<ICertificateTrustStoreRepository, CertificateTrustStoreRepository>();
        services.AddScoped<IWalletTemplateRepository, WalletTemplateRepository>();
        services.AddScoped<IIssuanceRepository, IssuanceRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        // Register Domain Services
        services.AddHttpClient<ICertificateValidationService, CertificateValidationService>();
        services.AddScoped<IRevocationRegistryService, RevocationRegistryService>();

        // Register Security Services
        services.AddScoped<IRequestSigningService, RequestSigningService>();
        services.AddScoped<ISessionService, DistributedSessionService>();

        // Register Password Validators
        // Multiple validators registered - LoginCommandHandler uses all that support the email domain.
        // SECURITY: TestPasswordValidator contains hardcoded test accounts and must NEVER run outside
        // Development/Testing — otherwise it is a production authentication backdoor.
        // Use the same environment resolution as ValidateMockServices (IHostEnvironment-aware)
        // so the guard is consistent and not fooled by a stray env var.
        var passwordEnv = ResolveEnvironmentName(services);
        var isDevOrTest = passwordEnv.Equals("Development", StringComparison.OrdinalIgnoreCase)
            || passwordEnv.Equals("Testing", StringComparison.OrdinalIgnoreCase);
        if (isDevOrTest)
        {
            services.AddScoped<IPasswordValidator, Authentication.TestPasswordValidator>();
        }
        services.AddScoped<IPasswordValidator, Authentication.AzureAdPasswordValidator>();
        services.AddScoped<IPasswordValidator, Authentication.ServiceWaPasswordValidator>();

        // Distributed (Redis-backed) token stores — override the in-memory POA implementations
        // so refresh tokens and the logout blacklist survive restarts and work across instances.
        services.AddSingleton<IRefreshTokenStore, Authentication.DistributedRefreshTokenStore>();
        services.AddSingleton<Application.Services.ITokenBlacklistService, Authentication.DistributedTokenBlacklistService>();

        // Access-token signing — explicit and fail-closed (see AccessTokenSignerSelector):
        //   * Development/Testing default to HS256 (HmacAccessTokenSigner registered by AddApplication),
        //     with Jwt:Signer=KeyVaultRsa available as an opt-in.
        //   * Any other environment REQUIRES Jwt:Signer=KeyVaultRsa + a Key Vault URI; startup
        //     throws otherwise so a misconfigured deployment can never silently mint HS256 tokens.
        // JwtBearer validation in Program.cs reads the active signer's keys/algorithm from
        // IAccessTokenSigner, so issuance and validation can never drift apart.
        var signerKind = Authentication.AccessTokenSignerSelector.Select(passwordEnv, configuration);
        if (signerKind == Authentication.AccessTokenSignerKind.KeyVaultRsa)
        {
            // Overrides the HS256 default registered by AddApplication (last registration wins).
            services.AddSingleton<IAccessTokenSigner, Authentication.KeyVaultRsaAccessTokenSigner>();
        }

        // Register Credential Services
        services.AddScoped<Application.Commands.Credentials.Handlers.ICredentialSharingService, CredentialSharingService>();

        // Presentation token service — REAL signed JWTs (no mock in the production path).
        services.AddScoped<IVerificationService, JwtPresentationTokenService>();

        // Register HSM Providers (required by HsmService)
        services.AddSingleton<Services.Providers.SoftwareHsmProvider>();
        services.AddSingleton<Services.Providers.KeyVaultHsmProvider>();
        services.AddSingleton<Services.Providers.ManagedHsmProvider>();

        services.AddSingleton<IHsmService, HsmService>();
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<IJwtSigningService, JwtSigningService>();
        services.AddSingleton<IJsonLdContextService, JsonLdContextService>();

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
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Credentials.GetCredentialsByWalletQuery, IEnumerable<Application.DTOs.CredentialDto>>,
            Application.Handlers.Credentials.GetCredentialsByWalletHandler>();

        // Register Issuance Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Issuances.GetIssuanceByIdQuery, Application.DTOs.IssuanceDto?>,
            Application.Queries.Issuances.Handlers.GetIssuanceByIdHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Issuances.GetIssuancesByStatusQuery, IEnumerable<Application.DTOs.IssuanceDto>>,
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
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Wallets.GetWalletsByPersonQuery, IEnumerable<Application.DTOs.WalletDto>>,
            Application.Queries.Wallets.GetWalletsByPersonQueryHandler>();

        // Register Tenant Query Handlers
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetTenantByIdQuery, Application.DTOs.TenantDto?>,
            Application.Queries.Tenants.GetTenantByIdQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetAllTenantsQuery, IEnumerable<Application.DTOs.TenantDto>>,
            Application.Queries.Tenants.GetAllTenantsQueryHandler>();
        services.AddScoped<Application.CQRS.Interfaces.IQueryHandler<Application.Queries.Tenants.GetTenantStatisticsQuery, Application.DTOs.TenantStatisticsDto>,
            Application.Queries.Tenants.Handlers.GetTenantStatisticsQueryHandler>();

        // Register Tenant Command Handlers
        services.AddScoped<Application.CQRS.Interfaces.ICommandHandler<Application.Commands.Tenants.CreateTenantCommand, Guid>,
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

        // Email: the SMTP sender requires a configured host. In Development/Testing with no
        // Email:SmtpHost configured, register a log-only sender so email-dependent flows
        // (e.g. shareCredential) work without a mail server. Production always gets the real
        // sender — an unconfigured host there should fail loudly, not silently log.
        var smtpConfigured = !string.IsNullOrEmpty(configuration["Email:SmtpHost"]);
        var emailEnvironment = ResolveEnvironmentName(services);
        if (!smtpConfigured && emailEnvironment is "Development" or "Testing")
        {
            services.AddScoped<IEmailService, LoggingEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, EmailService>();
        }

        // Verifiable Credentials Services
        services.AddScoped<Application.Services.IJsonLdService, Application.Services.JsonLdService>();
        services.AddScoped<Application.Services.ICredentialManifestService, Application.Services.CredentialManifestService>();
        // TODO: Add after fixing background jobs
        // services.AddScoped<Application.BackgroundJobs.IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        // TODO: Implement these services
        // services.AddScoped<IProtectionService, ProtectionService>();
        // services.AddScoped<ISearchTokenService, SearchTokenService>();
        // services.AddScoped<ISearchIndexingService, SearchIndexingService>();

        // Caching - use Aspire service discovery.
        // In Development we deliberately use the in-process memory cache: the local Redis
        // (e.g. Aspire's, whose connection string sets ssl=true against a non-TLS container)
        // is unreliable for local dev and would otherwise break the token stores.
        var cacheEnv = ResolveEnvironmentName(services);
        var useRedis = !cacheEnv.Equals("Development", StringComparison.OrdinalIgnoreCase);
        var redisConnectionString = configuration.GetConnectionString("redis")
            ?? configuration.GetConnectionString("Redis");
        if (useRedis && !string.IsNullOrEmpty(redisConnectionString))
        {
            // Add StackExchange.Redis IConnectionMultiplexer
            services.AddSingleton<IConnectionMultiplexer>(_ =>
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

        // Field-level PII encryption at rest (AES-256-GCM). Built eagerly so the key is loaded
        // and the static accessor used by ProtectedFieldConverter (an EF value converter that
        // cannot take scoped DI) is set before the first DbContext model is built.
        var fieldEncryptor = new Crypto.AesGcmFieldEncryptor(
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Crypto.AesGcmFieldEncryptor>.Instance);
        services.AddSingleton<IFieldEncryptor>(fieldEncryptor);
        Data.Converters.ProtectedFieldConverter.FieldEncryptor = fieldEncryptor;

        // Health Check Service
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        // POA: in-memory admin operation services (mock data) backing the Admin GraphQL
        // surface (feature flags, configurations, backups, reports, admin users, key
        // rotation, maintenance). Singletons with no scoped dependencies; replaced with
        // real implementations in the admin-operations epic.
        services.AddSingleton<IFeatureFlagService, InMemoryFeatureFlagService>();
        services.AddSingleton<IConfigurationService, InMemoryConfigurationService>();
        services.AddSingleton<IBackupService, InMemoryBackupService>();
        services.AddSingleton<IReportingService, InMemoryReportingService>();
        services.AddSingleton<IUserManagementService, InMemoryUserManagementService>();
        services.AddSingleton<IKeyManagementService, InMemoryKeyManagementService>();
        services.AddSingleton<IMaintenanceService, InMemoryMaintenanceService>();

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

        // SECURITY: Mock service guards - prevent mock services in production
        ValidateMockServices(services, configuration);

        return services;
    }

    /// <summary>
    /// Validates that mock services are not registered in production environments.
    /// In production, throws if mock is registered and AllowMocks is not explicitly enabled.
    /// In development/testing, logs warnings when mock services are active.
    /// </summary>
    private static void ValidateMockServices(IServiceCollection services, IConfiguration configuration)
    {
        var allowMocks = configuration.GetValue<bool>("Services:AllowMocks");

        // Resolve the environment name from IHostEnvironment (set by WebApplicationFactory in tests)
        // to avoid the raw env var defaulting to "Production" in test runners.
        var environmentName = ResolveEnvironmentName(services);
        var isProduction = environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                          environmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase);

        // Check for known mock service registrations
        var mockTypes = new[]
        {
            typeof(MockKeyVaultService).FullName,
            typeof(MockWAIdXService).FullName,
            typeof(Services.Mocks.MockBlobStorageService).FullName
        };

        var registeredMocks = services
            .Where(s => s.ImplementationType is not null &&
                        mockTypes.Contains(s.ImplementationType.FullName))
            .Select(s => s.ImplementationType!.Name)
            .ToList();

        if (registeredMocks.Count == 0) return;

        if (isProduction && !allowMocks)
        {
            throw new InvalidOperationException(
                $"Mock services detected in {environmentName} environment: {string.Join(", ", registeredMocks)}. " +
                "Configure the required external services or set Services:AllowMocks=true to override (NOT recommended for production).");
        }

        // Development/Testing: log warnings via console
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"[WARNING] Mock services are active in {environmentName}: {string.Join(", ", registeredMocks)}. " +
            "Ensure real services are configured before deploying to production.");
        Console.ResetColor();
    }

    /// <summary>
    /// Resolves the environment name from IHostEnvironment in the service collection,
    /// falling back to the ASPNETCORE_ENVIRONMENT/DOTNET_ENVIRONMENT env vars, then to "Production".
    /// This ensures test runners using WebApplicationFactory get the correct environment.
    /// </summary>
    private static string ResolveEnvironmentName(IServiceCollection services)
    {
        // Try to resolve IHostEnvironment from the service collection.
        // WebApplicationFactory registers it as a singleton instance.
        var hostEnvDescriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostEnvironment));

        if (hostEnvDescriptor is not null)
        {
            // Check ImplementationInstance first (most common for IHostEnvironment)
            if (hostEnvDescriptor.ImplementationInstance is Microsoft.Extensions.Hosting.IHostEnvironment hostEnv)
            {
                return hostEnv.EnvironmentName;
            }

            // If registered via factory, build a temporary provider to resolve it
            if (hostEnvDescriptor.ImplementationFactory is not null ||
                hostEnvDescriptor.ImplementationType is not null)
            {
                try
                {
                    using var tempProvider = services.BuildServiceProvider();
                    var resolved = tempProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                    if (resolved is not null)
                    {
                        return resolved.EnvironmentName;
                    }
                }
                catch
                {
                    // If building a temp provider fails, fall through to env var check
                }
            }
        }

        // Fallback to env vars, then default to Production
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
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
