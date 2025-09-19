using NumbatWallet.Domain.Enums;

namespace NumbatWallet.Application.DTOs;

public record OrganizationDto(
    Guid Id,
    string Name,
    string Identifier,
    OrganizationType Type,
    string ContactEmail,
    string? ContactPhone,
    AddressDto? Address,
    string? Website,
    bool IsVerified,
    DateTime CreatedAt,
    DateTime? LastModified);