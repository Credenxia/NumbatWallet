namespace NumbatWallet.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}