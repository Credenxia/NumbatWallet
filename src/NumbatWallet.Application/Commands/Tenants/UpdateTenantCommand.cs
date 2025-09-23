namespace NumbatWallet.Application.Commands.Tenants;

/// <summary>
/// Command to update a tenant
/// </summary>
public record UpdateTenantCommand(
    Guid Id,
    string? Name = null,
    bool? IsActive = null,
    string? SubscriptionTier = null,
    Dictionary<string, object>? Settings = null);