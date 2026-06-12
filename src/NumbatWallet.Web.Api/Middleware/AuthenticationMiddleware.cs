using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Web.Api.Authentication;
using System.Security.Claims;
using System.Text;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Configuration for authentication middleware supporting Azure AD and ServiceWA
/// </summary>
public static class AuthenticationMiddleware
{
    public static IServiceCollection AddCustomAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ServiceWA OIDC: the Authority/Audience values shipped in appsettings are PLACEHOLDERS —
        // a real integration requires the actual authority, audience and client registration from
        // the ServiceWA identity team. The scheme is therefore registered only when explicitly
        // enabled (`ServiceWA:OidcEnabled=true`), and fails fast at startup if enabled without
        // real configuration. Until then citizens authenticate via POST /api/v1/auth/login
        // (ServiceWaPasswordValidator / TestPasswordValidator) and receive self-issued JWTs.
        var serviceWaOidcEnabled = configuration.GetValue<bool>("ServiceWA:OidcEnabled");
        if (serviceWaOidcEnabled &&
            (string.IsNullOrWhiteSpace(configuration["ServiceWA:Authority"]) ||
             string.IsNullOrWhiteSpace(configuration["ServiceWA:Audience"])))
        {
            throw new InvalidOperationException(
                "ServiceWA OIDC is enabled (ServiceWA:OidcEnabled=true) but ServiceWA:Authority and/or " +
                "ServiceWA:Audience are not configured. Production requires the real ServiceWA " +
                "authority/audience — set them (from Key Vault) or disable ServiceWA:OidcEnabled.");
        }

        // Add authentication services
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer("AzureAD", options =>
        {
            // Configure Azure AD authentication for officers
            configuration.Bind("AzureAd", options);
            options.Authority = $"{configuration["AzureAd:Instance"]}{configuration["AzureAd:TenantId"]}/v2.0";
            options.Audience = configuration["AzureAd:ClientId"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Allow token from query string for SignalR/WebSocket connections
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/graphql") || path.StartsWithSegments("/hubs")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Add custom claims after token validation
                    if (context.Principal != null)
                    {
                        var claimsIdentity = (ClaimsIdentity)context.Principal.Identity!;

                        // Add role claims based on groups
                        var groups = context.Principal.FindAll("groups").ToList();
                        foreach (var group in groups)
                        {
                            var role = MapGroupToRole(group.Value);
                            if (!string.IsNullOrEmpty(role))
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                            }
                        }

                        // Add tenant claim if available
                        var tenantId = context.HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(tenantId))
                        {
                            claimsIdentity.AddClaim(new Claim("tenant_id", tenantId));
                        }
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

        // Registered only when real ServiceWA OIDC config is present (see fail-fast above).
        if (serviceWaOidcEnabled)
        {
            authBuilder.AddJwtBearer("ServiceWA", options =>
            {
            // Configure ServiceWA authentication for citizens
            options.Authority = configuration["ServiceWA:Authority"];
            options.Audience = configuration["ServiceWA:Audience"];
            options.RequireHttpsMetadata = !configuration.GetValue<bool>("ServiceWA:AllowInsecureHttp");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["ServiceWA:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["ServiceWA:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal != null)
                    {
                        var claimsIdentity = (ClaimsIdentity)context.Principal.Identity!;

                        // Add citizen role for ServiceWA users
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Citizen"));

                        // Map ServiceWA claims to standard claims
                        MapServiceWAClaims(claimsIdentity);
                    }

                    return Task.CompletedTask;
                }
            };
            });
        }

        // Credentry SSO federation: register the CredentryJwt bearer scheme (config-gated)
        // so the MultiAuth selector below can route Credentry-issued tokens to it. All other
        // schemes (AzureAD, ServiceWA, Internal) are unchanged — migration-period coexistence.
        var credentryEnabled = configuration.GetValue<bool>("Credentry:Enabled");
        var credentryAuthority = configuration["Credentry:Authority"];
        if (credentryEnabled)
        {
            authBuilder.AddCredentryJwt(configuration);
        }

        authBuilder.AddJwtBearer("Internal", options =>
        {
            // Configure internal JWT for service-to-service authentication
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT secret key not configured"));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        })
        .AddPolicyScheme("MultiAuth", "Multi-Authentication", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // Determine which authentication scheme to use
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                // Credentry federation: issuer-based selection — decode the JWT issuer
                // WITHOUT validating and forward to CredentryJwt when it matches the
                // configured Credentry issuer (full validation happens in that scheme).
                if (credentryEnabled &&
                    Authentication.CredentryTokenInspector.IsCredentryToken(authHeader, credentryAuthority))
                {
                    return Authentication.CredentryAuthenticationDefaults.AuthenticationScheme;
                }

                // Check issuer to determine scheme. Never forward to ServiceWA unless the
                // scheme was actually registered (real OIDC config present) — forwarding to
                // an unregistered scheme throws at request time.
                if (serviceWaOidcEnabled &&
                    authHeader.Contains("servicewa", StringComparison.OrdinalIgnoreCase))
                {
                    return "ServiceWA";
                }

                if (authHeader.Contains("internal", StringComparison.OrdinalIgnoreCase))
                {
                    return "Internal";
                }

                // Default to Azure AD
                return "AzureAD";
            };
        });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("AdminOrOfficer", policy => policy.RequireRole("Admin", "Officer"));
            options.AddPolicy("Issuer", policy => policy.RequireRole("Admin", "Officer", "Issuer"));
            options.AddPolicy("Citizen", policy => policy.RequireRole("Citizen"));

            // Claim-based policies
            options.AddPolicy("CanIssueCredentials", policy =>
            {
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "permissions" && c.Value.Contains("credential:issue")) ||
                    context.User.IsInRole("Admin") ||
                    context.User.IsInRole("Officer"));
            });

            options.AddPolicy("CanRevokeCredentials", policy =>
            {
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "permissions" && c.Value.Contains("credential:revoke")) ||
                    context.User.IsInRole("Admin"));
            });

            options.AddPolicy("CanManageOrganizations", policy =>
            {
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "permissions" && c.Value.Contains("organization:manage")) ||
                    context.User.IsInRole("Admin"));
            });

            // Multi-tenant policy
            options.AddPolicy("TenantAccess", policy =>
            {
                policy.RequireAssertion(context =>
                {
                    var tenantClaim = context.User.FindFirst("tenant_id");
                    var requestTenant = context.Resource as HttpContext;
                    var contextTenant = requestTenant?.Items["TenantId"]?.ToString();

                    return tenantClaim != null &&
                           contextTenant != null &&
                           tenantClaim.Value == contextTenant;
                });
            });

            // Default policy
            options.DefaultPolicy = options.GetPolicy("Citizen")
                ?? throw new InvalidOperationException("Default policy not found");
        });

        return services;
    }

    private static string? MapGroupToRole(string groupId)
    {
        // Map Azure AD group IDs to roles
        return groupId switch
        {
            // These would be actual Azure AD group IDs
            "admin-group-id" => "Admin",
            "officer-group-id" => "Officer",
            "issuer-group-id" => "Issuer",
            _ => null
        };
    }

    private static void MapServiceWAClaims(ClaimsIdentity identity)
    {
        // Map ServiceWA specific claims to standard claims
        var serviceWaId = identity.FindFirst("servicewa_id");
        if (serviceWaId != null)
        {
            identity.AddClaim(new Claim("sub", serviceWaId.Value));
        }

        var email = identity.FindFirst("email_verified");
        if (email != null && email.Value == "true")
        {
            identity.AddClaim(new Claim(ClaimTypes.Email,
                identity.FindFirst("email")?.Value ?? string.Empty));
        }

        var name = identity.FindFirst("preferred_name") ?? identity.FindFirst("name");
        if (name != null)
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, name.Value));
        }
    }
}

/// <summary>
/// Custom authentication handler for API key authentication (for service accounts)
/// </summary>
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if API key authentication is enabled
        if (!_configuration.GetValue<bool>("ApiKey:Enabled"))
        {
            await _next(context);
            return;
        }

        // Skip API key check if already authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            var validApiKeys = _configuration.GetSection("ApiKey:Keys").Get<Dictionary<string, string>>();

            if (validApiKeys != null && validApiKeys.ContainsValue(apiKey!))
            {
                var keyName = validApiKeys.FirstOrDefault(x => x.Value == apiKey).Key;

                // Create claims for API key authentication
                // A service key has a stable subject identity. Both ClaimTypes.NameIdentifier
                // and the OIDC-style "sub" claim are populated so resolvers that read either
                // (e.g. GraphQL mutations gating on "sub") treat the key as an authenticated principal.
                var subject = $"apikey:{keyName}";
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, keyName),
                    new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
                    new Claim("api_key_name", keyName),
                    new Claim(ClaimTypes.NameIdentifier, subject),
                    new Claim("sub", subject)
                };

                // Roles for service/SDK clients are configurable via ApiKey:Roles (CSV).
                var roles = (_configuration["ApiKey:Roles"] ?? "Service")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // A service key acts on behalf of a tenant it declares via X-Tenant-Id. This is
                // trusted only because the key itself is the credential (held by the agency/SDK).
                if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
                    && !string.IsNullOrWhiteSpace(tenantHeader))
                {
                    claims.Add(new Claim("tenant_id", tenantHeader.ToString()));
                }

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                _logger.LogInformation("API key authentication successful for: {KeyName}", keyName);
            }
            else
            {
                var apiKeyString = apiKey.ToString() ?? string.Empty;
                var maskedKey = apiKeyString.Length > 8 ? string.Concat(apiKeyString.AsSpan(0, 8), "...") : apiKeyString;
                _logger.LogWarning("Invalid API key attempted: {ApiKey}", maskedKey);
            }
        }

        await _next(context);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
