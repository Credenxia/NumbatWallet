using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// GraphQL-based implementation of IAuditLogService that communicates with the API
/// instead of directly accessing the database
/// </summary>
public class GraphQLAuditLogService : IAuditLogService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<GraphQLAuditLogService> _logger;

    public GraphQLAuditLogService(
        IApiClient apiClient,
        ILogger<GraphQLAuditLogService> logger)
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
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            // For now, use REST API
            var queryParams = BuildQueryString(filter, page, pageSize);
            var result = await _apiClient.GetAsync<PagedResult<AuditLogEntry>>($"/api/admin/audit-logs?{queryParams}", cancellationToken);

            return result ?? new PagedResult<AuditLogEntry>
            {
                Items = new List<AuditLogEntry>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit logs");
            return new PagedResult<AuditLogEntry>
            {
                Items = new List<AuditLogEntry>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize
            };
        }
    }

    public async Task<AuditLogEntry?> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            var result = await _apiClient.GetAsync<AuditLogEntry>($"/api/admin/audit-logs/{id}", cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit log {Id}", id);
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
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            var queryParams = BuildQueryString(filter, 1, 1000);
            var result = await _apiClient.GetAsync<byte[]>($"/api/admin/audit-logs/export?format={format}&{queryParams}", cancellationToken);

            return result ?? Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
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
            // TODO: Replace with GraphQL query once Strawberry Shake is configured
            var result = await _apiClient.GetAsync<AuditLogStatistics>(
                $"/api/admin/audit-logs/statistics?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}",
                cancellationToken);

            return result ?? new AuditLogStatistics
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audit statistics");
            return new AuditLogStatistics
            {
                StartDate = startDate,
                EndDate = endDate
            };
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
}