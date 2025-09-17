using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for generating search tokens for protected fields.
/// Uses HMAC to create searchable tokens that work regardless of encryption status.
/// </summary>
public interface ISearchTokenService
{
    /// <summary>
    /// Generates search tokens based on the specified strategy
    /// </summary>
    /// <param name="value">The value to tokenize</param>
    /// <param name="strategy">The search strategy to use</param>
    /// <param name="entityType">The entity type for context</param>
    /// <param name="fieldName">The field name for context</param>
    /// <returns>Collection of search tokens</returns>
    Task<IEnumerable<string>> GenerateTokensAsync(
        string value,
        SearchStrategy strategy,
        string entityType,
        string fieldName);

    /// <summary>
    /// Generates a single exact match token
    /// </summary>
    /// <param name="value">The value to tokenize</param>
    /// <param name="entityType">The entity type for context</param>
    /// <param name="fieldName">The field name for context</param>
    /// <returns>The search token</returns>
    Task<string> GenerateExactTokenAsync(
        string value,
        string entityType,
        string fieldName);

    /// <summary>
    /// Generates prefix tokens for partial matching
    /// </summary>
    /// <param name="value">The value to tokenize</param>
    /// <param name="minLength">Minimum prefix length</param>
    /// <param name="maxLength">Maximum prefix length</param>
    /// <returns>Collection of prefix tokens</returns>
    Task<IEnumerable<string>> GeneratePrefixTokensAsync(
        string value,
        int minLength = 3,
        int maxLength = 10);

    /// <summary>
    /// Generates phonetic tokens for fuzzy matching
    /// </summary>
    /// <param name="value">The value to tokenize</param>
    /// <returns>Collection of phonetic tokens</returns>
    Task<IEnumerable<string>> GeneratePhoneticTokensAsync(string value);

    /// <summary>
    /// Validates that a search token matches a value
    /// </summary>
    /// <param name="value">The original value</param>
    /// <param name="token">The search token</param>
    /// <param name="entityType">The entity type for context</param>
    /// <param name="fieldName">The field name for context</param>
    /// <returns>True if the token matches the value</returns>
    Task<bool> ValidateTokenAsync(
        string value,
        string token,
        string entityType,
        string fieldName);

    /// <summary>
    /// Gets the search strategy for a field based on its configuration
    /// </summary>
    /// <param name="entityType">The entity type</param>
    /// <param name="fieldName">The field name</param>
    /// <returns>The configured search strategy</returns>
    Task<SearchStrategy> GetFieldStrategyAsync(
        string entityType,
        string fieldName);

    /// <summary>
    /// Bulk generates tokens for multiple values
    /// </summary>
    /// <param name="values">Dictionary of field names to values</param>
    /// <param name="entityType">The entity type</param>
    /// <returns>Dictionary of field names to token collections</returns>
    Task<Dictionary<string, IEnumerable<string>>> BulkGenerateTokensAsync(
        Dictionary<string, string> values,
        string entityType);
}