using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for generating reports and analytics
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generate a report
    /// </summary>
    Task<ReportResult> GenerateReportAsync(ReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a recurring report
    /// </summary>
    Task<string> ScheduleReportAsync(ScheduledReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a scheduled report
    /// </summary>
    Task<bool> CancelScheduledReportAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get report history
    /// </summary>
    Task<List<ReportHistoryItem>> GetReportHistoryAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available report templates
    /// </summary>
    Task<List<ReportTemplate>> GetReportTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export report data
    /// </summary>
    Task<Stream> ExportReportAsync(string reportId, ExportFormat format, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get report by ID
    /// </summary>
    Task<ReportResult?> GetReportAsync(string reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate custom analytics
    /// </summary>
    Task<AnalyticsResult> GenerateAnalyticsAsync(AnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a report (for GraphQL)
    /// </summary>
    Task<ReportDto> GenerateReportAsync(ReportType type, ReportParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get scheduled reports (for GraphQL)
    /// </summary>
    Task<List<ScheduledReportDto>> GetScheduledReportsAsync(CancellationToken cancellationToken = default);
}

public class ReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public ExportFormat Format { get; set; } = ExportFormat.PDF;
    public bool IncludeCharts { get; set; } = true;
}

public class ReportResult
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ScheduledReportRequest
{
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public ExportFormat Format { get; set; } = ExportFormat.PDF;
    public bool Enabled { get; set; } = true;
}

public class ReportHistoryItem
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ReportTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<ReportParameter> Parameters { get; set; } = new();
}

public class ReportParameter
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<object>? AllowedValues { get; set; }
}

public enum ExportFormat
{
    PDF,
    Excel,
    CSV,
    JSON,
    HTML,
    XML
}

public class AnalyticsRequest
{
    public string MetricType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public List<string> Filters { get; set; } = new();
    public bool IncludeTrends { get; set; } = true;
}

public class AnalyticsResult
{
    public string AnalyticsId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<DataPoint> DataPoints { get; set; } = new();
    public Dictionary<string, double> Summary { get; set; } = new();
    public TrendData? Trends { get; set; }
}

public class DataPoint
{
    public DateTime Timestamp { get; set; }
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TrendData
{
    public double GrowthRate { get; set; }
    public string Direction { get; set; } = string.Empty;
    public double Forecast { get; set; }
    public double Confidence { get; set; }
}

// GraphQL DTOs
public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string Format { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ScheduledReportDto
{
    public string Id { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string Schedule { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
}

public enum ReportType
{
    Audit,
    Compliance,
    Usage,
    Performance,
    Security
}

public class ReportParameters
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? IncludeSections { get; set; }
    public string Format { get; set; } = "PDF";
}