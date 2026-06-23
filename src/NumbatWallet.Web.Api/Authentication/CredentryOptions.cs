using Microsoft.Extensions.Options;

namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Strongly-typed binding for the "Credentry" configuration section (see
/// <see cref="CredentryAuthenticationDefaults.ConfigurationSection"/>). Replaces the ad-hoc
/// <c>IConfiguration</c> string lookups so misconfiguration fails fast at startup (via the
/// paired <see cref="CredentryOptionsValidator"/> + <c>ValidateOnStart()</c>) rather than on the
/// first incoming token. Bound through the options pattern, so late-applied configuration
/// sources (tests, reload) still win — read it via <c>IOptionsMonitor&lt;CredentryOptions&gt;.CurrentValue</c>.
/// </summary>
public sealed class CredentryOptions
{
    /// <summary>Configuration section name ("Credentry").</summary>
    public const string SectionName = CredentryAuthenticationDefaults.ConfigurationSection;

    /// <summary>Master switch. When false the federation wiring is not registered at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>OIDC authority/issuer (the Credentry IdP). Required when <see cref="Enabled"/>.</summary>
    public string? Authority { get; set; }

    /// <summary>Expected token audience. Defaults to the contract value "numbatwallet-api".</summary>
    public string Audience { get; set; } = "numbatwallet-api";

    /// <summary>Required <c>product</c> claim. Defaults to "NUMBATWALLET".</summary>
    public string RequiredProduct { get; set; } = "NUMBATWALLET";

    /// <summary>
    /// Whether OIDC metadata retrieval requires HTTPS. Defaults true; set false only for a
    /// loopback dev IdP (http://localhost:5144).
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Credentry tenant GUID → NumbatWallet tenant GUID translation table. The two systems own
    /// different tenant GUIDs (deliberate), so every accepted tenant must have an entry here.
    /// An unmapped Credentry tenant FAILS CLOSED (no tenant_id claim → EF filters match nothing).
    /// Case-insensitive keys so token casing/format differences don't break the lookup.
    /// </summary>
    public Dictionary<string, string> TenantMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Fail-fast validation for <see cref="CredentryOptions"/>, run at host start
/// (<c>ValidateOnStart()</c>). Environment-agnostic by design: the only HTTP exception is a
/// loopback authority (the dev/stub IdP), so the rules protect every non-local deployment
/// without coupling to <c>IHostEnvironment</c> (which keeps the existing federation tests —
/// which run a stub IdP on 127.0.0.1 — green).
/// </summary>
public sealed class CredentryOptionsValidator : IValidateOptions<CredentryOptions>
{
    public ValidateOptionsResult Validate(string? name, CredentryOptions options)
    {
        // Disabled federation is always valid — nothing is wired.
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        // Authority: present + absolute URI.
        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            failures.Add("Credentry:Authority is required when Credentry:Enabled is true.");
        }
        else if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out var authorityUri))
        {
            failures.Add($"Credentry:Authority ('{options.Authority}') must be an absolute URI.");
        }
        else if (!string.Equals(authorityUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            // Reject file://, ftp://, etc. (a bare "/path" parses as file:// on Unix).
            failures.Add($"Credentry:Authority ('{options.Authority}') must be an http or https URL.");
        }
        else if (!authorityUri.IsLoopback &&
                 !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            // Non-loopback authorities must be HTTPS — federation tokens are bearer credentials.
            failures.Add($"Credentry:Authority ('{options.Authority}') must use HTTPS unless it is a loopback address.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Credentry:Audience must not be empty when Credentry:Enabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.RequiredProduct))
        {
            failures.Add("Credentry:RequiredProduct must not be empty when Credentry:Enabled is true.");
        }

        // TenantMap: at least one real mapping, and every real key/value a parseable GUID. An
        // enabled federation with no tenant map can never resolve a tenant, so it's a misconfig.
        // Keys beginning with '_' are treated as inline documentation (a common JSON-config
        // comment convention) and ignored — they never match a GUID tenant lookup at runtime.
        var realEntries = options.TenantMap
            .Where(kvp => !kvp.Key.StartsWith('_'))
            .ToList();

        if (realEntries.Count == 0)
        {
            failures.Add(
                "Credentry:TenantMap must contain at least one Credentry-tenant → NumbatWallet-tenant " +
                "mapping when Credentry:Enabled is true (otherwise every token fails closed with no tenant).");
        }
        else
        {
            foreach (var (key, value) in realEntries)
            {
                if (!Guid.TryParse(key, out _))
                {
                    failures.Add($"Credentry:TenantMap key '{key}' is not a valid GUID.");
                }

                if (!Guid.TryParse(value, out _))
                {
                    failures.Add($"Credentry:TenantMap['{key}'] value '{value}' is not a valid GUID.");
                }
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>DI helpers for binding + validating <see cref="CredentryOptions"/>.</summary>
public static class CredentryOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds the "Credentry" section to <see cref="CredentryOptions"/> and registers the
    /// fail-fast validator (runs at host start). Safe to call unconditionally — when the
    /// section has <c>Enabled=false</c> the validator passes without further checks.
    /// </summary>
    public static IServiceCollection AddCredentryOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<CredentryOptions>, CredentryOptionsValidator>();
        services
            .AddOptions<CredentryOptions>()
            .Bind(configuration.GetSection(CredentryOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
