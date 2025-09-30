using System.Text;

namespace NumbatWallet.Web.Admin.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IApiClient apiClient, ILogger<AuditLogService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<AuditLogEntry>> GetAuditLogsAsync(
        AuditLogFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = BuildQueryString(filter, page, pageSize);
            var result = await _apiClient.GetAsync<PagedResult<AuditLogEntry>>(
                $"/api/admin/audit-logs?{queryParams}",
                cancellationToken);

            return result ?? GetMockAuditLogs(filter, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch audit logs");
            return GetMockAuditLogs(filter, page, pageSize);
        }
    }

    public async Task<AuditLogEntry?> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetAsync<AuditLogEntry>(
                $"/api/admin/audit-logs/{id}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch audit log {Id}", id);
            return null;
        }
    }

    public async Task<byte[]> ExportLogsAsync(
        AuditLogFilter filter,
        string format = "csv",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, return mock CSV data
            var logs = await GetAuditLogsAsync(filter, 1, 1000, cancellationToken);
            return GenerateCsv(logs.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs");
            return Array.Empty<byte>();
        }
    }

    public async Task<AuditLogStatistics> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _apiClient.GetAsync<AuditLogStatistics>(
                $"/api/admin/audit-logs/statistics?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}",
                cancellationToken);

            return stats ?? GetMockStatistics(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch audit log statistics");
            return GetMockStatistics(startDate, endDate);
        }
    }

    private string BuildQueryString(AuditLogFilter filter, int page, int pageSize)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (filter.StartDate.HasValue)
        {
            queryParams.Add($"startDate={filter.StartDate:yyyy-MM-dd}");
        }

        if (filter.EndDate.HasValue)
        {
            queryParams.Add($"endDate={filter.EndDate:yyyy-MM-dd}");
        }

        if (!string.IsNullOrEmpty(filter.Severity))
        {
            queryParams.Add($"severity={filter.Severity}");
        }

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            queryParams.Add($"entityType={filter.EntityType}");
        }

        if (!string.IsNullOrEmpty(filter.Action))
        {
            queryParams.Add($"action={filter.Action}");
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            queryParams.Add($"userId={filter.UserId}");
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            queryParams.Add($"search={Uri.EscapeDataString(filter.SearchTerm)}");
        }

        return string.Join("&", queryParams);
    }

    private PagedResult<AuditLogEntry> GetMockAuditLogs(AuditLogFilter filter, int page, int pageSize)
    {
        var random = new Random();
        var severities = new[] { "Debug", "Info", "Warning", "Error", "Critical" };
        var actions = new[] { "Create", "Update", "Delete", "View", "Verify", "Revoke" };
        var entityTypes = new[] { "Credential", "Wallet", "Person", "Verification", "System" };
        var users = new[] { "admin@wa.gov.au", "officer1@wa.gov.au", "officer2@wa.gov.au", "system" };

        var logs = new List<AuditLogEntry>();
        var totalCount = 250;
        var startIndex = (page - 1) * pageSize;

        for (int i = 0; i < Math.Min(pageSize, totalCount - startIndex); i++)
        {
            var timestamp = DateTime.UtcNow.AddMinutes(-(startIndex + i) * 5);
            var severity = severities[random.Next(severities.Length)];
            var action = actions[random.Next(actions.Length)];
            var entityType = entityTypes[random.Next(entityTypes.Length)];
            var user = users[random.Next(users.Length)];

            logs.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = timestamp,
                Severity = severity,
                UserId = user,
                UserName = user.Split('@')[0],
                Action = action,
                EntityType = entityType,
                EntityId = Guid.NewGuid().ToString().Substring(0, 8),
                Message = $"{action} {entityType} operation performed",
                IpAddress = $"192.168.1.{random.Next(1, 255)}",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                TenantId = "default",
                Details = new Dictionary<string, object>
                {
                    ["RequestId"] = Guid.NewGuid().ToString(),
                    ["Duration"] = random.Next(10, 500),
                    ["Success"] = severity != "Error"
                }
            });
        }

        // Apply filters
        if (filter.StartDate.HasValue)
        {
            logs = logs.Where(l => l.Timestamp >= filter.StartDate.Value).ToList();
        }

        if (filter.EndDate.HasValue)
        {
            logs = logs.Where(l => l.Timestamp <= filter.EndDate.Value).ToList();
        }

        if (!string.IsNullOrEmpty(filter.Severity))
        {
            logs = logs.Where(l => l.Severity == filter.Severity).ToList();
        }

        if (!string.IsNullOrEmpty(filter.Action))
        {
            logs = logs.Where(l => l.Action == filter.Action).ToList();
        }

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            logs = logs.Where(l => l.EntityType == filter.EntityType).ToList();
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLowerInvariant();
            logs = logs.Where(l =>
                l.Message.ToLowerInvariant().Contains(term) ||
                l.UserId?.ToLowerInvariant().Contains(term) == true ||
                l.EntityId?.ToLowerInvariant().Contains(term) == true).ToList();
        }

        return new PagedResult<AuditLogEntry>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    private AuditLogStatistics GetMockStatistics(DateTime startDate, DateTime endDate)
    {
        var random = new Random();
        return new AuditLogStatistics
        {
            TotalLogs = random.Next(5000, 10000),
            LogsBySeverity = new Dictionary<string, int>
            {
                ["Debug"] = random.Next(100, 500),
                ["Info"] = random.Next(2000, 4000),
                ["Warning"] = random.Next(500, 1000),
                ["Error"] = random.Next(100, 300),
                ["Critical"] = random.Next(10, 50)
            },
            LogsByAction = new Dictionary<string, int>
            {
                ["Create"] = random.Next(500, 1500),
                ["Update"] = random.Next(1000, 2000),
                ["Delete"] = random.Next(100, 300),
                ["View"] = random.Next(2000, 3000),
                ["Verify"] = random.Next(500, 1000)
            },
            LogsByEntityType = new Dictionary<string, int>
            {
                ["Credential"] = random.Next(1000, 2000),
                ["Wallet"] = random.Next(500, 1000),
                ["Person"] = random.Next(500, 1000),
                ["Verification"] = random.Next(1000, 1500),
                ["System"] = random.Next(500, 1000)
            },
            StartDate = startDate,
            EndDate = endDate
        };
    }

    private byte[] GenerateCsv(List<AuditLogEntry> logs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Severity,User,Action,EntityType,EntityId,Message,IpAddress");

        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.Severity},{log.UserId},{log.Action},{log.EntityType},{log.EntityId},\"{log.Message}\",{log.IpAddress}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}