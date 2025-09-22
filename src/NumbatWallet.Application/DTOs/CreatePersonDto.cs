namespace NumbatWallet.Application.DTOs;

public class CreatePersonDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public AddressDto? Address { get; set; }
}
