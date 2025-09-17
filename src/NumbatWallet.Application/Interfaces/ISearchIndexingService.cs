using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing search indexes for protected fields.
/// Works independently of encryption status.
/// </summary>
public interface ISearchIndexingService
{
    /// <summary>
    /// Generates search index entries for an entity
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="entity">The entity to index</param>
    /// <returns>Array of search index entries</returns>
    Task<SearchIndexEntry[]> GenerateIndexEntriesAsync<T>(T entity) where T : class;

    /// <summary>
    /// Updates search indexes for an entity
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="entity">The entity to update indexes for</param>
    Task UpdateIndexAsync<T>(T entity) where T : class;

    /// <summary>
    /// Searches for entities using tokenized search
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="entityType">The type of entity to search</param>
    /// <param name="fieldName">The field to search in</param>
    /// <param name="options">Search options</param>
    /// <returns>List of matching entity IDs</returns>
    Task<IEnumerable<Guid>> SearchAsync(
        string searchTerm,
        string entityType,
        string fieldName,
        SearchOptions options);

    /// <summary>
    /// Removes search indexes for an entity
    /// </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="entityType">The type of entity</param>
    Task RemoveIndexAsync(Guid entityId, string entityType);

    /// <summary>
    /// Rebuilds all search indexes for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    Task RebuildIndexesAsync(Guid tenantId);
}

/// <summary>
/// Represents a search index entry
/// </summary>
public class SearchIndexEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string SearchToken { get; set; } = string.Empty;
    public SearchStrategy Strategy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Options for search operations
/// </summary>
public class SearchOptions
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool IncludeDeleted { get; set; } = false;
    public SearchStrategy Strategy { get; set; } = SearchStrategy.ExactAndPrefix;
    public int MaxResults { get; set; } = 100;
}