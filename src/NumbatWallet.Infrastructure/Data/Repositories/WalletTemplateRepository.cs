using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Infrastructure.Data.Repositories;

public class WalletTemplateRepository : IWalletTemplateRepository
{
    private readonly NumbatWalletDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<WalletTemplateRepository> _logger;

    public WalletTemplateRepository(
        NumbatWalletDbContext context,
        ITenantService tenantService,
        ILogger<WalletTemplateRepository> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    private Guid GetCurrentTenantIdOrThrow()
    {
        if (_tenantService.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is required but not available.");
        }

        return _tenantService.TenantId;
    }

    // IRepository implementation
    public async Task<WalletTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting wallet template by ID: {Id}", id);

        return await _context.Set<WalletTemplate>()
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == GetCurrentTenantIdOrThrow(), cancellationToken);
    }

    public async Task<IEnumerable<WalletTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all wallet templates for tenant: {TenantId}", _tenantService.TenantId);

        return await _context.Set<WalletTemplate>()
            .Where(t => t.TenantId == GetCurrentTenantIdOrThrow())
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<WalletTemplate> AddAsync(WalletTemplate template, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding new wallet template: {Name} of type {Type}", template.Name, template.Type);

        // Template properties are read-only, so we need to create a new instance
        // This should be done in the service layer, not repository
        await _context.Set<WalletTemplate>().AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return template;
    }

    public async Task UpdateAsync(WalletTemplate template, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating wallet template: {Id}", template.Id);

        // Template is already modified through domain methods
        _context.Set<WalletTemplate>().Update(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WalletTemplate template, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting wallet template: {Id}", template.Id);

        _context.Set<WalletTemplate>().Remove(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WalletTemplate>()
            .AnyAsync(t => t.Id == id && t.TenantId == GetCurrentTenantIdOrThrow(), cancellationToken);
    }

    public async Task<IEnumerable<WalletTemplate>> FindAsync(
        Expression<Func<WalletTemplate, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantIdOrThrow();

        return await _context.Set<WalletTemplate>()
            .Where(t => t.TenantId == tenantId)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WalletTemplate>> FindAsync(
        ISpecification<WalletTemplate> specification,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantIdOrThrow();

        var query = _context.Set<WalletTemplate>()
            .Where(t => t.TenantId == tenantId);

        // Apply specification criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // IWalletTemplateRepository specific methods
    public async Task<IReadOnlyList<WalletTemplate>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting wallet templates for tenant: {TenantId}", tenantId);

        var templates = await _context.Set<WalletTemplate>()
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.AsReadOnly();
    }

    public async Task<IReadOnlyList<WalletTemplate>> GetActiveTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active wallet templates for tenant: {TenantId}", _tenantService.TenantId);

        var tenantId = GetCurrentTenantIdOrThrow();

        var templates = await _context.Set<WalletTemplate>()
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.AsReadOnly();
    }

    public async Task<WalletTemplate?> GetByNameAsync(
        string name,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting wallet template by name: {Name} for tenant: {TenantId}", name, tenantId);

        return await _context.Set<WalletTemplate>()
            .FirstOrDefaultAsync(t => t.Name == name && t.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<WalletTemplate>> GetByTypeAsync(
        WalletTemplateType type,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting wallet templates by type: {Type}", type);

        var tenantId = GetCurrentTenantIdOrThrow();

        var templates = await _context.Set<WalletTemplate>()
            .Where(t => t.Type == type && t.TenantId == tenantId)
            .OrderByDescending(t => t.Version)
            .ToListAsync(cancellationToken);

        return templates.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(
        string name,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<WalletTemplate>()
            .AnyAsync(t => t.Name == name && t.TenantId == tenantId, cancellationToken);
    }
}