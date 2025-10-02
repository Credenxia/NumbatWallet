using NumbatWallet.Application.DependencyInjection;
using NumbatWallet.Infrastructure.DependencyInjection;
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

    // Add Web API specific services
    builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();

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

    // Add minimal authentication (for testing - allows anonymous)
    builder.Services.AddAuthentication("Test")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, NumbatWallet.Web.Api.Testing.TestAuthenticationHandler>(
            "Test", options => { });

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

        // Officer policy for service delivery endpoints
        options.AddPolicy("OfficerOnly", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("Officer", "Admin");
        });

        // Allow anonymous for endpoints without [Authorize] or with [AllowAnonymous]
        options.FallbackPolicy = null;
    });

    // Add minimal Swagger for testing
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "NumbatWallet API (Minimal)",
            Version = "v1.0"
        });
    });

    // Add global exception handlers (order matters - more specific first)
    builder.Services.AddExceptionHandler<NumbatWallet.Web.Api.Middleware.ArgumentExceptionHandler>();
    builder.Services.AddExceptionHandler<NumbatWallet.Web.Api.Middleware.NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<NumbatWallet.Web.Api.Middleware.ValidationExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Add output caching (ASP.NET Core 9 feature)
    builder.Services.AddOutputCache(options =>
    {
        // Default policy - 60 seconds cache
        options.AddBasePolicy(builder => builder
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
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
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
        options.AddPolicy("Authentication", context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(ipAddress, partition =>
                new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
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
            context.HttpContext.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests;

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

    // SECURITY: Add security audit logging for 401/403 responses
    app.UseMiddleware<NumbatWallet.Web.Api.Security.SecurityAuditMiddleware>();

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

    // SECURITY: Rate Limiting (MUST be after CORS, before authentication)
    app.UseRateLimiter();

    // PERFORMANCE: Output caching (ASP.NET Core 9)
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Add a simple health check endpoint
    app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

    Log.Information("NumbatWallet Web API (Minimal) configured successfully");
    Log.Information("Swagger UI available at: http://localhost:5000");
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

// Minimal test authentication handler that allows all requests
namespace NumbatWallet.Web.Api.Testing
{
    public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public TestAuthenticationHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            System.Security.Claims.ClaimsPrincipal? principal = null;

            // Check for Authorization header with Bearer token
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        // Parse JWT token to extract claims
                        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var key = System.Text.Encoding.UTF8.GetBytes(
                            _configuration["Jwt:SecretKey"] ?? "TestSecretKey123456789012345678901234567890");

                        var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                            ValidateIssuer = false, // Allow any issuer for testing
                            ValidateAudience = false, // Allow any audience for testing
                            ValidateLifetime = true,
                            ClockSkew = System.TimeSpan.FromMinutes(5)
                        };

                        principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                    }
                    catch
                    {
                        // JWT parsing failed - use default claims
                        principal = null;
                    }
                }
            }

            // If no valid JWT token, check if endpoint allows anonymous
            if (principal == null)
            {
                var endpoint = Context.GetEndpoint();
                var allowAnonymous = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null;

                if (allowAnonymous)
                {
                    // For anonymous endpoints, return default test claims
                    var claims = new[]
                    {
                        new System.Security.Claims.Claim("user_id", "test-user"),
                        new System.Security.Claims.Claim("tenant_id", "test-tenant"),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "User"),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user")
                    };

                    var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
                    principal = new System.Security.Claims.ClaimsPrincipal(identity);

                    var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
                    return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
                }
                else
                {
                    // For non-anonymous endpoints without token, return authentication failure (401)
                    return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("No authentication token provided"));
                }
            }

            // Valid JWT token - return success
            var successTicket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(successTicket));
        }
    }
}