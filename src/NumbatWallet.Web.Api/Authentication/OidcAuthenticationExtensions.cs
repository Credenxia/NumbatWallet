using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace NumbatWallet.Web.Api.Authentication;

public static class OidcAuthenticationExtensions
{
    public static IServiceCollection AddOidcAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication");
        var useRealAuth = authConfig.GetValue("UseRealAuthentication", false);

        if (useRealAuth)
        {
            // Configure authentication with multiple schemes
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "AzureAd";
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "NumbatWallet.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToLogin = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            // Add Microsoft Identity Web API (JWT Bearer for Azure AD)
            authBuilder.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"), jwtBearerScheme: "AzureAdBearer");

            // Add OpenID Connect for Azure AD (for web apps)
            authBuilder.AddOpenIdConnect("AzureAd", "Azure AD", options =>
            {
                var azureAdConfig = configuration.GetSection("AzureAd");
                options.Authority = azureAdConfig["Authority"] ?? azureAdConfig["Instance"] + "/" + azureAdConfig["TenantId"];
                options.ClientId = azureAdConfig["ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
                options.ClientSecret = azureAdConfig["ClientSecret"] ?? "";
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        await EnrichUserClaims(context, "AzureAd");
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/auth/error?message=" + Uri.EscapeDataString(context.Exception.Message));
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

            // Add ServiceWA authentication as secondary scheme
            var serviceWaConfig = configuration.GetSection("ServiceWA");
            if (serviceWaConfig.Exists())
            {
                authBuilder.AddOpenIdConnect("ServiceWA", "ServiceWA", options =>
                    {
                        options.Authority = serviceWaConfig["Authority"] ?? "https://auth.servicewa.wa.gov.au";
                        options.ClientId = serviceWaConfig["ClientId"] ?? "numbat-wallet";
                        options.ClientSecret = serviceWaConfig["ClientSecret"] ?? "";
                        options.ResponseType = "code";
                        options.SaveTokens = true;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.CallbackPath = "/signin-servicewa";
                        options.SignedOutCallbackPath = "/signout-callback-servicewa";

                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");

                        // Map ServiceWA claims
                        options.ClaimActions.MapJsonKey("waid", "waid");
                        options.ClaimActions.MapJsonKey("verified", "verified");
                        options.ClaimActions.MapJsonKey("credential_level", "credential_level");
                        options.ClaimActions.MapJsonKey("phone_number", "phone_number");
                        options.ClaimActions.MapJsonKey("phone_number_verified", "phone_number_verified");

                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                await EnrichUserClaims(context, "ServiceWA");
                            },
                            OnAuthenticationFailed = context =>
                            {
                                context.Response.Redirect("/auth/error?message=" + Uri.EscapeDataString(context.Exception.Message));
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
            }

            // Add JWT Bearer for API access with multi-issuer support
            authBuilder.AddJwtBearer("Bearer", options =>
                {
                    var azureAdConfig = configuration.GetSection("AzureAd");
                    var serviceWaConfig = configuration.GetSection("ServiceWA");

                    options.Authority = azureAdConfig["Authority"] ?? azureAdConfig["Instance"] + "/" + azureAdConfig["TenantId"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuers = new[]
                        {
                            azureAdConfig["Authority"] ?? azureAdConfig["Instance"] + "/" + azureAdConfig["TenantId"] + "/v2.0",
                            serviceWaConfig["Authority"] ?? "https://auth.servicewa.wa.gov.au"
                        }.Where(i => !string.IsNullOrEmpty(i)),
                        ValidateAudience = true,
                        ValidAudiences = new[]
                        {
                            azureAdConfig["ClientId"] ?? "",
                            serviceWaConfig["ClientId"] ?? ""
                        }.Where(a => !string.IsNullOrEmpty(a)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        RequireExpirationTime = true,
                        RequireSignedTokens = true
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            await EnrichJwtClaims(context);
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError(context.Exception, "JWT authentication failed");
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var response = new
                            {
                                error = "unauthorized",
                                error_description = context.ErrorDescription ?? "Authentication required",
                                timestamp = DateTime.UtcNow
                            };

                            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                        }
                    };
                });
        }
        else
        {
            // Use test authentication for development/POA
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions,
                    TestAuthenticationHandler>("Test", options => { });
        }

        return services;
    }

    public static IServiceCollection AddOidcAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Admin policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin", "SuperAdmin"));

            // Issuer policy
            options.AddPolicy("IssuerOnly", policy =>
                policy.RequireRole("Issuer", "Admin"));

            // Holder policy (any authenticated user)
            options.AddPolicy("Holder", policy =>
                policy.RequireAuthenticatedUser());

            // ServiceWA verified user policy
            options.AddPolicy("VerifiedUser", policy =>
                policy.RequireClaim("verified", "true"));

            // High assurance credential policy
            options.AddPolicy("HighAssurance", policy =>
                policy.RequireClaim("credential_level", "3", "4"));

            // Multi-factor authentication policy
            options.AddPolicy("RequireMFA", policy =>
                policy.RequireClaim("amr", "mfa"));
        });

        return services;
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api") ||
               request.Path.StartsWithSegments("/graphql") ||
               request.Headers["Accept"].ToString().Contains("application/json") ||
               request.Headers["Content-Type"].ToString().Contains("application/json");
    }

    private static async Task EnrichUserClaims(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, string provider)
    {
        var principal = context.Principal;
        if (principal == null)
        {
            return;
        }

        var userId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Fail("User identifier not found in token");
            return;
        }

        // Try to get IUserService if registered
        var userService = context.HttpContext.RequestServices.GetService<IUserService>();
        if (userService != null)
        {
            try
            {
                var user = await userService.GetOrCreateUserAsync(userId, provider, principal.Claims);
                if (user != null)
                {
                    var identity = principal.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Add custom claims
                        identity.AddClaim(new Claim("tenant_id", user.TenantId));
                        identity.AddClaim(new Claim("user_id", user.Id));
                        identity.AddClaim(new Claim("provider", provider));

                        foreach (var role in user.Roles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }

                        foreach (var permission in user.Permissions)
                        {
                            identity.AddClaim(new Claim("permission", permission));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to enrich user claims for {UserId}", userId);
            }
        }
        else
        {
            // Fallback: add basic claims without database lookup
            var identity = principal.Identity as ClaimsIdentity;
            if (identity != null)
            {
                identity.AddClaim(new Claim("provider", provider));
                // Default tenant for development
                if (!identity.HasClaim(c => c.Type == "tenant_id"))
                {
                    identity.AddClaim(new Claim("tenant_id", "default-tenant"));
                }
            }
        }
    }

    private static async Task EnrichJwtClaims(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal == null)
        {
            return;
        }

        var userId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Try to get IUserService if registered
        var userService = context.HttpContext.RequestServices.GetService<IUserService>();
        if (userService != null)
        {
            try
            {
                var provider = principal.FindFirst("iss")?.Value?.Contains("microsoft") == true ? "AzureAd" : "ServiceWA";
                var user = await userService.GetOrCreateUserAsync(userId, provider, principal.Claims);

                if (user != null)
                {
                    var identity = principal.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Add enriched claims
                        if (!identity.HasClaim(c => c.Type == "tenant_id"))
                        {
                            identity.AddClaim(new Claim("tenant_id", user.TenantId));
                        }
                        if (!identity.HasClaim(c => c.Type == "user_id"))
                        {
                            identity.AddClaim(new Claim("user_id", user.Id));
                        }

                        foreach (var role in user.Roles)
                        {
                            if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == role))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, role));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to enrich JWT claims for {UserId}", userId);
            }
        }

        await Task.CompletedTask;
    }
}

// Real WA IdX Service implementation
public class ServiceWAIdXService : IWAIdXService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceWAIdXService> _logger;

    public ServiceWAIdXService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ServiceWAIdXService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WAIdXUserInfo?> GetUserInfoAsync(string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync("/userinfo");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WAIdXUserInfo>(json);
            }

            _logger.LogWarning("Failed to get user info: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from ServiceWA");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var introspectionEndpoint = _configuration["ServiceWA:IntrospectionEndpoint"]
                ?? "/oauth2/introspect";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("token_type_hint", "access_token")
            });

            var response = await _httpClient.PostAsync(introspectionEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TokenIntrospectionResponse>(json);
                return result?.Active ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with ServiceWA");
            return false;
        }
    }

    public async Task<string> ExchangeCodeAsync(string code, string redirectUri)
    {
        try
        {
            var tokenEndpoint = _configuration["ServiceWA:TokenEndpoint"] ?? "/oauth2/token";
            var clientId = _configuration["ServiceWA:ClientId"] ?? throw new InvalidOperationException("ClientId not configured");
            var clientSecret = _configuration["ServiceWA:ClientSecret"] ?? throw new InvalidOperationException("ClientSecret not configured");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
                return tokenResponse?.AccessToken ?? throw new InvalidOperationException("No access token in response");
            }

            throw new InvalidOperationException($"Token exchange failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging code for token");
            throw;
        }
    }

    private class TokenIntrospectionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("active")]
        public bool Active { get; set; }
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

public class WAIdXUserInfo
{
    public string? Sub { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? WaId { get; set; }
    public bool Verified { get; set; }
    public int CredentialLevel { get; set; }
}

public interface IWAIdXService
{
    Task<WAIdXUserInfo?> GetUserInfoAsync(string accessToken);
    Task<bool> ValidateTokenAsync(string token);
    Task<string> ExchangeCodeAsync(string code, string redirectUri);
}