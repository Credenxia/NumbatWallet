namespace NumbatWallet.Application.DTOs;

public class MetricsSnapshotDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Dictionary<string, decimal> Metrics { get; set; } = new();
    public List<TimeSeriesDataPoint> TimeSeries { get; set; } = new();
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public string? Label { get; set; }
}