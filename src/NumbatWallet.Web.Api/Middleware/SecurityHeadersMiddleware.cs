using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Comprehensive security headers middleware
/// POA: Implementing OWASP security best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;
    private readonly string _nonce;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _nonce = GenerateNonce();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing request
        AddSecurityHeaders(context);

        // Store nonce for CSP
        context.Items["CSP-Nonce"] = _nonce;

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Remove sensitive headers
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");

        // HSTS - Enforce HTTPS
        if (_options.UseHsts)
        {
            headers.Append("Strict-Transport-Security",
                $"max-age={_options.HstsMaxAge}; includeSubDomains; preload");
        }

        // Content Security Policy
        var csp = BuildContentSecurityPolicy();
        headers.Append("Content-Security-Policy", csp);
        headers.Append("Content-Security-Policy-Report-Only", csp); // For testing

        // XSS Protection (legacy browsers)
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Content Type Options
        headers.Append("X-Content-Type-Options", "nosniff");

        // Frame Options - Prevent clickjacking
        headers.Append("X-Frame-Options", _options.XFrameOptions);

        // Referrer Policy
        headers.Append("Referrer-Policy", _options.ReferrerPolicy);

        // Permissions Policy (formerly Feature Policy)
        var permissionsPolicy = BuildPermissionsPolicy();
        headers.Append("Permissions-Policy", permissionsPolicy);

        // CORS headers (if not already set)
        if (_options.EnableCors && !headers.ContainsKey("Access-Control-Allow-Origin"))
        {
            AddCorsHeaders(context);
        }

        // Cache Control for sensitive endpoints
        if (IsSensitiveEndpoint(context.Request.Path))
        {
            headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
        }

        // Add custom security headers
        foreach (var customHeader in _options.CustomHeaders)
        {
            headers.Append(customHeader.Key, customHeader.Value);
        }

        _logger.LogDebug("Security headers applied to response");
    }

    private string BuildContentSecurityPolicy()
    {
        var policies = new List<string>();

        // Default source
        policies.Add($"default-src {_options.CspDefaultSrc}");

        // Script source with nonce
        var scriptSrc = $"{_options.CspScriptSrc} 'nonce-{_nonce}'";
        if (_options.AllowInlineScripts)
        {
            scriptSrc += " 'unsafe-inline'";
        }
        policies.Add($"script-src {scriptSrc}");

        // Style source
        var styleSrc = _options.CspStyleSrc;
        if (_options.AllowInlineStyles)
        {
            styleSrc += " 'unsafe-inline'";
        }
        policies.Add($"style-src {styleSrc}");

        // Image source
        policies.Add($"img-src {_options.CspImgSrc}");

        // Font source
        policies.Add($"font-src {_options.CspFontSrc}");

        // Connect source (AJAX, WebSocket, EventSource)
        policies.Add($"connect-src {_options.CspConnectSrc}");

        // Media source
        policies.Add($"media-src {_options.CspMediaSrc}");

        // Object source (plugins)
        policies.Add("object-src 'none'");

        // Frame ancestors (who can frame this site)
        policies.Add($"frame-ancestors {_options.CspFrameAncestors}");

        // Form action
        policies.Add($"form-action {_options.CspFormAction}");

        // Base URI
        policies.Add("base-uri 'self'");

        // Upgrade insecure requests
        if (_options.UpgradeInsecureRequests)
        {
            policies.Add("upgrade-insecure-requests");
        }

        // Block all mixed content
        if (_options.BlockAllMixedContent)
        {
            policies.Add("block-all-mixed-content");
        }

        // Report URI
        if (!string.IsNullOrEmpty(_options.CspReportUri))
        {
            policies.Add($"report-uri {_options.CspReportUri}");
            policies.Add($"report-to csp-endpoint");
        }

        return string.Join("; ", policies);
    }

    private string BuildPermissionsPolicy()
    {
        var policies = new List<string>
        {
            "accelerometer=()",
            "ambient-light-sensor=()",
            "autoplay=(self)",
            "battery=()",
            "camera=()",
            "display-capture=()",
            "document-domain=()",
            "encrypted-media=(self)",
            "execution-while-not-rendered=()",
            "execution-while-out-of-viewport=()",
            "fullscreen=(self)",
            "geolocation=()",
            "gyroscope=()",
            "layout-animations=(self)",
            "legacy-image-formats=()",
            "magnetometer=()",
            "microphone=()",
            "midi=()",
            "navigation-override=()",
            "oversized-images=(self)",
            "payment=()",
            "picture-in-picture=()",
            "publickey-credentials-get=()",
            "sync-xhr=()",
            "usb=()",
            "vr=()",
            "wake-lock=()",
            "screen-wake-lock=()",
            "web-share=()",
            "xr-spatial-tracking=()"
        };

        return string.Join(", ", policies);
    }

    private void AddCorsHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        var origin = context.Request.Headers["Origin"].ToString();

        if (IsAllowedOrigin(origin))
        {
            headers.Append("Access-Control-Allow-Origin", origin);
            headers.Append("Access-Control-Allow-Credentials", "true");
            headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            headers.Append("Access-Control-Allow-Headers",
                "Content-Type, Authorization, X-Requested-With, X-API-Key, X-Tenant-Id");
            headers.Append("Access-Control-Max-Age", "86400"); // 24 hours
            headers.Append("Access-Control-Expose-Headers",
                "X-Request-Id, X-RateLimit-Limit, X-RateLimit-Remaining");
        }
    }

    private bool IsAllowedOrigin(string origin)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return false;
        }

        return _options.AllowedOrigins.Any(allowed =>
            allowed == "*" ||
            origin.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
            (allowed.StartsWith("*.", StringComparison.OrdinalIgnoreCase) && origin.EndsWith(allowed.Substring(1), StringComparison.OrdinalIgnoreCase)));
    }

    private bool IsSensitiveEndpoint(PathString path)
    {
        var sensitiveEndpoints = new[]
        {
            "/api/v1/admin",
            "/api/v1/credentials",
            "/api/v1/wallets",
            "/graphql"
        };

        return sensitiveEndpoints.Any(endpoint =>
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateNonce()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>
/// Security headers configuration options
/// </summary>
public class SecurityHeadersOptions
{
    // HSTS
    public bool UseHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 year

    // X-Frame-Options
    public string XFrameOptions { get; set; } = "DENY";

    // Referrer Policy
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    // CSP Sources
    public string CspDefaultSrc { get; set; } = "'self'";
    public string CspScriptSrc { get; set; } = "'self'";
    public string CspStyleSrc { get; set; } = "'self'";
    public string CspImgSrc { get; set; } = "'self' data: https:";
    public string CspFontSrc { get; set; } = "'self' data:";
    public string CspConnectSrc { get; set; } = "'self'";
    public string CspMediaSrc { get; set; } = "'none'";
    public string CspFrameAncestors { get; set; } = "'none'";
    public string CspFormAction { get; set; } = "'self'";
    public string? CspReportUri { get; set; }

    // CSP Options
    public bool AllowInlineScripts { get; set; } = false;
    public bool AllowInlineStyles { get; set; } = false;
    public bool UpgradeInsecureRequests { get; set; } = true;
    public bool BlockAllMixedContent { get; set; } = true;

    // CORS
    public bool EnableCors { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new()
    {
        "https://numbatwallet.gov.au",
        "https://*.numbatwallet.gov.au"
    };

    // Custom Headers
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Extension methods for security headers
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IServiceCollection AddSecurityHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection("SecurityHeaders"));

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseSecurityHeadersForProduction(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsProduction())
        {
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseHsts();
        }

        return app;
    }
}