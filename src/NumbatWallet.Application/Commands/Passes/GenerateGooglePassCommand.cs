using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Commands.Passes;

public sealed class GenerateGooglePassCommand : ICommand<WalletPackageDto>
{
    public required Guid TenantId { get; init; }
    public required Guid PersonId { get; init; }
    public Guid? TemplateId { get; init; }
    public Dictionary<string, object> AdditionalData { get; init; } = [];
}
