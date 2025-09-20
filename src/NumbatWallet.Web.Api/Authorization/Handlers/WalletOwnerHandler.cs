using Microsoft.AspNetCore.Authorization;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Web.Api.Authorization.Requirements;

namespace NumbatWallet.Web.Api.Authorization.Handlers;

public class WalletOwnerHandler : AuthorizationHandler<WalletOwnerRequirement, Wallet>
{
    private readonly ILogger<WalletOwnerHandler> _logger;

    public WalletOwnerHandler(ILogger<WalletOwnerHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WalletOwnerRequirement requirement,
        Wallet resource)
    {
        // Allow admin access if configured
        if (requirement.AllowAdminAccess && context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin accessed wallet {WalletId}", resource.Id);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Get user's person ID from claims
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("person_id");
        if (userIdClaim == null)
        {
            _logger.LogWarning("User has no identifier claim");
            return Task.CompletedTask;
        }

        // Check if user owns this wallet
        if (Guid.TryParse(userIdClaim.Value, out var userId))
        {
            if (resource.PersonId == userId)
            {
                _logger.LogDebug("User {UserId} authorized as owner of wallet {WalletId}",
                    userId, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // Check tenant match as additional security
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim != null && resource.TenantId == tenantClaim.Value)
        {
            // User is in same tenant but not owner - check if they have delegation rights
            if (context.User.HasClaim("permissions", "wallet:view:delegated"))
            {
                _logger.LogDebug("User {UserId} authorized via delegation for wallet {WalletId}",
                    userIdClaim.Value, resource.Id);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        _logger.LogWarning("User {UserId} denied access to wallet {WalletId}",
            userIdClaim.Value, resource.Id);
        return Task.CompletedTask;
    }
}