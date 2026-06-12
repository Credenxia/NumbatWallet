using Carter;
using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Web.Api.Authentication;
using NumbatWallet.Infrastructure.DependencyInjection;
using NumbatWallet.Web.Api.Extensions;
using NumbatWallet.Web.Api.Security;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

// Configure minimal bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NumbatWallet Web API (Minimal Version)");

    var builder = WebApplication.CreateBuilder(args);

    // Configure minimal Serilog
    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console();
    });

    // Add essential services
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // GRAPHQL: Add GraphQL server with schema
    builder.Services.AddGraphQLServer(builder.Configuration);
    Log.Information("GraphQL server configured at /graphql endpoint");

    // PERFORMANCE: Add distributed caching with Redis (fallback to in-memory for development)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString) && !builder.Environment.IsDevelopment())
    {
        try
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "NumbatWallet_";
            });
            Log.Information("Using Redis distributed cache: {ConnectionString}", redisConnectionString);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Redis. Falling back to in-memory cache.");
            builder.Services.AddDistributedMemoryCache();
        }
    }
    else
    {
        // Development: Use in-memory cache
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Using in-memory distributed cache (Development mode)");
    }

    // Add Web API specific services
    builder.Services.AddSingleton<ISecurityAuditService, SecurityAuditService>();

    // Add security services (input sanitization, CORS, anti-forgery, data protection)
    builder.Services.AddSingleton<IInputSanitizationService, InputSanitizationService>();

    // Configure security headers
    builder.Services.Configure<NumbatWallet.Web.Api.Middleware.SecurityHeadersOptions>(options =>
    {
        options.UseHsts = true;
        options.HstsMaxAge = 31536000; // 1 year
        options.XFrameOptions = "DENY";
        options.ReferrerPolicy = "strict-origin-when-cross-origin";
        options.CspDefaultSrc = "'self'";
        options.CspScriptSrc = "'self'";
        options.CspStyleSrc = "'self'";
        options.CspImgSrc = "'self' data: https:";
        options.CspFontSrc = "'self' data:";
        options.CspConnectSrc = "'self'";
        options.AllowInlineScripts = false;
        options.AllowInlineStyles = false;
        options.UpgradeInsecureRequests = true;
        options.BlockAllMixedContent = true;
        options.EnableCors = false; // We handle CORS separately

        // Development vs Production origins
        if (builder.Environment.IsDevelopment())
        {
            options.CspScriptSrc += " 'unsafe-inline' 'unsafe-eval'"; // For Swagger UI
            options.CspStyleSrc += " 'unsafe-inline'"; // For Swagger UI
        }
    });

    // Configure request size limits for Kestrel (10 MB max)
    builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
        options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
        options.Limits.MaxRequestLineSize = 8 * 1024; // 8 KB
    });

    // Add Controllers with JSON configuration
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // Carter minimal-API modules. The duplicate Wallet/Credential/BulkOperation Carter modules
    // were removed in favour of the canonical MVC controllers; PersonEndpoints (no controller
    // equivalent) is the one mapped module. WalletPassEndpoints remains unmapped pending design.
    builder.Services.AddCarter(configurator: c => c.WithModule<NumbatWallet.Web.Api.Endpoints.PersonEndpoints>());

    // Add minimal API versioning (required for WalletGenerationController route constraints)
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Default;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Add production-ready CORS configuration
    builder.Services.AddCors(options =>
    {
        // Production policy - whitelist specific origins
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(
                "https://wallet.numbatwallet.gov.au",
                "https://admin.numbatwallet.gov.au",
                "https://api.numbatwallet.gov.au"
            )
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Total-Count", "X-Page-Number", "X-RateLimit-Limit", "X-RateLimit-Remaining")
            .SetPreflightMaxAge(TimeSpan.FromHours(24));
        });

        // Development policy - localhost only
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:4200",
                "http://localhost:5000"
            )
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Total-Count", "X-Page-Number", "X-RateLimit-Limit", "X-RateLimit-Remaining");
        });
    });

    // AUTHENTICATION: Environment-specific configuration.
    // Credentry SSO federation (Credentry:Enabled=true) adds the "CredentryJwt" bearer scheme
    // and swaps the DEFAULT scheme for a selector that probes the bearer token's issuer:
    // Credentry-issued tokens go to CredentryJwt, everything else keeps flowing to the
    // pre-existing default scheme — self-issued JWTs, the API-key middleware and the
    // (dead-code) Azure AD/ServiceWA schemes are untouched (integrate-first, delete-later).
    var credentryEnabled = builder.Configuration.GetValue<bool>("Credentry:Enabled");
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        // Development/Testing: Use test handler for easier testing
        var devAuthBuilder = builder.Services.AddAuthentication(
                credentryEnabled
                    ? NumbatWallet.Web.Api.Authentication.CredentryAuthenticationDefaults.SelectorScheme
                    : "Test")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, NumbatWallet.Web.Api.Authentication.TestAuthenticationHandler>(
                "Test", options => { });

        if (credentryEnabled)
        {
            devAuthBuilder
                .AddCredentryJwt(builder.Configuration)
                .AddCredentrySelector(builder.Configuration, fallbackScheme: "Test");
            Log.Information(
                "Credentry SSO federation ENABLED (issuer {Authority})",
                builder.Configuration["Credentry:Authority"]);
        }

        Log.Information("Using TEST authentication handler (Development/Testing only)");
    }
    else
    {
        // Production/Staging: validate the self-issued JWTs using the configured access-token
        // signer (HS256 or RS256-from-KeyVault). Configured via the options pattern so the
        // signer (and, for RS256, its Key Vault public key) is resolved from DI.
        var prodAuthBuilder = builder.Services.AddAuthentication(
                credentryEnabled
                    ? NumbatWallet.Web.Api.Authentication.CredentryAuthenticationDefaults.SelectorScheme
                    : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        if (credentryEnabled)
        {
            prodAuthBuilder
                .AddCredentryJwt(builder.Configuration)
                .AddCredentrySelector(
                    builder.Configuration,
                    fallbackScheme: Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
            Log.Information(
                "Credentry SSO federation ENABLED (issuer {Authority})",
                builder.Configuration["Credentry:Authority"]);
        }
        builder.Services.AddOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .Configure<NumbatWallet.Application.Interfaces.IAccessTokenSigner>((options, signer) =>
            {
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signer.GetValidationKeys(),
                    ValidAlgorithms = new[] { signer.Algorithm },
                    ValidateIssuer = true,
                    ValidIssuer = signer.Issuer,
                    ValidateAudience = true,
                    ValidAudience = signer.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            });

        Log.Information("Using JWT Bearer authentication (Production)");
    }

    builder.Services.AddAuthorization(options =>
    {
        // Require authenticated user for endpoints with [Authorize]
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        // Admin-only policy for administrative endpoints
        options.AddPolicy("AdminOnly", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Admin");
        });

        // Super-admin policy for high-risk admin GraphQL mutations
        // (tenant lifecycle, key rotation, restore). Referenced by
        // [Authorize(Policy = "SuperAdmin")] on AdminMutation fields — an
        // unregistered policy would throw at execution time.
        options.AddPolicy("SuperAdmin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("SuperAdmin");
        });

        // Officer policy for service delivery endpoints
        options.AddPolicy("OfficerOnly", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Officer", "Admin");
        });

        // Admin-or-Officer policy (referenced by PersonEndpoints and other minimal-API modules)
        options.AddPolicy("AdminOrOfficer", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Admin", "Officer");
        });

        // Admin policy alias (referenced by name in some endpoints)
        options.AddPolicy("Admin", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Admin");
        });

        // Allow anonymous for endpoints without [Authorize] or with [AllowAnonymous]
        options.FallbackPolicy = null;
    });

    // Add minimal Swagger for testing
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "NumbatWallet API (Minimal)",
            Version = "v1.0"
        });
    });

    // Add global exception handlers (order matters - more specific first)
    builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // PERFORMANCE: Add response caching (client-side HTTP cache headers)
    builder.Services.AddResponseCaching(options =>
    {
        options.MaximumBodySize = 1024 * 1024 * 10; // 10 MB
        options.UseCaseSensitivePaths = true;
        options.SizeLimit = 100 * 1024 * 1024; // 100 MB cache size
    });

    // Add output caching (ASP.NET Core 9 feature)
    builder.Services.AddOutputCache(options =>
    {
        // Default policy - 60 seconds cache.
        // SECURITY: only cache UNAUTHENTICATED requests. Caching per-user/authenticated responses
        // by URL alone could serve one caller's data (or a cached 200) to another caller — the
        // built-in rule skips Authorization-header requests, but API-key (X-API-Key) requests must
        // be excluded too.
        options.AddBasePolicy(builder => builder
            .With(c => !c.HttpContext.Request.Headers.ContainsKey("Authorization")
                       && !c.HttpContext.Request.Headers.ContainsKey("X-API-Key"))
            .Expire(TimeSpan.FromSeconds(60))
            .Tag("default"));

        // Wallet list - 5 minutes cache
        options.AddPolicy("WalletsList", builder => builder
            .Expire(TimeSpan.FromMinutes(5))
            .Tag("wallets")
            .SetVaryByQuery("page", "pageSize", "tenantId"));

        // Credentials list - 10 minutes cache
        options.AddPolicy("CredentialsList", builder => builder
            .Expire(TimeSpan.FromMinutes(10))
            .Tag("credentials")
            .SetVaryByQuery("page", "pageSize", "walletId"));

        // Templates - 1 hour cache (rarely changes)
        options.AddPolicy("Templates", builder => builder
            .Expire(TimeSpan.FromHours(1))
            .Tag("templates")
            .SetVaryByQuery("tenantId"));

        // Health check - 30 seconds cache
        options.AddPolicy("Health", builder => builder
            .Expire(TimeSpan.FromSeconds(30)));

        // No cache for authenticated requests (POA - can be relaxed later)
        options.AddPolicy("NoCache", builder => builder
            .NoCache());
    });

    // SECURITY: Rate Limiting (ASP.NET Core 9)
    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limit: 100 requests per minute per IP
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ipAddress, partition =>
                new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        });

        // Authentication endpoints: Stricter limits (5 attempts per 15 minutes)
        // Development/Testing: Higher limits for local dev and integration tests
        options.AddPolicy("Authentication", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Development/Testing: 100 requests/min for local dev and integration tests
            // Production: 5 requests/15min to prevent brute force
            var (permitLimit, window) = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
                ? (100, TimeSpan.FromMinutes(1))
                : (5, TimeSpan.FromMinutes(15));

            return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(ipAddress, partition =>
                new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    SegmentsPerWindow = 3,
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });

        // API endpoints: Token bucket for burst handling
        options.AddPolicy("Api", context =>
        {
            var userId = context.User.Identity?.Name ?? "anonymous";

            return System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(userId, partition =>
                new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                {
                    TokenLimit = 1000,
                    ReplenishmentPeriod = TimeSpan.FromHours(1),
                    TokensPerPeriod = 500,
                    QueueLimit = 100
                });
        });

        // Rejection response
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
            }

            context.HttpContext.Response.Headers.Append("X-RateLimit-Limit", context.Lease.ToString());
            context.HttpContext.Response.Headers.Append("X-RateLimit-Remaining", "0");

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                Error = "Too many requests",
                Message = "Rate limit exceeded. Please try again later.",
                RetryAfter = retryAfter.TotalSeconds
            }, cancellationToken);
        };
    });

    var app = builder.Build();

    // Configure minimal pipeline with security hardening
    app.UseExceptionHandler(); // Global exception handling with ProblemDetails

    // SECURITY: Force HTTPS redirection (except in development)
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // SECURITY: HTTP Strict Transport Security (HSTS)
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // SECURITY: Add comprehensive security headers
    app.UseMiddleware<NumbatWallet.Web.Api.Middleware.SecurityHeadersMiddleware>();

    // SECURITY: Input sanitization (validate content-type, check for injection attempts)
    app.UseMiddleware<InputSanitizationMiddleware>();

    // SECURITY: Add security audit logging for 401/403 responses
    app.UseMiddleware<SecurityAuditMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "NumbatWallet API v1.0 (Minimal)");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    // SECURITY: Use environment-specific CORS (NO MORE "AllowAll"!)
    app.UseCors(app.Environment.IsProduction() ? "Production" : "Development");

    // PERFORMANCE: Response caching (client-side HTTP cache headers)
    app.UseResponseCaching();

    // SECURITY: Rate Limiting (MUST be after CORS, before authentication)
    app.UseRateLimiter();

    // PERFORMANCE: Output caching (ASP.NET Core 9)
    app.UseOutputCache();

    app.UseAuthentication();

    // SDK: API key authentication for service accounts. MUST run before UseAuthorization so the
    // API-key principal is established before endpoint authorization is evaluated (otherwise
    // [Authorize] endpoints reject the request before the key is ever inspected).
    app.UseApiKeyAuthentication();

    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Map Carter modules (only those registered above via WithModule)
    app.MapCarter();

    // GRAPHQL: Map GraphQL endpoint
    app.MapGraphQL();

    // Add a simple health check endpoint
    app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

    Log.Information("NumbatWallet Web API configured successfully");
    Log.Information("Swagger UI available at: http://localhost:5000");
    Log.Information("GraphQL endpoint available at: http://localhost:5000/graphql");
    Log.Information("GraphQL playground available at: http://localhost:5000/graphql (Development mode)");
    Log.Information("Health check available at: http://localhost:5000/health");
    Log.Information("Wallet generation endpoints available at: http://localhost:5000/api/v1.0/wallet-generation/");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }