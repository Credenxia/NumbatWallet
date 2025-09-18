using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

public class HmacSearchTokenService : IHmacSearchTokenService
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<HmacSearchTokenService> _logger;

    private const int PREFIX_LENGTH = 5;
    private const string PEPPER_SECRET_NAME = "search-pepper";

    public HmacSearchTokenService(
        IKeyVaultService keyVaultService,
        ICurrentTenantService currentTenantService,
        IMemoryCache memoryCache,
        ILogger<HmacSearchTokenService> logger)
    {
        _keyVaultService = keyVaultService;
        _currentTenantService = currentTenantService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<List<string>> GenerateNameTokensAsync(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new List<string>();
        }

        var tokens = new List<string>();
        var normalized = NormalizeName(fullName);
        var pepper = await GetTenantPepperAsync();

        // Generate prefix tokens (first N characters)
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.Length >= 3) // Skip very short words
            {
                var prefix = word.Substring(0, Math.Min(PREFIX_LENGTH, word.Length));
                var prefixToken = GenerateHmacToken(pepper, $"prefix:{prefix}");
                tokens.Add(prefixToken);
            }
        }

        // Generate phonetic tokens (simplified - in production would use Double Metaphone library)
        foreach (var word in words)
        {
            var phonetic = GetSimplePhonetic(word);
            if (!string.IsNullOrEmpty(phonetic))
            {
                var phoneticToken = GenerateHmacToken(pepper, $"phonetic:{phonetic}");
                tokens.Add(phoneticToken);
            }
        }

        // Generate combination tokens for full name
        var fullNameToken = GenerateHmacToken(pepper, $"full:{normalized}");
        tokens.Add(fullNameToken);

        // Generate initials token
        var initials = string.Join("", words.Select(w => w[0]));
        if (initials.Length > 1)
        {
            var initialsToken = GenerateHmacToken(pepper, $"initials:{initials}");
            tokens.Add(initialsToken);
        }

        return tokens.Distinct().ToList();
    }

    public async Task<List<string>> GenerateNameSearchTokensAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<string>();
        }

        var tokens = new List<string>();
        var normalized = NormalizeName(searchTerm);
        var pepper = await GetTenantPepperAsync();

        // Generate prefix tokens for search term
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.Length >= 3)
            {
                var prefix = word.Substring(0, Math.Min(PREFIX_LENGTH, word.Length));
                var prefixToken = GenerateHmacToken(pepper, $"prefix:{prefix}");
                tokens.Add(prefixToken);
            }

            // Also generate phonetic for search
            var phonetic = GetSimplePhonetic(word);
            if (!string.IsNullOrEmpty(phonetic))
            {
                var phoneticToken = GenerateHmacToken(pepper, $"phonetic:{phonetic}");
                tokens.Add(phoneticToken);
            }
        }

        return tokens.Distinct().ToList();
    }

    public async Task<string?> GenerateEmailTokenAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.ToLowerInvariant().Trim();
        var pepper = await GetTenantPepperAsync();

        return GenerateHmacToken(pepper, $"email:{normalized}");
    }

    public async Task<string> GenerateDateTokenAsync(DateTime dateTime, DateGranularity granularity)
    {
        var pepper = await GetTenantPepperAsync();

        var dateString = granularity switch
        {
            DateGranularity.Year => dateTime.ToString("yyyy"),
            DateGranularity.YearMonth => dateTime.ToString("yyyy-MM"),
            DateGranularity.FullDate => dateTime.ToString("yyyy-MM-dd"),
            _ => throw new ArgumentException($"Unknown granularity: {granularity}")
        };

        return GenerateHmacToken(pepper, $"date:{granularity}:{dateString}");
    }

    public async Task<Dictionary<string, List<string>>> GenerateBulkTokensAsync(
        IEnumerable<PersonSearchData> persons)
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var person in persons)
        {
            var tokens = new List<string>();

            if (!string.IsNullOrWhiteSpace(person.FullName))
            {
                var nameTokens = await GenerateNameTokensAsync(person.FullName);
                tokens.AddRange(nameTokens);
            }

            if (!string.IsNullOrWhiteSpace(person.Email))
            {
                var emailToken = await GenerateEmailTokenAsync(person.Email);
                if (emailToken != null)
                {
                    tokens.Add(emailToken);
                }
            }

            if (person.DateOfBirth.HasValue)
            {
                // Generate tokens for different granularities
                var yearToken = await GenerateDateTokenAsync(person.DateOfBirth.Value, DateGranularity.Year);
                var fullDateToken = await GenerateDateTokenAsync(person.DateOfBirth.Value, DateGranularity.FullDate);
                tokens.Add(yearToken);
                tokens.Add(fullDateToken);
            }

            result[person.Id] = tokens.Distinct().ToList();
        }

        return result;
    }

    private string GenerateHmacToken(byte[] pepper, string data)
    {
        using var hmac = new HMACSHA256(pepper);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        // Use URL-safe base64 for storage efficiency
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async Task<byte[]> GetTenantPepperAsync()
    {
        var tenantId = _currentTenantService.TenantId ?? "default";
        var cacheKey = $"pepper:{tenantId}";

        if (_memoryCache.TryGetValue<byte[]>(cacheKey, out var cachedPepper))
        {
            return cachedPepper!;
        }

        try
        {
            var secretName = $"{PEPPER_SECRET_NAME}-{tenantId}";
            var secret = await _keyVaultService.GetSecretAsync(secretName);

            byte[] pepper;
            if (!string.IsNullOrEmpty(secret))
            {
                pepper = Convert.FromBase64String(secret);
            }
            else
            {
                // Generate new pepper for tenant
                pepper = RandomNumberGenerator.GetBytes(32);
                await _keyVaultService.SetSecretAsync(
                    secretName,
                    Convert.ToBase64String(pepper));

                _logger.LogInformation(
                    "Generated new search pepper for tenant {TenantId}",
                    tenantId);
            }

            _memoryCache.Set(cacheKey, pepper, TimeSpan.FromHours(24));
            return pepper;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get pepper for tenant {TenantId}",
                tenantId);

            // Use a temporary pepper (in production would throw)
            var tempPepper = Encoding.UTF8.GetBytes($"temp-pepper-{tenantId}");
            _memoryCache.Set(cacheKey, tempPepper, TimeSpan.FromMinutes(5));
            return tempPepper;
        }
    }

    private string NormalizeName(string name)
    {
        // Remove diacritics, convert to lowercase, remove special chars
        var normalized = name.ToLowerInvariant();
        normalized = RemoveDiacritics(normalized);
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    private string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Simplified phonetic encoding (in production would use Double Metaphone)
    private string GetSimplePhonetic(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2)
        {
            return string.Empty;
        }

        var phonetic = word.ToUpperInvariant();

        // Very simple phonetic rules
        phonetic = phonetic.Replace("PH", "F");
        phonetic = phonetic.Replace("GH", "G");
        phonetic = phonetic.Replace("CK", "K");
        phonetic = phonetic.Replace("SCH", "SK");
        phonetic = phonetic.Replace("TH", "T");

        // Remove silent letters at end
        if (phonetic.Length > 1 && phonetic.EndsWith('E'))
        {
            phonetic = phonetic.Substring(0, phonetic.Length - 1);
        }

        // Common soundalike replacements
        phonetic = Regex.Replace(phonetic, @"[AEIOU]", "");
        phonetic = phonetic.Replace("C", "K");
        phonetic = phonetic.Replace("Q", "K");
        phonetic = phonetic.Replace("X", "KS");
        phonetic = phonetic.Replace("Y", "I");
        phonetic = phonetic.Replace("Z", "S");

        return phonetic.Length >= 2 ? phonetic : string.Empty;
    }
}