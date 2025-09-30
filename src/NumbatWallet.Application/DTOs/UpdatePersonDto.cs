namespace NumbatWallet.Application.DTOs;

public class UpdatePersonDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public AddressDto? Address { get; set; }
}
