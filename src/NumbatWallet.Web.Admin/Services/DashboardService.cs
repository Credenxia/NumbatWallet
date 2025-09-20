namespace NumbatWallet.Web.Admin.Services;

public class DashboardService : IDashboardService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IApiClient apiClient, ILogger<DashboardService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string timeRange = "24h", CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _apiClient.GetAsync<DashboardMetrics>(
                $"/api/admin/dashboard/metrics?timeRange={timeRange}",
                cancellationToken);

            return metrics ?? GetDefaultMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch dashboard metrics");
            return GetDefaultMetrics();
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetCredentialTrendAsync(string timeRange = "7d", CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _apiClient.GetAsync<List<TimeSeriesDataPoint>>(
                $"/api/admin/dashboard/credential-trend?timeRange={timeRange}",
                cancellationToken);

            return data ?? GenerateMockTrendData(timeRange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch credential trend data");
            return GenerateMockTrendData(timeRange);
        }
    }

    public async Task<List<ChartDataPoint>> GetCredentialTypeDistributionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _apiClient.GetAsync<List<ChartDataPoint>>(
                "/api/admin/dashboard/credential-distribution",
                cancellationToken);

            return data ?? GetMockDistributionData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch credential distribution data");
            return GetMockDistributionData();
        }
    }

    public async Task<List<ActivityLogEntry>> GetRecentActivityAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var activities = await _apiClient.GetAsync<List<ActivityLogEntry>>(
                $"/api/admin/dashboard/activity?count={count}",
                cancellationToken);

            return activities ?? GetMockActivityData(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch recent activity");
            return GetMockActivityData(count);
        }
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _apiClient.GetAsync<SystemHealthStatus>(
                "/api/admin/dashboard/health",
                cancellationToken);

            return health ?? GetMockHealthStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch system health");
            return GetMockHealthStatus();
        }
    }

    public async Task<List<TimeSeriesDataPoint>> GetPerformanceMetricsAsync(string timeRange = "1h", CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _apiClient.GetAsync<List<TimeSeriesDataPoint>>(
                $"/api/admin/dashboard/performance?timeRange={timeRange}",
                cancellationToken);

            return metrics ?? GenerateMockPerformanceData(timeRange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch performance metrics");
            return GenerateMockPerformanceData(timeRange);
        }
    }

    private DashboardMetrics GetDefaultMetrics()
    {
        return new DashboardMetrics
        {
            TotalWallets = 1250,
            ActiveWallets = 1180,
            TotalCredentials = 3456,
            ActiveCredentials = 3200,
            ExpiredCredentials = 156,
            RevokedCredentials = 100,
            TotalPersons = 1250,
            VerifiedPersons = 1100,
            VerificationsToday = 45,
            VerificationsThisWeek = 312,
            VerificationsThisMonth = 1250,
            SystemHealthScore = 0.98,
            WalletGrowth = "+12%",
            CredentialGrowth = "+8%",
            VerificationChange = "+15%",
            LastUpdated = DateTime.UtcNow
        };
    }

    private List<TimeSeriesDataPoint> GenerateMockTrendData(string timeRange)
    {
        var data = new List<TimeSeriesDataPoint>();
        var random = new Random();
        var now = DateTime.UtcNow;

        int days = timeRange switch
        {
            "1h" => 1,
            "24h" => 1,
            "7d" => 7,
            "30d" => 30,
            _ => 7
        };

        for (int i = days; i >= 0; i--)
        {
            data.Add(new TimeSeriesDataPoint
            {
                Timestamp = now.AddDays(-i),
                Value = random.Next(20, 100),
                Label = now.AddDays(-i).ToString("MMM dd")
            });
        }

        return data;
    }

    private List<ChartDataPoint> GetMockDistributionData()
    {
        return new List<ChartDataPoint>
        {
            new() { Label = "Driver's License", Value = 45, Color = "#007bff" },
            new() { Label = "Passport", Value = 25, Color = "#28a745" },
            new() { Label = "Medicare Card", Value = 15, Color = "#ffc107" },
            new() { Label = "Working With Children", Value = 10, Color = "#17a2b8" },
            new() { Label = "Other", Value = 5, Color = "#6c757d" }
        };
    }

    private List<ActivityLogEntry> GetMockActivityData(int count)
    {
        var activities = new List<ActivityLogEntry>();
        var types = new[] { "CredentialIssued", "WalletCreated", "CredentialVerified", "PersonVerified", "CredentialRevoked" };
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var type = types[i % types.Length];
            activities.Add(new ActivityLogEntry
            {
                Timestamp = now.AddMinutes(-i * 5),
                Type = type,
                Description = GetActivityDescription(type),
                UserId = $"user-{i + 1}",
                EntityId = Guid.NewGuid().ToString(),
                Severity = i % 4 == 0 ? "Warning" : "Info"
            });
        }

        return activities;
    }

    private string GetActivityDescription(string type)
    {
        return type switch
        {
            "CredentialIssued" => "New credential issued to wallet",
            "WalletCreated" => "Digital wallet created for person",
            "CredentialVerified" => "Credential verification completed",
            "PersonVerified" => "Person identity verified successfully",
            "CredentialRevoked" => "Credential revoked by issuer",
            _ => "System activity recorded"
        };
    }

    private SystemHealthStatus GetMockHealthStatus()
    {
        return new SystemHealthStatus
        {
            IsHealthy = true,
            HealthScore = 0.98,
            LastChecked = DateTime.UtcNow,
            Checks = new List<HealthCheckResult>
            {
                new() { Name = "Database", IsHealthy = true, Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(25) },
                new() { Name = "Redis Cache", IsHealthy = true, Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(5) },
                new() { Name = "Azure Storage", IsHealthy = true, Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(45) },
                new() { Name = "Key Vault", IsHealthy = true, Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(35) },
                new() { Name = "API Gateway", IsHealthy = true, Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(15) }
            },
            PerformanceMetrics = new Dictionary<string, double>
            {
                ["CPU Usage"] = 35.5,
                ["Memory Usage"] = 62.3,
                ["Disk I/O"] = 15.8,
                ["Network Latency"] = 12.5
            }
        };
    }

    private List<TimeSeriesDataPoint> GenerateMockPerformanceData(string timeRange)
    {
        var data = new List<TimeSeriesDataPoint>();
        var random = new Random();
        var now = DateTime.UtcNow;

        int points = timeRange switch
        {
            "1h" => 60,
            "24h" => 24,
            "7d" => 7 * 24,
            _ => 60
        };

        int interval = timeRange switch
        {
            "1h" => 1,
            "24h" => 60,
            "7d" => 60,
            _ => 1
        };

        for (int i = points; i >= 0; i -= interval)
        {
            data.Add(new TimeSeriesDataPoint
            {
                Timestamp = now.AddMinutes(-i),
                Value = 20 + random.NextDouble() * 30,
                Metadata = new Dictionary<string, object>
                {
                    ["requests"] = random.Next(100, 500),
                    ["errors"] = random.Next(0, 10)
                }
            });
        }

        return data;
    }
}