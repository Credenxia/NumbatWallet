using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface IPersonService
{
    Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PersonDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<PersonDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersonDto> CreateAsync(CreatePersonDto dto, CancellationToken cancellationToken = default);
    Task<PersonDto> UpdateAsync(Guid id, UpdatePersonDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PersonDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}