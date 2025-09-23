using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Queries.Wallets;

/// <summary>
/// Query for retrieving all wallets without pagination
/// </summary>
public sealed class GetAllWalletsQuery : IQuery<IEnumerable<WalletDto>>
{
    public Guid? PersonId { get; set; }
    public string? Status { get; set; }
}