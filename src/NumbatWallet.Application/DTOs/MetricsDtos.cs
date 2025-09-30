namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Request metrics data
/// </summary>
public class RequestMetricsDto
{
    public string RequestId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public long? RequestBodySize { get; set; }
    public long? ResponseBodySize { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string> CustomTags { get; set; } = new();
}