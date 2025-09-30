using NumbatWallet.Application.CQRS.Interfaces;

namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Command to create a new tenant
/// </summary>
public record CreateTenantCommand(
    string Name,
    string Identifier,
    string SubscriptionTier = "Basic",
    Dictionary<string, object>? Settings = null) : ICommand<Guid>;