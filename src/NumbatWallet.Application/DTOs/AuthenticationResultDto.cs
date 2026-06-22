namespace NumbatWallet.Application.DTOs;

public class AuthenticationResultDto
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string[] Roles { get; init; }
    public Dictionary<string, string> Claims { get; init; } = new();
}