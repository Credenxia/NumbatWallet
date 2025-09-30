using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace NumbatWallet.Web.Api.Authentication;

public static class OidcAuthenticationExtensions
{
    public static IServiceCollection AddOidcAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication");
        var useRealAuth = authConfig.GetValue<bool>("UseRealAuthentication", false);

        if (useRealAuth)
        {
            // Configure Azure AD / Entra ID authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

            // Add ServiceWA authentication as secondary scheme
            var serviceWaConfig = configuration.GetSection("ServiceWA");
            if (serviceWaConfig.Exists())
            {
                services.AddAuthentication()
                    .AddOpenIdConnect("ServiceWA", options =>
                    {
                        options.Authority = serviceWaConfig["Authority"] ?? "https://auth.servicewa.wa.gov.au";
                        options.ClientId = serviceWaConfig["ClientId"] ?? "numbat-wallet";
                        options.ClientSecret = serviceWaConfig["ClientSecret"] ?? "";
                        options.ResponseType = "code";
                        options.SaveTokens = true;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");

                        // Map ServiceWA claims
                        options.ClaimActions.MapJsonKey("waid", "waid");
                        options.ClaimActions.MapJsonKey("verified", "verified");
                        options.ClaimActions.MapJsonKey("credential_level", "credential_level");

                        options.Events = new OpenIdConnectEvents
                        {
                            OnTokenValidated = async context =>
                            {
                                // Additional validation or user provisioning can be done here
                                await Task.CompletedTask;
                            },
                            OnAuthenticationFailed = context =>
                            {
                                context.HandleResponse();
                                context.Response.StatusCode = 401;
                                return Task.CompletedTask;
                            }
                        };
                    });
            }

            // Configure JWT validation for API access
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
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
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirst("sub")?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            // Log successful authentication
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("User {UserId} authenticated successfully", userId);
                        }
                        await Task.CompletedTask;
                    }
                };
            });
        }
        else
        {
            // Use test authentication for development/POA
            services.AddAuthentication("Test")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                    Testing.TestAuthenticationHandler>("Test", options => { });
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
                return System.Text.Json.JsonSerializer.Deserialize<WAIdXUserInfo>(json);
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
                var result = System.Text.Json.JsonSerializer.Deserialize<TokenIntrospectionResponse>(json);
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
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(json);
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