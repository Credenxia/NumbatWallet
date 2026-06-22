using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration? _configuration;

    private const int PREFIX_LENGTH = 5;
    private const string PEPPER_SECRET_NAME = "search-pepper";

    // Deployment-wide pepper for DETERMINISTIC identifier tokens (email/phone). Deliberately
    // NOT tenant-scoped: these tokens back exact-match lookups on the login path, which must
    // produce identical tokens whether the row was written by the startup seeder (no ambient
    // tenant context) or by an authenticated request (tenant resolved from claims). Tenant
    // isolation is still enforced by the global tenant query filters / per-tenant databases.
    // Sourced from config 'Search:TokenPepper' (base64; local dev — stable across restarts)
    // or the Key Vault secret 'search-token-pepper' (created on first use).
    private const string IDENTIFIER_PEPPER_SECRET_NAME = "search-token-pepper";
    private const string IDENTIFIER_PEPPER_CONFIG_KEY = "Search:TokenPepper";
    private const string IDENTIFIER_PEPPER_CACHE_KEY = "search-token-pepper";

    public HmacSearchTokenService(
        IKeyVaultService keyVaultService,
        ICurrentTenantService currentTenantService,
        IMemoryCache memoryCache,
        ILogger<HmacSearchTokenService> logger,
        IConfiguration? configuration = null)
    {
        _keyVaultService = keyVaultService;
        _currentTenantService = currentTenantService;
        _memoryCache = memoryCache;
        _logger = logger;
        _configuration = configuration;
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
        var pepper = await GetIdentifierPepperAsync();

        return GenerateHmacToken(pepper, $"email:{normalized}");
    }

    public async Task<string?> GeneratePhoneTokenAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        // Normalize to digits only so "+61 400 000 000", "0061400000000" formatting variants
        // of the SAME stored value cannot diverge ("+" / spaces / dashes / parens stripped).
        var normalized = new string(phoneNumber.Where(char.IsAsciiDigit).ToArray());
        if (normalized.Length == 0)
        {
            return null;
        }

        var pepper = await GetIdentifierPepperAsync();
        return GenerateHmacToken(pepper, $"phone:{normalized}");
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

    /// <summary>
    /// Resolves the deployment-wide pepper for deterministic identifier tokens (email/phone).
    /// Config 'Search:TokenPepper' (base64) takes precedence; otherwise the Key Vault secret
    /// 'search-token-pepper' is fetched (and created on first use). FAILS CLOSED: an unresolvable
    /// or invalid pepper throws rather than silently deriving a different key — a wrong pepper
    /// would make every stored token unmatchable and break login for all users.
    /// KEY ROTATION: rotating this pepper invalidates every stored email/phone search token;
    /// rotation requires recomputing the token columns for all rows (re-save persons) in the
    /// same deployment step.
    /// </summary>
    private async Task<byte[]> GetIdentifierPepperAsync()
    {
        if (_memoryCache.TryGetValue<byte[]>(IDENTIFIER_PEPPER_CACHE_KEY, out var cached))
        {
            return cached!;
        }

        byte[] pepper;

        var configured = _configuration?[IDENTIFIER_PEPPER_CONFIG_KEY];
        if (!string.IsNullOrEmpty(configured))
        {
            pepper = DecodePepper(configured, $"config '{IDENTIFIER_PEPPER_CONFIG_KEY}'");
        }
        else
        {
            var secret = await _keyVaultService.GetSecretAsync(IDENTIFIER_PEPPER_SECRET_NAME);
            if (!string.IsNullOrEmpty(secret))
            {
                pepper = DecodePepper(secret, $"Key Vault secret '{IDENTIFIER_PEPPER_SECRET_NAME}'");
            }
            else
            {
                pepper = RandomNumberGenerator.GetBytes(32);
                var stored = await _keyVaultService.SetSecretAsync(
                    IDENTIFIER_PEPPER_SECRET_NAME,
                    Convert.ToBase64String(pepper));
                if (!stored)
                {
                    throw new InvalidOperationException(
                        $"Failed to persist the search-token pepper to Key Vault secret '{IDENTIFIER_PEPPER_SECRET_NAME}'. " +
                        "Refusing to continue with an unpersisted pepper: tokens written now would be unmatchable after restart.");
                }

                _logger.LogInformation("Generated new deployment-wide search-token pepper");
            }
        }

        _memoryCache.Set(IDENTIFIER_PEPPER_CACHE_KEY, pepper, TimeSpan.FromHours(24));
        return pepper;
    }

    private static byte[] DecodePepper(string base64, string source)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length < 16)
            {
                throw new InvalidOperationException(
                    $"Search-token pepper from {source} is too short ({bytes.Length} bytes); at least 16 bytes required.");
            }
            return bytes;
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Search-token pepper from {source} is not valid base64.", ex);
        }
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
