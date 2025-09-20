using System;
using System.Collections.Generic;

namespace NumbatWallet.Infrastructure.Telemetry;

public interface ITelemetryRedactionService
{
    string RedactString(string input, RedactionContext context);
    object RedactObject(object input, RedactionContext context);
    Dictionary<string, object> RedactProperties(
        Dictionary<string, object> properties,
        RedactionContext context);
    Exception RedactException(Exception ex, RedactionContext context);
    string GenerateCorrelationId(string originalId);
}

public class RedactionContext
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public RedactionLevel Level { get; set; } = RedactionLevel.Standard;
    public HashSet<string> AllowedFields { get; set; } = new();
    public bool PreserveCounts { get; set; } = true;
    public bool PreserveStructure { get; set; } = true;
}

public enum RedactionLevel
{
    None,        // No redaction (dev only)
    Minimal,     // Redact only highly sensitive data
    Standard,    // Default - redact all PII
    Strict,      // Redact all potentially identifying information
    Complete     // Redact everything except system data
}

public class RedactionPattern
{
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = "[REDACTED]";
    public RedactionLevel MinLevel { get; set; } = RedactionLevel.Standard;
}

public class RedactionStatistics
{
    public int TotalFieldsProcessed { get; set; }
    public int FieldsRedacted { get; set; }
    public Dictionary<string, int> RedactionsByType { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}