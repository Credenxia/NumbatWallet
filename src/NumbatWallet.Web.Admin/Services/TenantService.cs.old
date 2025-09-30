using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Infrastructure.Data;

namespace NumbatWallet.Web.Admin.Services;

public interface ITenantService
{
    Task<TenantInfo?> GetCurrentTenantAsync();
    Task<TenantInfo?> GetTenantByIdAsync(string tenantId);
    Task<List<TenantInfo>> GetAllTenantsAsync();
    Task<bool> UpdateTenantSettingsAsync(string tenantId, TenantSettings settings);
}

public class TenantService : ITenantService
{
    private readonly NumbatWalletDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        NumbatWalletDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TenantInfo?> GetCurrentTenantAsync()
    {
        // Get tenant from current user's claims
        var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
        {
            // If no tenant claim, get from header or query string (for multi-tenant scenarios)
            tenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault()
                ?? _httpContextAccessor.HttpContext?.Request.Query["tenantId"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("No tenant ID found in request");
            return null;
        }

        return await GetTenantByIdAsync(tenantId);
    }

    public async Task<TenantInfo?> GetTenantByIdAsync(string tenantId)
    {
        // Mock implementation since Tenants table doesn't exist yet
        // In production, this would query the actual Tenants table

        // Get wallet count for this tenant
        var walletCount = await _context.Wallets
            .Where(w => w.TenantId == tenantId)
            .CountAsync();

        if (walletCount == 0 && tenantId != "default")
        {
            return null;
        }

        // Calculate monthly usage from credentials
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var verificationCount = await _context.Credentials
            .Where(c => c.TenantId == tenantId && c.IssuedAt >= startOfMonth)
            .CountAsync();

        // Return mock tenant data
        var activeWalletCount = await _context.Wallets
            .Where(w => w.TenantId == tenantId && w.Status == SharedKernel.Enums.WalletStatus.Active)
            .CountAsync();

        return new TenantInfo
        {
            Id = tenantId,
            Name = tenantId == "default" ? "Default Tenant" : $"Tenant {tenantId}",
            DisplayName = tenantId == "default" ? "Default Organization" : $"Organization {tenantId}",
            LogoUrl = null,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true,
            Plan = "Standard",
            MonthlyUsage = verificationCount,
            WalletCount = walletCount,
            ActiveWalletCount = activeWalletCount,
            Settings = new TenantSettings
            {
                PrimaryColor = "#4F46E5",
                SecondaryColor = "#10B981",
                MaxWallets = 10000,
                MaxVerificationsPerMonth = 100000,
                EnableWhiteLabeling = false,
                CustomDomain = null
            }
        };
    }

    public async Task<List<TenantInfo>> GetAllTenantsAsync()
    {
        // Get unique tenant IDs from wallets
        var tenantIds = await _context.Wallets
            .Select(w => w.TenantId)
            .Distinct()
            .ToListAsync();

        var tenantInfos = new List<TenantInfo>();

        foreach (var tenantId in tenantIds)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var verificationCount = await _context.Credentials
                .Where(c => c.TenantId == tenantId && c.IssuedAt >= startOfMonth)
                .CountAsync();

            var walletCount = await _context.Wallets
                .Where(w => w.TenantId == tenantId)
                .CountAsync();

            var activeWalletCount = await _context.Wallets
                .Where(w => w.TenantId == tenantId && w.Status == SharedKernel.Enums.WalletStatus.Active)
                .CountAsync();

            tenantInfos.Add(new TenantInfo
            {
                Id = tenantId,
                Name = tenantId == "default" ? "Default Tenant" : $"Tenant {tenantId}",
                DisplayName = tenantId == "default" ? "Default Organization" : $"Organization {tenantId}",
                LogoUrl = null,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true,
                Plan = "Standard",
                MonthlyUsage = verificationCount,
                WalletCount = walletCount,
                ActiveWalletCount = activeWalletCount
            });
        }

        return tenantInfos;
    }

    public async Task<bool> UpdateTenantSettingsAsync(string tenantId, TenantSettings settings)
    {
        // Mock implementation - in production would update actual tenant settings
        // For now, just validate the tenant exists by checking if any wallets belong to it
        var tenantExists = await _context.Wallets
            .AnyAsync(w => w.TenantId == tenantId);

        if (!tenantExists && tenantId != "default")
        {
            return false;
        }

        // In a real implementation, we would save these settings
        // For now, just return success since we don't have a Tenant entity yet
        return true;
    }
}

// Models
public class TenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Plan { get; set; } = "Standard";
    public int MonthlyUsage { get; set; }
    public int WalletCount { get; set; }
    public int ActiveWalletCount { get; set; }
    public TenantSettings? Settings { get; set; }
}

public class TenantSettings
{
    public string PrimaryColor { get; set; } = "#4F46E5";
    public string SecondaryColor { get; set; } = "#10B981";
    public int MaxWallets { get; set; } = 10000;
    public int MaxVerificationsPerMonth { get; set; } = 100000;
    public bool EnableWhiteLabeling { get; set; }
    public string? CustomDomain { get; set; }
}