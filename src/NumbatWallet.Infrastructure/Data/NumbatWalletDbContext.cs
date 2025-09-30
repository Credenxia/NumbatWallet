using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Entities;
using NumbatWallet.Infrastructure.EventSourcing;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Primitives;

namespace NumbatWallet.Infrastructure.Data;

public class NumbatWalletDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEventDispatcher _eventDispatcher;
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
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Issuer> Issuers => Set<Issuer>();
    public DbSet<TenantCertificate> TenantCertificates => Set<TenantCertificate>();
    public DbSet<CertificateAuthority> CertificateAuthorities => Set<CertificateAuthority>();
    public DbSet<CertificateTrustStore> CertificateTrustStores => Set<CertificateTrustStore>();
    public DbSet<CertificateRevocation> CertificateRevocations => Set<CertificateRevocation>();
    public DbSet<WalletTemplate> WalletTemplates => Set<WalletTemplate>();
    public DbSet<Issuance> Issuances => Set<Issuance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    // Event sourcing entities
    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();
    public DbSet<EventSnapshot> EventSnapshots => Set<EventSnapshot>();

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
        // Use dynamic evaluation of TenantId to ensure it's evaluated at query time
        modelBuilder.Entity<Wallet>().HasQueryFilter(w => w.TenantId == _tenantService.TenantId.ToString());
        modelBuilder.Entity<Credential>().HasQueryFilter(c => c.TenantId == _tenantService.TenantId.ToString());
        modelBuilder.Entity<Person>().HasQueryFilter(p => p.TenantId == _tenantService.TenantId.ToString());
        modelBuilder.Entity<Issuer>().HasQueryFilter(i => i.TenantId == _tenantService.TenantId.ToString());
        modelBuilder.Entity<WalletTemplate>().HasQueryFilter(wt => wt.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Issuance>().HasQueryFilter(i => i.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a => a.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<AdminUser>().HasQueryFilter(u => u.TenantId == _tenantService.TenantId);

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Identifier).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Identifier).IsUnique();

            // Configure Settings as JSON column (SQLite: text, PostgreSQL: jsonb)
            if (Database.IsSqlite())
            {
                entity.Property(e => e.Settings)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
            else
            {
                entity.Property(e => e.Settings)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });

        // Configure WalletTemplate entity
        modelBuilder.Entity<WalletTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(20);

            // Configure complex properties as JSON
            if (Database.IsSqlite())
            {
                entity.Property(e => e.Fields)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<WalletField>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<WalletField>());

                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

                entity.Property(e => e.SupportedCredentialTypes)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            }
            else
            {
                entity.Property(e => e.Fields)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<WalletField>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<WalletField>());

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

                entity.Property(e => e.SupportedCredentialTypes)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            }

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        // Configure JSONB for PostgreSQL only
        if (!Database.IsSqlite())
        {
            modelBuilder.HasPostgresExtension("pgcrypto"); // For encryption functions if needed
        }

        // Configure event sourcing entities
        EventSourcingConfiguration.ConfigureEventSourcing(modelBuilder);

        // Apply tenant filter to event store
        modelBuilder.Entity<StoredEvent>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId.ToString());
        modelBuilder.Entity<EventSnapshot>().HasQueryFilter(s => s.TenantId == _tenantService.TenantId.ToString());
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

                // NOTE: Tenant ID setting is handled by TenantInterceptor using ICurrentTenantService
                // Removed duplicate logic here to avoid conflicts with string-based tenant IDs
                // if (entry.Entity is ITenantAware tenantEntity)
                // {
                //     var tenantIdString = _tenantService.TenantId == Guid.Empty ? null : _tenantService.TenantId.ToString();
                //     if (!string.IsNullOrEmpty(tenantIdString))
                //     {
                //         tenantEntity.SetTenantId(tenantIdString);
                //     }
                // }
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
        if (domainEvents.Count > 0)
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
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction started");
        }

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
