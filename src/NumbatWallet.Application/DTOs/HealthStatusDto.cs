namespace NumbatWallet.Application.DTOs;

public class HealthStatusDto
{
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public Dictionary<string, ComponentHealthDto> Components { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}

public class ComponentHealthDto
{
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}
