using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Web.Admin.Services;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Web.Admin.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NumbatWallet.Web.Admin.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "OfficerOrAdmin")]
public class AdminApiController : ControllerBase
{
    private readonly NumbatWalletDbContext _context;
    private readonly IDashboardService _dashboardService;
    private readonly ITenantService _tenantService;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILogger<AdminApiController> _logger;

    public AdminApiController(
        NumbatWalletDbContext context,
        IDashboardService dashboardService,
        ITenantService tenantService,
        IRealtimeNotificationService notificationService,
        ILogger<AdminApiController> logger)
    {
        _context = context;
        _dashboardService = dashboardService;
        _tenantService = tenantService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // Dashboard Endpoints
    [HttpGet("dashboard/metrics")]
    public async Task<IActionResult> GetDashboardMetrics([FromQuery] string? tenantId = null)
    {
        // For now, using the same GetMetricsAsync for both master and tenant views
        // In a real implementation, this would filter by tenant
        var metrics = await _dashboardService.GetMetricsAsync("24h");

        return Ok(metrics);
    }

    [HttpGet("dashboard/chart/wallet-growth")]
    public async Task<IActionResult> GetWalletGrowthChart([FromQuery] string? tenantId = null)
    {
        // Mock wallet growth data for now
        var data = new List<object>
        {
            new { Date = DateTime.UtcNow.AddDays(-7), Count = 100 },
            new { Date = DateTime.UtcNow.AddDays(-6), Count = 120 },
            new { Date = DateTime.UtcNow.AddDays(-5), Count = 135 },
            new { Date = DateTime.UtcNow.AddDays(-4), Count = 145 },
            new { Date = DateTime.UtcNow.AddDays(-3), Count = 160 },
            new { Date = DateTime.UtcNow.AddDays(-2), Count = 175 },
            new { Date = DateTime.UtcNow.AddDays(-1), Count = 190 },
            new { Date = DateTime.UtcNow, Count = 200 }
        };
        return Ok(data);
    }

    [HttpGet("dashboard/chart/credential-distribution")]
    public async Task<IActionResult> GetCredentialDistributionChart([FromQuery] string? tenantId = null)
    {
        // Mock credential distribution data
        var data = await _dashboardService.GetCredentialTypeDistributionAsync();
        return Ok(data);
    }

    [HttpGet("dashboard/activity-feed")]
    public async Task<IActionResult> GetActivityFeed([FromQuery] string? tenantId = null, [FromQuery] int limit = 20)
    {
        var activities = await _dashboardService.GetRecentActivityAsync(limit);
        return Ok(activities);
    }

    [HttpGet("dashboard/system-health")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var health = await _dashboardService.GetSystemHealthAsync();
        return Ok(health);
    }

    // Wallet Management Endpoints
    [HttpGet("wallets")]
    public async Task<IActionResult> GetWallets(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? tenantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Wallets
            .Include(w => w.Credentials)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(w => w.Id.ToString().Contains(search) || w.PersonId.ToString().Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            var walletStatus = Enum.Parse<WalletStatus>(status);
            query = query.Where(w => w.Status == walletStatus);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(w => w.TenantId == tenantId);
        }

        var totalCount = await query.CountAsync();
        var wallets = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                UserId = w.PersonId.ToString(),
                w.TenantId,
                TenantName = "N/A", // Tenant navigation doesn't exist
                CredentialCount = w.Credentials.Count,
                Status = w.Status.ToString(),
                IsActive = w.Status == WalletStatus.Active,
                w.CreatedAt,
                UpdatedAt = w.ModifiedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = wallets
        });
    }

    [HttpGet("wallets/{id}")]
    public async Task<IActionResult> GetWallet(Guid id)
    {
        var wallet = await _context.Wallets
            .Include(w => w.Credentials)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (wallet == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            wallet.Id,
            UserId = wallet.PersonId.ToString(),
            wallet.TenantId,
            TenantName = "N/A", // Tenant navigation doesn't exist
            Credentials = wallet.Credentials.Select(c => new
            {
                c.Id,
                Type = c.CredentialType,
                Status = c.Status.ToString(),
                c.IssuedAt,
                c.ExpiresAt
            }),
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == WalletStatus.Active,
            wallet.CreatedAt,
            UpdatedAt = wallet.ModifiedAt
        });
    }

    [HttpPost("wallets")]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request)
    {
        // Parse UserId as Guid for PersonId
        if (!Guid.TryParse(request.UserId, out var personId))
        {
            return BadRequest("Invalid user ID format");
        }

        var walletResult = Wallet.Create(personId, request.WalletName ?? "Default Wallet");

        if (!walletResult.IsSuccess)
        {
            return BadRequest(walletResult.Error);
        }

        var wallet = walletResult.Value;
        wallet.SetTenantId(request.TenantId);
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.NotifyWalletCreated(request.TenantId, wallet.Id, request.UserId);

        return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, new { wallet.Id });
    }

    [HttpPut("wallets/{id}/suspend")]
    public async Task<IActionResult> SuspendWallet(Guid id, [FromBody] SuspendRequest request)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet == null)
        {
            return NotFound();
        }

        wallet.Suspend(request.Reason ?? "Suspended via admin API");
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("wallets/{id}/reactivate")]
    public async Task<IActionResult> ReactivateWallet(Guid id)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet == null)
        {
            return NotFound();
        }

        wallet.Reactivate();
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("wallets/{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteWallet(Guid id)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet == null)
        {
            return NotFound();
        }

        _context.Wallets.Remove(wallet);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Credential Management Endpoints
    [HttpGet("credentials")]
    public async Task<IActionResult> GetCredentials(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? walletId = null,
        [FromQuery] string? issuerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.Credentials
            .Include(c => c.Wallet)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(c => c.CredentialType == type);
        }

        if (!string.IsNullOrEmpty(status))
        {
            var credStatus = Enum.Parse<CredentialStatus>(status);
            query = query.Where(c => c.Status == credStatus);
        }

        if (!string.IsNullOrEmpty(walletId) && Guid.TryParse(walletId, out var wId))
        {
            query = query.Where(c => c.WalletId == wId);
        }

        if (!string.IsNullOrEmpty(issuerId) && Guid.TryParse(issuerId, out var iId))
        {
            query = query.Where(c => c.IssuerId == iId);
        }

        var totalCount = await query.CountAsync();
        var credentials = await query
            .OrderByDescending(c => c.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Type = c.CredentialType,
                c.WalletId,
                c.IssuerId,
                SchemaVersion = c.SchemaId,
                Status = c.Status.ToString(),
                c.IssuedAt,
                c.ExpiresAt,
                UpdatedAt = c.ModifiedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = credentials
        });
    }

    [HttpGet("credentials/{id}")]
    public async Task<IActionResult> GetCredential(Guid id)
    {
        var credential = await _context.Credentials
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (credential == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            credential.Id,
            Type = credential.CredentialType,
            credential.WalletId,
            credential.IssuerId,
            SchemaVersion = credential.SchemaId,
            Status = credential.Status.ToString(),
            credential.IssuedAt,
            credential.ExpiresAt,
            UpdatedAt = credential.ModifiedAt,
            // Don't return actual credential data for security
            HasData = !string.IsNullOrEmpty(credential.CredentialData)
        });
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> IssueCredential([FromBody] IssueCredentialRequest request)
    {
        var wallet = await _context.Wallets.FindAsync(request.WalletId);
        if (wallet == null)
        {
            return BadRequest("Wallet not found");
        }

        var credentialResult = Credential.Create(
            request.WalletId,
            request.IssuerId,
            request.Type,
            request.Data,
            request.SchemaVersion);

        if (!credentialResult.IsSuccess)
        {
            return BadRequest(credentialResult.Error);
        }

        var credential = credentialResult.Value;

        if (request.ExpiresAt.HasValue)
        {
            credential.SetExpiry(request.ExpiresAt.Value);
        }

        if (request.AutoActivate)
        {
            credential.Activate();
        }

        _context.Credentials.Add(credential);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notificationService.NotifyCredentialIssued(
            wallet.TenantId,
            wallet.Id,
            credential.Id,
            credential.CredentialType);

        return CreatedAtAction(nameof(GetCredential), new { id = credential.Id }, new { credential.Id });
    }

    [HttpPut("credentials/{id}/suspend")]
    public async Task<IActionResult> SuspendCredential(Guid id, [FromBody] SuspendRequest request)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential == null)
        {
            return NotFound();
        }

        credential.Suspend(request.Reason ?? "Suspended via admin API");
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("credentials/{id}/revoke")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RevokeCredential(Guid id, [FromBody] RevokeRequest request)
    {
        var credential = await _context.Credentials.FindAsync(id);
        if (credential == null)
        {
            return NotFound();
        }

        credential.Revoke(request.Reason ?? "Revoked via admin API");
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Tenant Management Endpoints
    [HttpGet("tenants")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _tenantService.GetAllTenantsAsync();
        return Ok(tenants);
    }

    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenant(string id)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPut("tenants/{id}/settings")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateTenantSettings(string id, [FromBody] TenantSettings settings)
    {
        var success = await _tenantService.UpdateTenantSettingsAsync(id, settings);
        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }

    // Statistics Endpoints
    [HttpGet("stats/summary")]
    public async Task<IActionResult> GetStatsSummary([FromQuery] string? tenantId = null)
    {
        var query = _context.Wallets.AsQueryable();
        var credQuery = _context.Credentials.AsQueryable();

        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(w => w.TenantId == tenantId);
            credQuery = credQuery.Where(c => c.Wallet != null && c.Wallet.TenantId == tenantId);
        }

        var stats = new
        {
            TotalWallets = await query.CountAsync(),
            ActiveWallets = await query.CountAsync(w => w.Status == WalletStatus.Active),
            TotalCredentials = await credQuery.CountAsync(),
            ActiveCredentials = await credQuery.CountAsync(c => c.Status == CredentialStatus.Active),
            ExpiredCredentials = await credQuery.CountAsync(c => c.ExpiresAt != null && c.ExpiresAt < DateTimeOffset.UtcNow),
            WalletsCreatedToday = await query.CountAsync(w => w.CreatedAt.Date == DateTime.UtcNow.Date),
            CredentialsIssuedToday = await credQuery.CountAsync(c => c.IssuedAt.Date == DateTime.UtcNow.Date)
        };

        return Ok(stats);
    }

    [HttpPost("export/csv")]
    public async Task<IActionResult> ExportToCsv([FromBody] ExportRequest request)
    {
        byte[] csvData;

        switch (request.Type?.ToLowerInvariant())
        {
            case "wallets":
                csvData = await ExportWalletsToCsv(request.TenantId);
                break;
            case "credentials":
                csvData = await ExportCredentialsToCsv(request.TenantId);
                break;
            default:
                // Return empty CSV for unsupported types
                csvData = System.Text.Encoding.UTF8.GetBytes("Type,Message\nError,Unsupported export type");
                break;
        }

        return File(csvData, "text/csv", $"{request.Type ?? "dashboard"}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    private async Task<byte[]> ExportWalletsToCsv(string? tenantId)
    {
        var query = _context.Wallets.Include(w => w.Credentials).AsQueryable();
        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(w => w.TenantId == tenantId);
        }

        var wallets = await query.ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Wallet ID,Person ID,Tenant ID,Status,Credentials,Created,Last Modified");

        foreach (var wallet in wallets)
        {
            csv.AppendLine($"{wallet.Id},{wallet.PersonId},{wallet.TenantId},{wallet.Status},{wallet.Credentials.Count},{wallet.CreatedAt:yyyy-MM-dd},{wallet.ModifiedAt?.ToString("yyyy-MM-dd") ?? "N/A"}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private async Task<byte[]> ExportCredentialsToCsv(string? tenantId)
    {
        var query = _context.Credentials.Include(c => c.Wallet).AsQueryable();
        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(c => c.Wallet != null && c.Wallet.TenantId == tenantId);
        }

        var credentials = await query.ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Credential ID,Type,Wallet ID,Issuer ID,Status,Issued,Expires");

        foreach (var credential in credentials)
        {
            csv.AppendLine($"{credential.Id},{credential.CredentialType},{credential.WalletId},{credential.IssuerId},{credential.Status},{credential.IssuedAt:yyyy-MM-dd},{credential.ExpiresAt?.ToString("yyyy-MM-dd") ?? "N/A"}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    // System Alert Endpoint
    [HttpPost("alert")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendSystemAlert([FromBody] SystemAlertRequest request)
    {
        await _notificationService.SendSystemAlert(
            request.Message,
            Enum.Parse<AlertSeverity>(request.Severity));

        return Ok();
    }

    // Request DTOs
    public class CreateWalletRequest
    {
        public string UserId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string? WalletName { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class IssueCredentialRequest
    {
        public Guid WalletId { get; set; }
        public Guid IssuerId { get; set; }
        public string Type { get; set; } = "";
        public string Data { get; set; } = "{}";
        public string SchemaVersion { get; set; } = "";
        public DateTimeOffset? ExpiresAt { get; set; }
        public bool AutoActivate { get; set; }
    }

    public class SuspendRequest
    {
        public string? Reason { get; set; }
    }

    public class RevokeRequest
    {
        public string? Reason { get; set; }
    }

    public class ExportRequest
    {
        public string? Type { get; set; }
        public string? TenantId { get; set; }
    }

    public class SystemAlertRequest
    {
        public string Message { get; set; } = "";
        public string Severity { get; set; } = "Info";
    }
}