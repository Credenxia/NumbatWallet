namespace NumbatWallet.Application.DTOs;

/// <summary>
/// Data transfer object for Person entity
/// </summary>
public sealed class PersonDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string EmailVerificationStatus { get; init; } = string.Empty;
    public string PhoneVerificationStatus { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}