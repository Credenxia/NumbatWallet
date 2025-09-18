using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Infrastructure.Services;

public class TelemetryService : ITelemetryService
{
    private readonly IRedactionService _redactionService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;

    // Patterns for detecting PII in property names and values
    private static readonly Dictionary<string, PiiType> PiiPatterns = new()
    {
        { @"username", PiiType.Email },  // Username often contains email
        { @"email", PiiType.Email },
        { @"e-mail", PiiType.Email },
        { @"phone", PiiType.Phone },
        { @"mobile", PiiType.Phone },
        { @"cell", PiiType.Phone },
        { @"ssn", PiiType.TaxId },
        { @"taxid", PiiType.TaxId },
        { @"tax_id", PiiType.TaxId },
        { @"tfn", PiiType.TaxId },
        { @"name", PiiType.Name },
        { @"firstname", PiiType.Name },
        { @"lastname", PiiType.Name },
        { @"fullname", PiiType.Name },
        { @"ipaddress", PiiType.IpAddress },
        { @"ip_address", PiiType.IpAddress },
        { @"street", PiiType.Address },
        { @"city", PiiType.Address },
        { @"postcode", PiiType.Address },
        { @"zip", PiiType.Address },
        { @"dob", PiiType.DateOfBirth },
        { @"dateofbirth", PiiType.DateOfBirth },
        { @"birthdate", PiiType.DateOfBirth },
        { @"accountnumber", PiiType.AccountNumber },
        { @"account_number", PiiType.AccountNumber },
        { @"card", PiiType.PaymentCard },
        { @"creditcard", PiiType.PaymentCard },
        { @"debitcard", PiiType.PaymentCard },
        { @"bsb", PiiType.AccountNumber },
        { @"routing", PiiType.AccountNumber }
    };

    // Regex patterns for detecting PII values
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled);
    private static readonly Regex IpAddressPattern = new(@"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new(@"\b(19|20)\d\d[-/](0[1-9]|1[012])[-/](0[1-9]|[12][0-9]|3[01])\b", RegexOptions.Compiled);

    public TelemetryService(
        IRedactionService redactionService,
        ICurrentTenantService currentTenantService,
        ICurrentUserService currentUserService,
        TelemetryClient telemetryClient,
        ILogger<TelemetryService> logger)
    {
        _redactionService = redactionService;
        _currentTenantService = currentTenantService;
        _currentUserService = currentUserService;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task LogEventAsync(
        string eventName,
        Dictionary<string, string>? properties = null,
        Dictionary<string, double>? metrics = null)
    {
        var eventTelemetry = new EventTelemetry(eventName);

        // Add tenant and user context
        AddContextProperties(eventTelemetry.Properties);

        // Redact PII from properties
        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                eventTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        // Add metrics
        if (metrics != null)
        {
            foreach (var kvp in metrics)
            {
                eventTelemetry.Metrics[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackEvent(eventTelemetry);
    }

    public async Task LogSecurityEventAsync(
        string eventType,
        string severity,
        object? details = null)
    {
        var eventTelemetry = new EventTelemetry($"Security_{eventType}");

        AddContextProperties(eventTelemetry.Properties);
        eventTelemetry.Properties["Severity"] = severity;
        eventTelemetry.Properties["EventType"] = eventType;

        if (details != null)
        {
            var detailsJson = JsonSerializer.Serialize(details);
            var redactedDetails = await RedactJsonAsync(detailsJson);
            eventTelemetry.Properties["Details"] = redactedDetails;
        }

        _telemetryClient.TrackEvent(eventTelemetry);
    }

    public async Task LogExceptionAsync(
        Exception exception,
        Dictionary<string, string>? properties = null)
    {
        // Redact PII from exception message
        var redactedMessage = await RedactMessageAsync(exception.Message);
        var redactedException = new InvalidOperationException(redactedMessage, exception.InnerException);

        var exceptionTelemetry = new ExceptionTelemetry(redactedException)
        {
            Message = redactedMessage
        };

        AddContextProperties(exceptionTelemetry.Properties);

        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                exceptionTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackException(exceptionTelemetry);
    }

    public async Task LogMetricAsync(
        string metricName,
        double value,
        Dictionary<string, string>? properties = null)
    {
        var metricTelemetry = new MetricTelemetry(metricName, value);

        AddContextProperties(metricTelemetry.Properties);

        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                metricTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackMetric(metricTelemetry);
    }

    public async Task LogDependencyAsync(
        string dependencyType,
        string dependencyName,
        string data,
        DateTimeOffset startTime,
        TimeSpan duration,
        bool success)
    {
        // Redact PII from data
        var redactedData = await RedactMessageAsync(data);

        var dependencyTelemetry = new DependencyTelemetry
        {
            Type = dependencyType,
            Name = dependencyName,
            Data = redactedData,
            Timestamp = startTime,
            Duration = duration,
            Success = success
        };

        AddContextProperties(dependencyTelemetry.Properties);

        _telemetryClient.TrackDependency(dependencyTelemetry);
    }

    public async Task LogPageViewAsync(
        string pageName,
        string url,
        TimeSpan? duration = null,
        Dictionary<string, string>? properties = null)
    {
        // Redact PII from URL
        var redactedUrl = await RedactMessageAsync(url);

        var pageViewTelemetry = new PageViewTelemetry(pageName)
        {
            Url = new Uri(redactedUrl, UriKind.RelativeOrAbsolute),
            Duration = duration ?? TimeSpan.Zero
        };

        AddContextProperties(pageViewTelemetry.Properties);

        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                pageViewTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackPageView(pageViewTelemetry);
    }

    public async Task LogRequestAsync(
        string requestName,
        DateTimeOffset startTime,
        TimeSpan duration,
        string responseCode,
        bool success,
        Dictionary<string, string>? properties = null)
    {
        var requestTelemetry = new RequestTelemetry(
            requestName,
            startTime,
            duration,
            responseCode,
            success);

        AddContextProperties(requestTelemetry.Properties);

        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                requestTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackRequest(requestTelemetry);
    }

    public async Task LogTraceAsync(
        string message,
        TelemetrySeverityLevel severityLevel = TelemetrySeverityLevel.Information,
        Dictionary<string, string>? properties = null)
    {
        // Redact PII from message
        var redactedMessage = await RedactMessageAsync(message);

        // Map custom enum to ApplicationInsights enum
        var aiSeverityLevel = severityLevel switch
        {
            TelemetrySeverityLevel.Verbose => SeverityLevel.Verbose,
            TelemetrySeverityLevel.Warning => SeverityLevel.Warning,
            TelemetrySeverityLevel.Error => SeverityLevel.Error,
            TelemetrySeverityLevel.Critical => SeverityLevel.Critical,
            _ => SeverityLevel.Information
        };

        var traceTelemetry = new TraceTelemetry(redactedMessage, aiSeverityLevel);

        AddContextProperties(traceTelemetry.Properties);

        if (properties != null)
        {
            var redactedProperties = await RedactPropertiesAsync(properties);
            foreach (var kvp in redactedProperties)
            {
                traceTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        _telemetryClient.TrackTrace(traceTelemetry);
    }

    public async Task LogAvailabilityAsync(
        string testName,
        DateTimeOffset startTime,
        TimeSpan duration,
        string runLocation,
        bool success,
        string? message = null)
    {
        var availabilityTelemetry = new AvailabilityTelemetry(
            testName,
            startTime,
            duration,
            runLocation,
            success,
            message);

        AddContextProperties(availabilityTelemetry.Properties);

        _telemetryClient.TrackAvailability(availabilityTelemetry);

        await Task.CompletedTask;
    }

    private void AddContextProperties(IDictionary<string, string> properties)
    {
        if (!string.IsNullOrEmpty(_currentTenantService.TenantId))
        {
            properties["TenantId"] = _currentTenantService.TenantId;
        }

        if (!string.IsNullOrEmpty(_currentUserService.UserId))
        {
            properties["UserId"] = _currentUserService.UserId;
        }

        if (!string.IsNullOrEmpty(_currentUserService.UserEmail))
        {
            properties["UserEmail"] = _currentUserService.UserEmail;
        }
    }

    private async Task<Dictionary<string, string>> RedactPropertiesAsync(
        Dictionary<string, string> properties)
    {
        var redactedProperties = new Dictionary<string, string>();

        foreach (var kvp in properties)
        {
            var piiType = DetectPiiType(kvp.Key, kvp.Value);

            if (piiType != PiiType.None)
            {
                var pattern = GetRedactionPattern(piiType);
                var redactedValue = await _redactionService.RedactAsync(
                    kvp.Value,
                    piiType,
                    pattern);
                redactedProperties[kvp.Key] = redactedValue;
            }
            else
            {
                redactedProperties[kvp.Key] = kvp.Value;
            }
        }

        return redactedProperties;
    }

    private PiiType DetectPiiType(string propertyName, string value)
    {
        // Check property name for known PII patterns
        var lowerPropertyName = propertyName.ToLowerInvariant();
        foreach (var pattern in PiiPatterns)
        {
            if (lowerPropertyName.Contains(pattern.Key))
            {
                return pattern.Value;
            }
        }

        // Check value for PII patterns
        if (EmailPattern.IsMatch(value))
        {
            return PiiType.Email;
        }

        if (PhonePattern.IsMatch(value))
        {
            return PiiType.Phone;
        }

        if (IpAddressPattern.IsMatch(value))
        {
            return PiiType.IpAddress;
        }

        if (DatePattern.IsMatch(value))
        {
            return PiiType.DateOfBirth;
        }

        // Check if value looks like a number that could be sensitive
        if (value.Length >= 8 && value.All(char.IsDigit))
        {
            return PiiType.AccountNumber;
        }

        return PiiType.None;
    }

    private RedactionPattern GetRedactionPattern(PiiType piiType)
    {
        return piiType switch
        {
            PiiType.Email => RedactionPattern.ShowDomain,
            PiiType.Phone => RedactionPattern.ShowLastFour,
            PiiType.Name => RedactionPattern.ShowFirstThree,
            PiiType.IpAddress => RedactionPattern.ShowFirstThree,
            PiiType.AccountNumber => RedactionPattern.ShowLastFour,
            PiiType.DateOfBirth => RedactionPattern.Full,
            PiiType.PaymentCard => RedactionPattern.ShowLastFour,
            PiiType.TaxId => RedactionPattern.ShowLastFour,
            PiiType.Address => RedactionPattern.Full,
            _ => RedactionPattern.Full
        };
    }

    private async Task<string> RedactMessageAsync(string message)
    {
        // Redact email addresses
        if (EmailPattern.IsMatch(message))
        {
            foreach (Match match in EmailPattern.Matches(message))
            {
                var redacted = await _redactionService.RedactAsync(
                    match.Value,
                    PiiType.Email,
                    RedactionPattern.ShowDomain);
                message = message.Replace(match.Value, redacted);
            }
        }

        // Redact IP addresses
        if (IpAddressPattern.IsMatch(message))
        {
            foreach (Match match in IpAddressPattern.Matches(message))
            {
                var redacted = await _redactionService.RedactAsync(
                    match.Value,
                    PiiType.IpAddress,
                    RedactionPattern.ShowFirstThree);
                message = message.Replace(match.Value, redacted);
            }
        }

        return message;
    }

    private async Task<string> RedactJsonAsync(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            await WriteRedactedJsonElement(doc.RootElement, writer);

            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            // If JSON parsing fails, treat as plain text
            return await RedactMessageAsync(json);
        }
    }

    private async Task WriteRedactedJsonElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString() ?? string.Empty;
                        var piiType = DetectPiiType(property.Name, value);
                        if (piiType != PiiType.None)
                        {
                            var pattern = GetRedactionPattern(piiType);
                            var redacted = await _redactionService.RedactAsync(
                                value,
                                piiType,
                                pattern);
                            writer.WriteStringValue(redacted);
                        }
                        else
                        {
                            writer.WriteStringValue(value);
                        }
                    }
                    else
                    {
                        await WriteRedactedJsonElement(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    await WriteRedactedJsonElement(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
