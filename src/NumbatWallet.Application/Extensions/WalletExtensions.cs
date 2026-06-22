using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Application.Extensions;

/// <summary>
/// Extension methods for Wallet entity to DTO conversions
/// Replaces AutoMapper for better performance and explicit mappings
/// </summary>
public static class WalletExtensions
{
    /// <summary>
    /// Converts Wallet entity to WalletDto
    /// </summary>
    public static WalletDto ToDto(this Wallet wallet, string? personName = null)
    {
        ArgumentNullException.ThrowIfNull(wallet);

        return new WalletDto
        {
            Id = wallet.Id.ToString(),
            PersonId = wallet.PersonId.ToString(),
            PersonName = personName ?? "Unknown", // Caller should provide this from Person entity
            Name = wallet.WalletName,
            Status = wallet.Status.ToString(),
            IsActive = wallet.Status == SharedKernel.Enums.WalletStatus.Active,
            IsSuspended = wallet.Status == SharedKernel.Enums.WalletStatus.Suspended,
            CredentialCount = wallet.GetCredentials().Count,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.CreatedAt, // Wallet doesn't have UpdatedAt, use CreatedAt
            Metadata = new Dictionary<string, string>
            {
                ["WalletDid"] = wallet.WalletDid,
                ["TenantId"] = wallet.TenantId
            }
        };
    }

    /// <summary>
    /// Converts collection of Wallet entities to WalletDto collection
    /// </summary>
    public static IEnumerable<WalletDto> ToDtos(this IEnumerable<Wallet> wallets)
    {
        ArgumentNullException.ThrowIfNull(wallets);
        return wallets.Select(w => w.ToDto());
    }
}