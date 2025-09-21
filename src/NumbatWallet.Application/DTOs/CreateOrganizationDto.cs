namespace NumbatWallet.Application.DTOs;

public class CreateOrganizationDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Type { get; set; } // Government, Enterprise, etc.
    public AddressDto? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}