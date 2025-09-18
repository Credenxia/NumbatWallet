using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Infrastructure.Data;

public class NumbatWalletDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<NumbatWalletDbContext> _logger;
    private IDbContextTransaction? _currentTransaction;

    public NumbatWalletDbContext(
        DbContextOptions<NumbatWalletDbContext> options,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IEventDispatcher eventDispatcher,
        ILogger<NumbatWalletDbContext> logger) : base(options)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Issuer> Issuers => Set<Issuer>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add interceptors for protection and auditing
        // These would be configured in the DI container in production

        // Apply snake_case naming convention for PostgreSQL
        optionsBuilder.UseSnakeCaseNamingConvention();

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NumbatWalletDbContext).Assembly);

        // Apply global query filters for multi-tenancy
        modelBuilder.Entity<Wallet>().HasQueryFilter(w => w.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Credential>().HasQueryFilter(c => c.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Person>().HasQueryFilter(p => p.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Issuer>().HasQueryFilter(i => i.TenantId == _tenantService.TenantId);

        // Configure JSONB for PostgreSQL
        modelBuilder.HasPostgresExtension("pgcrypto"); // For encryption functions if needed
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Apply audit fields and tenant information
        foreach (var entry in ChangeTracker.Entries<AuditableEntity<Guid>>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = _dateTimeService.UtcNow;
                entry.Entity.CreatedBy = _currentUserService.UserId;

                if (entry.Entity is ITenantAware tenantEntity)
                {
                    tenantEntity.TenantId = _tenantService.TenantId;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = _dateTimeService.UtcNow;
                entry.Entity.ModifiedBy = _currentUserService.UserId;
            }
        }

        // Get domain events before saving
        var domainEvents = ChangeTracker.Entries<Entity<Guid>>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful save
        if (domainEvents.Any())
        {
            await _eventDispatcher.DispatchAsync(domainEvents, cancellationToken);

            // Clear domain events from entities
            ChangeTracker.Entries<Entity<Guid>>()
                .ToList()
                .ForEach(e => e.Entity.ClearDomainEvents());
        }

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) throw new InvalidOperationException("No transaction started");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }
    }

    public bool HasActiveTransaction => _currentTransaction != null;
}