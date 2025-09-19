using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Web.Api.GraphQL.Inputs;

public record CreatePersonInput(
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateTime DateOfBirth,
    string PhoneNumber,
    string? CountryCode,
    AddressDto? Address);

public record UpdatePersonInput(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    DateTime? DateOfBirth,
    string PhoneNumber,
    string? CountryCode,
    AddressDto? Address);