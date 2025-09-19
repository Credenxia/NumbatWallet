using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.Commands.Organizations;

public record CreateOrganizationCommand(
    string Name,
    string Identifier,
    OrganizationType Type,
    string ContactEmail,
    string? ContactPhone,
    AddressDto? Address,
    string? Website,
    string CreatedBy) : ICommand<OrganizationDto>;

public record UpdateOrganizationCommand(
    Guid Id,
    string? Name,
    string? ContactEmail,
    string? ContactPhone,
    AddressDto? Address,
    string? Website) : ICommand<OrganizationDto>;

public record VerifyOrganizationCommand(
    Guid OrganizationId,
    string VerificationType,
    Dictionary<string, string> VerificationData) : ICommand<VerifyOrganizationResult>;

public record AddOrganizationMemberCommand(
    Guid OrganizationId,
    Guid PersonId,
    string Role) : ICommand<bool>;

public record RemoveOrganizationMemberCommand(
    Guid OrganizationId,
    Guid PersonId) : ICommand<bool>;

public record VerifyOrganizationResult(
    bool IsVerified,
    string VerificationLevel,
    DateTime VerifiedAt,
    DateTime? ExpiresAt);