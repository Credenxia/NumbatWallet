namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for generating privacy-preserving search tokens using HMAC for PII data
/// </summary>
public interface IHmacSearchTokenService
{
    /// <summary>
    /// Generates search tokens for a person's full name using various strategies
    /// </summary>
    /// <param name="fullName">The full name to tokenize</param>
    /// <returns>List of HMAC tokens for searching</returns>
    Task<List<string>> GenerateNameTokensAsync(string fullName);

    /// <summary>
    /// Generates search tokens for a search term (used when searching)
    /// </summary>
    /// <param name="searchTerm">The search term entered by user</param>
    /// <returns>List of tokens to match against index</returns>
    Task<List<string>> GenerateNameSearchTokensAsync(string searchTerm);

    /// <summary>
    /// Generates a search token for an email address
    /// </summary>
    /// <param name="email">The email address to tokenize</param>
    /// <returns>HMAC token for the normalized email</returns>
    Task<string?> GenerateEmailTokenAsync(string email);

    /// <summary>
    /// Generates a search token for a date with specified granularity
    /// </summary>
    /// <param name="dateTime">The date to tokenize</param>
    /// <param name="granularity">The level of date precision</param>
    /// <returns>HMAC token for the date</returns>
    Task<string> GenerateDateTokenAsync(DateTime dateTime, DateGranularity granularity);

    /// <summary>
    /// Generates tokens for multiple persons in a batch operation
    /// </summary>
    /// <param name="persons">Collection of person data to tokenize</param>
    /// <returns>Dictionary mapping person ID to their tokens</returns>
    Task<Dictionary<string, List<string>>> GenerateBulkTokensAsync(
        IEnumerable<PersonSearchData> persons);
}

/// <summary>
/// Defines the granularity level for date tokenization
/// </summary>
public enum DateGranularity
{
    /// <summary>
    /// Year only (for age range searches)
    /// </summary>
    Year,

    /// <summary>
    /// Year and month (for more precise matching)
    /// </summary>
    YearMonth,

    /// <summary>
    /// Full date (for exact DOB matching)
    /// </summary>
    FullDate
}

/// <summary>
/// Data structure for batch tokenization of person search data
/// </summary>
public class PersonSearchData
{
    /// <summary>
    /// Unique identifier for the person
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Person's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Person's email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Person's date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}