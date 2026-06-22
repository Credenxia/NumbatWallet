using Microsoft.EntityFrameworkCore;

namespace NumbatWallet.Web.Api.Controllers;

/// <summary>
/// Admin API endpoints
/// POA: Placeholder for admin operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics from database
    /// </summary>
    [HttpGet("statistics/dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardStatistics([FromServices] Infrastructure.Data.NumbatWalletDbContext dbContext)
    {
        _logger.LogInformation("Admin: Getting dashboard statistics from database");

        try
        {
            // Query REAL data from database
            var totalPersons = await dbContext.Persons.CountAsync();
            var totalWallets = await dbContext.Wallets.CountAsync();
            var totalCredentials = await dbContext.Credentials.CountAsync();
            var activeCredentials = await dbContext.Credentials
                .CountAsync(c => c.Status == SharedKernel.Enums.CredentialStatus.Active);

            var today = DateTime.UtcNow.Date;
            var issuedToday = await dbContext.Credentials
                .CountAsync(c => c.IssuedAt.Date == today);

            var nextWeek = DateTime.UtcNow.AddDays(7);
            var expiringThisWeek = await dbContext.Credentials
                .CountAsync(c => c.ExpiresAt.HasValue && c.ExpiresAt.Value <= nextWeek && c.ExpiresAt.Value >= DateTime.UtcNow);

            var stats = new
            {
                TotalPersons = totalPersons,
                TotalWallets = totalWallets,
                TotalCredentials = totalCredentials,
                ActiveCredentials = activeCredentials,
                IssuedToday = issuedToday,
                ExpiringThisWeek = expiringThisWeek
            };

            _logger.LogInformation("Dashboard stats: {Stats}", System.Text.Json.JsonSerializer.Serialize(stats));
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard statistics from database");

            // Fallback to zeros if database not ready
            var fallbackStats = new
            {
                TotalPersons = 0,
                TotalWallets = 0,
                TotalCredentials = 0,
                ActiveCredentials = 0,
                IssuedToday = 0,
                ExpiringThisWeek = 0
            };

            return Ok(fallbackStats);
        }
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
