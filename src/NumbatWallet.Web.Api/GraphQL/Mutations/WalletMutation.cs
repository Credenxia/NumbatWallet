using NumbatWallet.Application.Commands.Wallets;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Queries.Wallets;
using NumbatWallet.Application.Wallets.Commands.CreateWallet;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.Web.Api.Security;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.GraphQL.Mutations;

/// <summary>
/// GraphQL mutations for wallet operations
/// </summary>
[ExtendObjectType("Mutation")]
public class WalletMutation
{
    private readonly ICommandHandler<CreateWalletCommand, WalletDto> _createWalletHandler;
    private readonly ICommandHandler<UpdateWalletCommand, WalletDto> _updateWalletHandler;
    private readonly ICommandHandler<DeleteWalletCommand, bool> _deleteWalletHandler;
    private readonly IQueryHandler<GetWalletByIdQuery, WalletDto?> _getWalletHandler;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WalletMutation> _logger;
    private readonly ICacheService _cacheService;

    public WalletMutation(
        ICommandHandler<CreateWalletCommand, WalletDto> createWalletHandler,
        ICommandHandler<UpdateWalletCommand, WalletDto> updateWalletHandler,
        ICommandHandler<DeleteWalletCommand, bool> deleteWalletHandler,
        IQueryHandler<GetWalletByIdQuery, WalletDto?> getWalletHandler,
        ISecurityAuditService auditService,
        ILogger<WalletMutation> logger,
        ICacheService cacheService)
    {
        _createWalletHandler = createWalletHandler;
        _updateWalletHandler = updateWalletHandler;
        _deleteWalletHandler = deleteWalletHandler;
        _getWalletHandler = getWalletHandler;
        _auditService = auditService;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Create a new wallet
    /// </summary>
    [GraphQLDescription("Create a new digital wallet for a user")]
    [Authorize]
    public async Task<WalletDto> CreateWallet(
        CreateWalletInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Creating wallet for user {UserId}", userId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Wallet created for user: {userId}");
        }

        var command = new CreateWalletCommand
        {
            PersonId = Guid.Parse(userId ?? input.UserId ?? throw new InvalidOperationException("User ID is required")),
            Name = input.DisplayName,
            Description = null,
            Type = string.Equals(input.WalletType, "issuer", StringComparison.OrdinalIgnoreCase) ? WalletType.Issuer : WalletType.Holder,
            TenantId = null
        };

        var wallet = await _createWalletHandler.HandleAsync(command);

        // Cache the wallet
        await _cacheService.SetAsync($"wallet:{wallet.Id}", wallet, TimeSpan.FromHours(1));

        return wallet;
    }

    /// <summary>
    /// Update wallet settings
    /// </summary>
    [GraphQLDescription("Update wallet settings and metadata")]
    [Authorize]
    public async Task<WalletDto> UpdateWallet(
        UpdateWalletInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Updating wallet {WalletId}", input.WalletId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Wallet updated: {input.WalletId}");
        }

        var command = new UpdateWalletCommand
        {
            WalletId = input.WalletId,
            Name = input.DisplayName,
            Metadata = input.Metadata
        };

        var wallet = await _updateWalletHandler.HandleAsync(command);

        // Update cache
        await _cacheService.SetAsync($"wallet:{wallet.Id}", wallet, TimeSpan.FromHours(1));

        return wallet;
    }

    /// <summary>
    /// Deactivate a wallet
    /// </summary>
    [GraphQLDescription("Deactivate a digital wallet")]
    [Authorize]
    public async Task<bool> DeactivateWallet(
        DeactivateWalletInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogWarning("Deactivating wallet {WalletId} for reason: {Reason}",
            input.WalletId, input.Reason);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataDeletion,
                $"Wallet deactivated: {input.WalletId}");
        }

        var command = new DeleteWalletCommand(Guid.Parse(input.WalletId));

        var result = await _deleteWalletHandler.HandleAsync(command);

        // Remove from cache
        await _cacheService.RemoveAsync($"wallet:{input.WalletId}");

        return result;
    }

    /// <summary>
    /// Export wallet data
    /// </summary>
    [GraphQLDescription("Export wallet data in various formats")]
    [Authorize]
    public async Task<WalletExportDto> ExportWallet(
        ExportWalletInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;

        _logger.LogInformation("Exporting wallet {WalletId} in format {Format}",
            input.WalletId, input.Format);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataAccess,
                $"Wallet exported: {input.WalletId}");
        }

        // Get wallet data
        var query = new GetWalletByIdQuery(Guid.Parse(input.WalletId));
        var wallet = await _getWalletHandler.HandleAsync(query);

        if (wallet == null)
        {
            throw new InvalidOperationException($"Wallet {input.WalletId} not found");
        }

        // For now, return basic export data
        // Full export logic would involve gathering credentials and history
        var exportData = new WalletExportDto
        {
            WalletId = input.WalletId,
            Format = input.Format ?? "JSON",
            ExportedAt = DateTime.UtcNow,
            Data = wallet,
            IncludeCredentials = input.IncludeCredentials ?? true,
            IncludeHistory = input.IncludeHistory ?? false
        };

        return exportData;
    }

    /// <summary>
    /// Import wallet data
    /// </summary>
    [GraphQLDescription("Import wallet data from backup")]
    [Authorize]
    public async Task<WalletImportResultDto> ImportWallet(
        ImportWalletInput input,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Importing wallet data for user {UserId}", userId);

        if (httpContext != null)
        {
            await _auditService.LogSecurityEventAsync(
                httpContext,
                SecurityEventType.DataModification,
                $"Wallet import initiated by: {userId}");
        }

        // Import logic would deserialize the data and create a new wallet
        // For now, create a new wallet with imported data
        var walletData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(input.Data);

        var createCommand = new CreateWalletCommand
        {
            PersonId = Guid.Parse(userId ?? throw new InvalidOperationException("User ID is required")),
            Name = "Imported Wallet",
            Description = $"Imported on {DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}",
            Type = WalletType.Holder,
            TenantId = null
        };

        var wallet = await _createWalletHandler.HandleAsync(createCommand);

        var result = new WalletImportResultDto
        {
            Success = true,
            WalletId = wallet.Id,
            ImportedAt = DateTime.UtcNow,
            CredentialsImported = 0, // Would be populated from actual import
            CredentialsSkipped = 0,
            Errors = new List<string>()
        };

        return result;
    }

    private static string GeneratePublicKey()
    {
        // Mock public key generation
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// Input types for wallet mutations
public class CreateWalletInput
{
    public string? UserId { get; set; }
    public required string DisplayName { get; set; }
    public string? WalletType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class UpdateWalletInput
{
    public required string WalletId { get; set; }
    public string? DisplayName { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class DeactivateWalletInput
{
    public required string WalletId { get; set; }
    public required string Reason { get; set; }
}

public class ExportWalletInput
{
    public required string WalletId { get; set; }
    public string? Format { get; set; }
    public bool? IncludeCredentials { get; set; }
    public bool? IncludeHistory { get; set; }
}

public class ImportWalletInput
{
    public required string Data { get; set; }
    public required string Format { get; set; }
    public string? Password { get; set; }
}

// DTOs for wallet operations
public class WalletExportDto
{
    public string WalletId { get; set; }
    public required string Format { get; set; }
    public DateTime ExportedAt { get; set; }
    public object? Data { get; set; }
    public bool IncludeCredentials { get; set; }
    public bool IncludeHistory { get; set; }
}

public class WalletImportResultDto
{
    public bool Success { get; set; }
    public string WalletId { get; set; }
    public DateTime ImportedAt { get; set; }
    public int CredentialsImported { get; set; }
    public int CredentialsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
}