using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NumbatWallet.Infrastructure.Search;

public interface ISearchTokenService
{
    // Name search tokens
    Task<List<string>> GenerateNameTokensAsync(string fullName);
    Task<List<string>> GenerateNameSearchTokensAsync(string searchTerm);

    // Email search tokens
    Task<string> GenerateEmailTokenAsync(string email);

    // Date search tokens
    Task<string> GenerateDateTokenAsync(DateTime dateTime, DateGranularity granularity);

    // Phone search tokens
    Task<string> GeneratePhoneTokenAsync(string phoneNumber);

    // Batch operations
    Task<Dictionary<string, List<string>>> GenerateBulkTokensAsync(
        IEnumerable<PersonSearchData> persons);

    // Verify token matches
    Task<bool> VerifyTokenMatchAsync(string input, string token);
}

public enum DateGranularity
{
    Year,      // For age range searches
    YearMonth, // For more precise matching
    FullDate   // For exact DOB matching
}

public class PersonSearchData
{
    public Guid PersonId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
}

public class SearchTokenResult
{
    public Guid PersonId { get; set; }
    public List<string> NameTokens { get; set; } = new();
    public string? EmailToken { get; set; }
    public List<string> DateTokens { get; set; } = new();
    public string? PhoneToken { get; set; }
}