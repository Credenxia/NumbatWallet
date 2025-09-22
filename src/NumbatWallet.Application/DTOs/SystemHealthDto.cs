using System;
using System.Collections.Generic;

namespace NumbatWallet.Application.DTOs;

/// <summary>
/// System health status information
/// </summary>
public class SystemHealthDto
{
    public string Status { get; set; } = "Healthy";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealthDto> Components { get; set; } = new();
    public SystemMetrics Metrics { get; set; } = new();
    public List<string> ActiveAlerts { get; set; } = new();
}


/// <summary>
/// System performance metrics
/// </summary>
public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public double MemoryUsagePercent => MemoryTotal > 0 ? (double)MemoryUsed / MemoryTotal * 100 : 0;
    public long DiskUsed { get; set; }
    public long DiskTotal { get; set; }
    public double DiskUsagePercent => DiskTotal > 0 ? (double)DiskUsed / DiskTotal * 100 : 0;
    public int ActiveConnections { get; set; }
    public int RequestsPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public long TotalRequests { get; set; }
    public long FailedRequests { get; set; }
    public double ErrorRate => TotalRequests > 0 ? (double)FailedRequests / TotalRequests * 100 : 0;
}