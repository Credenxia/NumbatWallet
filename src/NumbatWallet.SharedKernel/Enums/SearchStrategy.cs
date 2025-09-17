namespace NumbatWallet.SharedKernel.Enums;

/// <summary>
/// Defines the search strategy for tokenized fields
/// </summary>
public enum SearchStrategy
{
    /// <summary>
    /// No search capability for this field
    /// </summary>
    None = 0,

    /// <summary>
    /// Exact match only using full value hash
    /// </summary>
    Exact = 1,

    /// <summary>
    /// Prefix search with progressive tokens (1-5 characters)
    /// </summary>
    Prefix = 2,

    /// <summary>
    /// Phonetic search using Soundex/Metaphone for name variations
    /// </summary>
    Phonetic = 3,

    /// <summary>
    /// Fuzzy search with edit distance tolerance
    /// </summary>
    Fuzzy = 4,

    /// <summary>
    /// Full-text search (only for unencrypted fields)
    /// </summary>
    FullText = 5,

    /// <summary>
    /// Combination of exact and prefix
    /// </summary>
    ExactAndPrefix = 6
}