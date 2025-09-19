using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Organizations;

public record GetOrganizationByIdQuery(Guid OrganizationId) : IQuery<OrganizationDto?>;

public record GetAllOrganizationsQuery(
    bool? VerifiedOnly = null) : IQuery<IEnumerable<OrganizationDto>>;

public record SearchOrganizationsQuery(
    string? SearchTerm,
    bool? IsVerified,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<IEnumerable<OrganizationDto>>;

public record GetOrganizationMembersQuery(Guid OrganizationId) : IQuery<IEnumerable<PersonDto>>;

public record GetOrganizationStatisticsQuery(Guid OrganizationId) : IQuery<object>;