namespace NumbatWallet.Application.DTOs;

public class ConfigurationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Environment { get; set; } = "Production";
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class FeatureFlagDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Description { get; set; }
    public DateTime? EnabledSince { get; set; }
    public DateTime? DisabledSince { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}