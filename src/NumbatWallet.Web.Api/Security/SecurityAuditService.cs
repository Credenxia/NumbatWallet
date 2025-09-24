using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;

namespace NumbatWallet.Web.Api.Security;

/// <summary>
/// Security event types for audit logging
/// </summary>
public enum SecurityEventType
{
    LoginAttempt,
    LoginSuccess,
    LoginFailed,
    LogoutSuccess,
    UnauthorizedAccess,
    ForbiddenAccess,
    SuspiciousActivity,
    DataAccess,
    DataModification,
    DataDeletion,
    PrivilegeEscalation,
    ConfigurationChange,
    ApiKeyUsage,
    TokenRefresh,
    PasswordChange,
    AccountLocked,
    RateLimitExceeded,
    InvalidInput,
    SqlInjectionAttempt,
    XssAttempt,
    CsrfAttempt
}

/// <summary>
/// Security audit event
/// </summary>
public class SecurityAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public string? Details { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
    public bool IsSuccessful { get; set; }
    public string? TenantId { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// Security audit service interface
/// </summary>
public interface ISecurityAuditService
{
    Task LogSecurityEventAsync(SecurityAuditEvent auditEvent);
    Task LogSecurityEventAsync(HttpContext context, SecurityEventType eventType, string details, bool isSuccessful = true);
    Task<IEnumerable<SecurityAuditEvent>> GetRecentEventsAsync(int count = 100);
    Task<IEnumerable<SecurityAuditEvent>> GetEventsByUserAsync(string userId, DateTime? from = null, DateTime? endDate = null);
    Task<IEnumerable<SecurityAuditEvent>> GetSuspiciousEventsAsync(DateTime? from = null);
    Task<bool> HasSuspiciousActivityAsync(string ipAddress, TimeSpan window);
}

/// <summary>
/// Implementation of security audit service
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly ConcurrentQueue<SecurityAuditEvent> _recentEvents = new();
    private readonly int _maxRecentEvents = 1000;

    // In production, this should write to a persistent audit log store
    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public async Task LogSecurityEventAsync(SecurityAuditEvent auditEvent)
    {
        // Add to recent events queue
        _recentEvents.Enqueue(auditEvent);

        // Maintain queue size
        while (_recentEvents.Count > _maxRecentEvents && _recentEvents.TryDequeue(out _))
        {
            // Remove old events
        }

        // Log to structured logging
        _logger.LogInformation(
            "Security Event: {EventType} - User: {UserId} - IP: {IpAddress} - Path: {RequestPath} - Success: {IsSuccessful}",
            auditEvent.EventType,
            auditEvent.UserId ?? "Anonymous",
            auditEvent.IpAddress,
            auditEvent.RequestPath,
            auditEvent.IsSuccessful);

        // In production, write to persistent store (database, event store, etc.)
        // await WriteToAuditLogStoreAsync(auditEvent);

        // Check for patterns that indicate attacks
        await CheckForSecurityPatternsAsync(auditEvent);
    }

    public async Task LogSecurityEventAsync(HttpContext context, SecurityEventType eventType, string details, bool isSuccessful = true)
    {
        var auditEvent = new SecurityAuditEvent
        {
            EventType = eventType,
            UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserName = context.User?.Identity?.Name,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            RequestMethod = context.Request.Method,
            RequestPath = context.Request.GetDisplayUrl(),
            StatusCode = context.Response.StatusCode,
            Details = details,
            IsSuccessful = isSuccessful,
            TenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault(),
            SessionId = context.Session?.Id
        };

        await LogSecurityEventAsync(auditEvent);
    }

    public Task<IEnumerable<SecurityAuditEvent>> GetRecentEventsAsync(int count = 100)
    {
        var events = _recentEvents.TakeLast(count);
        return Task.FromResult(events);
    }

    public Task<IEnumerable<SecurityAuditEvent>> GetEventsByUserAsync(string userId, DateTime? from = null, DateTime? endDate = null)
    {
        var query = _recentEvents.Where(e => e.UserId == userId);

        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= endDate.Value);
        }

        return Task.FromResult(query.AsEnumerable());
    }

    public Task<IEnumerable<SecurityAuditEvent>> GetSuspiciousEventsAsync(DateTime? from = null)
    {
        var suspiciousTypes = new[]
        {
            SecurityEventType.SuspiciousActivity,
            SecurityEventType.SqlInjectionAttempt,
            SecurityEventType.XssAttempt,
            SecurityEventType.CsrfAttempt,
            SecurityEventType.UnauthorizedAccess,
            SecurityEventType.PrivilegeEscalation
        };

        var query = _recentEvents.Where(e => suspiciousTypes.Contains(e.EventType));

        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        return Task.FromResult(query.AsEnumerable());
    }

    public Task<bool> HasSuspiciousActivityAsync(string ipAddress, TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        var suspiciousCount = _recentEvents
            .Where(e => e.IpAddress == ipAddress && e.Timestamp >= cutoff)
            .Count(e => !e.IsSuccessful ||
                        e.EventType == SecurityEventType.LoginFailed ||
                        e.EventType == SecurityEventType.UnauthorizedAccess ||
                        e.EventType == SecurityEventType.RateLimitExceeded);

        // Consider suspicious if more than 5 failed events in the window
        return Task.FromResult(suspiciousCount > 5);
    }

    private async Task CheckForSecurityPatternsAsync(SecurityAuditEvent auditEvent)
    {
        // Check for brute force attempts
        if (auditEvent.EventType == SecurityEventType.LoginFailed && !string.IsNullOrEmpty(auditEvent.IpAddress))
        {
            var recentFailures = _recentEvents
                .Where(e => e.IpAddress == auditEvent.IpAddress &&
                           e.EventType == SecurityEventType.LoginFailed &&
                           e.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                .Count();

            if (recentFailures >= 5)
            {
                _logger.LogWarning("Possible brute force attack detected from IP: {IpAddress}", auditEvent.IpAddress);

                // Log suspicious activity
                await LogSecurityEventAsync(new SecurityAuditEvent
                {
                    EventType = SecurityEventType.SuspiciousActivity,
                    IpAddress = auditEvent.IpAddress,
                    Details = $"Multiple failed login attempts detected ({recentFailures} in 5 minutes)",
                    IsSuccessful = false
                });
            }
        }

        // Check for privilege escalation attempts
        if (auditEvent.EventType == SecurityEventType.UnauthorizedAccess ||
            auditEvent.EventType == SecurityEventType.ForbiddenAccess)
        {
            var recentAttempts = _recentEvents
                .Where(e => e.UserId == auditEvent.UserId &&
                           (e.EventType == SecurityEventType.UnauthorizedAccess ||
                            e.EventType == SecurityEventType.ForbiddenAccess) &&
                           e.Timestamp >= DateTime.UtcNow.AddMinutes(-10))
                .Count();

            if (recentAttempts >= 10)
            {
                _logger.LogWarning("Possible privilege escalation attempt by user: {UserId}", auditEvent.UserId);
            }
        }
    }
}

/// <summary>
/// Security audit middleware
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ISecurityAuditService auditService,
        ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture request details
        var requestPath = context.Request.Path.ToString();
        var method = context.Request.Method;

        try
        {
            await _next(context);

            // Log security-relevant status codes
            if (context.Response.StatusCode == 401)
            {
                await _auditService.LogSecurityEventAsync(
                    context,
                    SecurityEventType.UnauthorizedAccess,
                    $"Unauthorized access attempt to {requestPath}",
                    false);
            }
            else if (context.Response.StatusCode == 403)
            {
                await _auditService.LogSecurityEventAsync(
                    context,
                    SecurityEventType.ForbiddenAccess,
                    $"Forbidden access attempt to {requestPath}",
                    false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security audit middleware");
            throw;
        }
    }
}

/// <summary>
/// Extensions for security audit
/// </summary>
public static class SecurityAuditExtensions
{
    public static IServiceCollection AddSecurityAudit(this IServiceCollection services)
    {
        services.AddSingleton<ISecurityAuditService, SecurityAuditService>();
        return services;
    }

    public static IApplicationBuilder UseSecurityAudit(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityAuditMiddleware>();
        return app;
    }
}