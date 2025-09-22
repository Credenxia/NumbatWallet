using System.Text;
using Microsoft.Extensions.Options;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Web.Api.Middleware;

/// <summary>
/// Middleware for validating request signatures to prevent tampering and replay attacks
/// </summary>
public class RequestSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSignatureMiddleware> _logger;
    private readonly RequestSignatureOptions _options;

    public RequestSignatureMiddleware(
        RequestDelegate next,
        ILogger<RequestSignatureMiddleware> logger,
        IOptions<RequestSignatureOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestSigningService signingService,
        ITenantCertificateRepository certificateRepository,
        IApiKeyService apiKeyService)
    {
        // Skip signature validation for excluded paths
        if (IsPathExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract signature header
        if (!context.Request.Headers.TryGetValue("X-Request-Signature", out var signatureHeader))
        {
            if (_options.RequireSignature)
            {
                _logger.LogWarning("Request signature missing for path: {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Request signature required");
                return;
            }

            await _next(context);
            return;
        }

        // Parse signature
        var signature = signingService.ParseSignatureHeader(signatureHeader.ToString());
        if (signature == null)
        {
            _logger.LogWarning("Invalid signature header format");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid signature format");
            return;
        }

        // Get client certificate or API key
        string? publicKey = null;

        // Try to get public key from client certificate
        if (context.Connection.ClientCertificate != null)
        {
            var thumbprint = context.Connection.ClientCertificate.Thumbprint;
            var tenantCert = await certificateRepository.GetByThumbprintAsync(thumbprint);

            if (tenantCert != null && tenantCert.IsActive && !tenantCert.IsExpired())
            {
                // Extract public key from certificate
                publicKey = tenantCert.CertificateData; // This would need proper X.509 parsing
            }
        }

        // Try to get public key from API key header if no certificate
        if (publicKey == null && context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            publicKey = await apiKeyService.GetPublicKeyAsync(apiKey.ToString());
        }

        if (string.IsNullOrEmpty(publicKey))
        {
            _logger.LogWarning("No valid public key found for signature verification");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authentication required");
            return;
        }

        // Read request body for signature verification
        string? body = null;
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Extract relevant headers for signature
        var headers = new Dictionary<string, string>();
        foreach (var header in _options.SignedHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var value))
            {
                headers[header] = value.ToString();
            }
        }

        // Verify signature
        var isValid = await signingService.VerifyRequestSignatureAsync(
            signature,
            context.Request.Method,
            context.Request.Path + context.Request.QueryString,
            body,
            publicKey);

        if (!isValid)
        {
            _logger.LogWarning("Invalid request signature for path: {Path}", context.Request.Path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid request signature");
            return;
        }

        // Add signature info to context for downstream use
        context.Items["RequestSignature"] = signature;
        context.Items["SignatureVerified"] = true;

        await _next(context);
    }

    private bool IsPathExcluded(PathString path)
    {
        return _options.ExcludedPaths.Any(p => path.StartsWithSegments(p));
    }

}

public class RequestSignatureOptions
{
    /// <summary>
    /// Whether to require signatures for all requests (except excluded paths)
    /// </summary>
    public bool RequireSignature { get; set; } = false;

    /// <summary>
    /// Paths that don't require signature validation
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/.well-known"
    };

    /// <summary>
    /// Headers to include in signature validation
    /// </summary>
    public List<string> SignedHeaders { get; set; } = new()
    {
        "Content-Type",
        "Content-Length",
        "Host"
    };

    /// <summary>
    /// Maximum age of a request signature in seconds
    /// </summary>
    public int MaxSignatureAgeSeconds { get; set; } = 300; // 5 minutes
}