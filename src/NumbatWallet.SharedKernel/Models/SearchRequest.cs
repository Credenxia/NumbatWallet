namespace NumbatWallet.SharedKernel.Models;

/// <summary>
/// Standard search request model
/// </summary>
public class SearchRequest : PagedRequest
{
    public List<string> SearchFields { get; set; } = new();
    public bool IncludeDeleted { get; set; } = false;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<Guid> Ids { get; set; } = new();
    public string? TenantId { get; set; }
}