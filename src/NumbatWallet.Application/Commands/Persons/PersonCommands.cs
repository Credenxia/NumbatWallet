using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Persons;

public record CreatePersonCommand(
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateTime? DateOfBirth,
    string? PhoneNumber,
    string? CountryCode,
    AddressDto? Address,
    string UserId) : ICommand<PersonDto>;

public record UpdatePersonCommand(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    DateTime? DateOfBirth,
    string? PhoneNumber,
    string? CountryCode,
    AddressDto? Address) : ICommand<PersonDto>;

public record VerifyPersonIdentityCommand(
    Guid PersonId,
    string DocumentType,
    string DocumentNumber,
    string? DocumentIssuingCountry,
    DateTime? DocumentExpiry,
    byte[]? DocumentImage) : ICommand<VerifyIdentityResult>;

public record DeletePersonCommand(Guid PersonId) : ICommand<bool>;

public record VerifyIdentityResult(
    bool IsVerified,
    string? VerificationId,
    DateTime VerifiedAt,
    string? FailureReason);