namespace NumbatWallet.Web.Admin.Models;

public class HealthCheckResult
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public string? Description { get; set; }
}

public class SystemAlert
{
    public string Severity { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
