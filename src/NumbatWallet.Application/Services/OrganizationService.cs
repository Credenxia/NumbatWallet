using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _organizationRepository.GetByIdAsync(id, cancellationToken);
        return organization != null ? MapToDto(organization) : null;
    }

    public async Task<IEnumerable<OrganizationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await _organizationRepository.GetAllAsync(cancellationToken);
        return organizations.Select(MapToDto);
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto, CancellationToken cancellationToken = default)
    {
        var organization = Organization.Create(
            dto.Name,
            dto.Type,
            dto.Description
        );

        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(organization);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationDto dto, CancellationToken cancellationToken = default)
    {
        var organization = await _organizationRepository.GetByIdAsync(id, cancellationToken);
        if (organization == null)
            throw new InvalidOperationException($"Organization with ID {id} not found");

        if (dto.Name != null)
            organization.UpdateName(dto.Name);

        if (dto.Description != null)
            organization.UpdateDescription(dto.Description);

        await _organizationRepository.UpdateAsync(organization, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToDto(organization);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _organizationRepository.GetByIdAsync(id, cancellationToken);
        if (organization == null)
            return false;

        await _organizationRepository.DeleteAsync(organization, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return true;
    }

    private static OrganizationDto MapToDto(Organization organization)
    {
        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Type = organization.Type.ToString(),
            Description = organization.Description,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}