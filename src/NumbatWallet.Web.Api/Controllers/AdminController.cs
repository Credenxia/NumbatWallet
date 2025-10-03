using Microsoft.AspNetCore.Mvc;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Admin API endpoints
/// POA: Placeholder for admin operations
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants (Admin only)
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetTenants()
    {
        _logger.LogInformation("Admin: Getting all tenants");

        // POA: Return empty list for now
        return Ok(new List<object>());
    }

    /// <summary>
    /// Delete a tenant (Admin only)
    /// </summary>
    [HttpDelete("tenants/{tenantId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteTenant(string tenantId)
    {
        _logger.LogWarning("Admin: Deleting tenant {TenantId}", tenantId);

        // POA: Return 204 for now
        return NoContent();
    }
}
