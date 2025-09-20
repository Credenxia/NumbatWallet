using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Web.Api.Authorization.Requirements;

namespace NumbatWallet.Web.Api.Authorization.Handlers;

public class CredentialOwnerHandler : AuthorizationHandler<CredentialOwnerRequirement, Credential>
{
    private readonly NumbatWalletDbContext _dbContext;
    private readonly ILogger<CredentialOwnerHandler> _logger;

    public CredentialOwnerHandler(
        NumbatWalletDbContext dbContext,
        ILogger<CredentialOwnerHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CredentialOwnerRequirement requirement,
        Credential resource)
    {
        // Allow admin access if configured
        if (requirement.AllowAdminAccess && context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin accessed credential {CredentialId}", resource.Id);
            context.Succeed(requirement);
            return;
        }

        // Get user's person ID from claims
        var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst("person_id");
        if (userIdClaim == null)
        {
            _logger.LogWarning("User has no identifier claim");
            return;
        }

        // Check if user owns the wallet that contains this credential
        if (Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var wallet = await _dbContext.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == resource.WalletId);

            if (wallet != null && wallet.PersonId == userId)
            {
                _logger.LogDebug("User {UserId} authorized as owner of credential {CredentialId}",
                    userId, resource.Id);
                context.Succeed(requirement);
                return;
            }
        }

        // Check if user is the issuer (if allowed)
        if (requirement.AllowIssuerAccess)
        {
            var issuerIdClaim = context.User.FindFirst("issuer_id");
            if (issuerIdClaim != null && Guid.TryParse(issuerIdClaim.Value, out var issuerId))
            {
                if (resource.IssuerId == issuerId)
                {
                    _logger.LogDebug("Issuer {IssuerId} authorized for credential {CredentialId}",
                        issuerId, resource.Id);
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        _logger.LogWarning("User {UserId} denied access to credential {CredentialId}",
            userIdClaim.Value, resource.Id);
    }
}