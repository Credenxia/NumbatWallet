using Microsoft.EntityFrameworkCore.Storage;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly NumbatWalletDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(
        NumbatWalletDbContext context,
        IPersonRepository personRepository,
        IWalletRepository walletRepository,
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();

        // Register repositories
        PersonRepository = personRepository;
        WalletRepository = walletRepository;
        CredentialRepository = credentialRepository;
        IssuerRepository = issuerRepository;
    }

    public IPersonRepository PersonRepository { get; }
    public IWalletRepository WalletRepository { get; }
    public ICredentialRepository CredentialRepository { get; }
    public IIssuerRepository IssuerRepository { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Transaction not started.");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("Transaction not started.");
        }

        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public bool HasActiveTransaction => _transaction != null;

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
            }

            _disposed = true;
        }
    }
}