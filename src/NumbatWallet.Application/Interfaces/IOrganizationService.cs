using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto, CancellationToken cancellationToken = default);
    Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
