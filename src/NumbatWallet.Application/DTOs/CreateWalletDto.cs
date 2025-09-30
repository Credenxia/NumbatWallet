namespace NumbatWallet.Application.DTOs;

public class CreateWalletDto
{
    public required Guid PersonId { get; set; }
    public required DeviceInfoDto DeviceInfo { get; set; }
    public string? Name { get; set; }
}

public class DeviceInfoDto
{
    public required string Platform { get; set; } // iOS, Android
    public required string DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? AppVersion { get; set; }
}
