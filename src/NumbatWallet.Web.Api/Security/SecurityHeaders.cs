using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;
using System.Threading.RateLimiting;

namespace NumbatWallet.Web.Api.Security;

/// <summary>
/// Security headers middleware for NumbatWallet API
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        SecurityHeadersOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Process the request
        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Content Security Policy
        if (_options.EnableContentSecurityPolicy)
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // X-Content-Type-Options
        headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options
        headers["X-Frame-Options"] = _options.XFrameOptions;

        // X-XSS-Protection
        headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy
        headers["Referrer-Policy"] = _options.ReferrerPolicy;

        // Strict-Transport-Security
        if (_options.EnableStrictTransportSecurity && context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = _options.StrictTransportSecurity;
        }

        // Permissions-Policy
        if (_options.EnablePermissionsPolicy)
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Remove server header
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        // Add custom security headers
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["X-Request-Id"] = context.TraceIdentifier;
    }
}

/// <summary>
/// Options for security headers
/// </summary>
public class SecurityHeadersOptions
{
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public string ContentSecurityPolicy { get; set; } =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://api.numbatwallet.wa.gov.au wss://api.numbatwallet.wa.gov.au; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "upgrade-insecure-requests;";

    public string XFrameOptions { get; set; } = "DENY";
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public bool EnableStrictTransportSecurity { get; set; } = true;
    public string StrictTransportSecurity { get; set; } = "max-age=31536000; includeSubDomains; preload";
    public bool EnablePermissionsPolicy { get; set; } = true;
    public string PermissionsPolicy { get; set; } =
        "accelerometer=(), " +
        "camera=(), " +
        "geolocation=(), " +
        "gyroscope=(), " +
        "magnetometer=(), " +
        "microphone=(), " +
        "payment=(), " +
        "usb=()";
}

/// <summary>
/// Anti-forgery token validation attribute for API endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateAntiForgeryTokenApiAttribute : Attribute, IAsyncActionFilter
{
    private readonly IAntiforgery _antiforgery;

    public ValidateAntiForgeryTokenApiAttribute(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // Skip validation for GET requests
        if (string.Equals(httpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(httpContext);
            await next();
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ObjectResult("Invalid anti-forgery token")
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}

/// <summary>
/// Input sanitization service
/// </summary>
public interface IInputSanitizationService
{
    string SanitizeHtml(string input);
    string SanitizeSql(string input);
    string SanitizeFileName(string fileName);
    bool IsValidEmail(string email);
    bool IsValidUrl(string url);
    bool IsValidPhoneNumber(string phoneNumber);
}

/// <summary>
/// Implementation of input sanitization service
/// </summary>
public class InputSanitizationService : IInputSanitizationService
{
    private readonly ILogger<InputSanitizationService> _logger;
    private static readonly char[] InvalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();

    public InputSanitizationService(ILogger<InputSanitizationService> logger)
    {
        _logger = logger;
    }

    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove HTML tags
        var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"<[^>]+>", string.Empty);

        // Encode special characters
        sanitized = System.Net.WebUtility.HtmlEncode(sanitized);

        return sanitized;
    }

    public string SanitizeSql(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove or escape potentially dangerous SQL characters
        return input
            .Replace("'", "''")
            .Replace(";", string.Empty)
            .Replace("--", string.Empty)
            .Replace("/*", string.Empty)
            .Replace("*/", string.Empty)
            .Replace("xp_", string.Empty)
            .Replace("sp_", string.Empty);
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        // Remove invalid characters
        foreach (var invalidChar in InvalidFileNameChars)
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        // Remove path traversal attempts
        fileName = fileName.Replace("..", string.Empty);
        fileName = fileName.Replace("/", string.Empty);
        fileName = fileName.Replace("\\", string.Empty);

        // Limit length
        if (fileName.Length > 255)
        {
            var extension = System.IO.Path.GetExtension(fileName);
            var nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
            fileName = nameWithoutExtension.AsSpan(0, 255 - extension.Length).ToString() + extension;
        }

        return fileName;
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        // Australian phone number pattern
        var pattern = @"^(?:\+61|0)[2-478](?:[ -]?[0-9]){8}$";
        return System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, pattern);
    }
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public static class SecurityRateLimitingConfiguration
{
    public static IServiceCollection AddSecurityRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var partitionKey = context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // Add specific limiters for different endpoints
            options.AddPolicy("api", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("graphql", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
            };
        });

        return services;
    }
}

/// <summary>
/// Security extensions for the application
/// </summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add input sanitization
        services.AddSingleton<IInputSanitizationService, InputSanitizationService>();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", builder =>
            {
                var allowedOrigins = configuration.GetSection("Security:AllowedOrigins").Get<string[]>()
                    ?? new[] { "https://numbatwallet.wa.gov.au" };

                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromHours(24));
            });
        });

        // Add anti-forgery
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "X-XSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Add data protection
        services.AddDataProtection()
            .SetApplicationName("NumbatWallet")
            .PersistKeysToFileSystem(new DirectoryInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "numbatwallet-keys")));

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);

        app.UseMiddleware<SecurityHeadersMiddleware>(options);
        return app;
    }
}