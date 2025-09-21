using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface IWalletService
{
    Task<WalletDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WalletDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WalletDto> CreateAsync(CreateWalletDto dto, CancellationToken cancellationToken = default);
    Task<WalletDto> UpdateAsync(Guid id, UpdateWalletDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SuspendAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<bool> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
}