using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NumbatWallet.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NumbatWallet.Web.Api.Authentication;

/// <summary>
/// Credentry SSO federation (issuer: the Credentry platform IdP, OpenIddict/RS256).
/// NumbatWallet ACCEPTS Credentry-issued access tokens as an additional bearer scheme,
/// alongside (never replacing) the existing self-issued HS256/RS256 JWTs, the API-key
/// middleware and the Azure AD placeholder — migration-period coexistence per the
/// integrate-first/delete-later rule.
/// Contract: docs/integration/06-NUMBATWALLET-FEDERATION-CONTRACT.md in the Credentry repo.
/// - audience must be "numbatwallet-api" (added when the numbatwallet.api scope is granted)
/// - product claim must be NUMBATWALLET
/// - tenant comes from "tid" (user tokens) / "tenant" (tenant-bound M2M tokens) and is
///   translated to a NumbatWallet tenant id via the Credentry:TenantMap configuration table.
///   An unmapped Credentry tenant FAILS CLOSED: no tenant_id claim is added, so the EF
///   tenant query filters match nothing.
/// - roles use the NW.* convention (NW.Admin/NW.Officer/NW.Issuer/NW.Citizen); the prefix
///   is stripped into <see cref="ClaimTypes.Role"/> so existing policies (AdminOnly, …) apply.
/// </summary>
public static class CredentryAuthenticationDefaults
{
    /// <summary>Bearer scheme name for Credentry-issued access tokens.</summary>
    public const string AuthenticationScheme = "CredentryJwt";

    /// <summary>Policy (selector) scheme that routes between Credentry and the host's default scheme.</summary>
    public const string SelectorScheme = "CredentrySelector";

    /// <summary>Configuration section for the federation settings.</summary>
    public const string ConfigurationSection = "Credentry";
}

/// <summary>
/// Decides — WITHOUT validating the token — whether an incoming Authorization header carries
/// a Credentry-issued JWT, by decoding the (untrusted) "iss" claim and comparing it to the
/// configured Credentry issuer. Used only for SCHEME SELECTION; the selected scheme then
/// performs full signature/issuer/audience/lifetime validation. A spoofed "iss" merely routes
/// the token to the Credentry scheme where validation fails.
/// </summary>
public static class CredentryTokenInspector
{
    public static bool IsCredentryToken(string? authorizationHeader, string? credentryIssuer)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) || string.IsNullOrWhiteSpace(credentryIssuer))
        {
            return false;
        }

        const string bearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = authorizationHeader[bearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return false;
            }

            var issuer = handler.ReadJwtToken(token).Issuer;
            if (string.IsNullOrEmpty(issuer))
            {
                return false;
            }

            // The contract pins iss byte-identical to the Authority; tolerate only a
            // trailing-slash difference (a common configuration drift).
            return string.Equals(
                issuer.TrimEnd('/'),
                credentryIssuer.TrimEnd('/'),
                StringComparison.Ordinal);
        }
        catch
        {
            // Malformed token — not ours to select; let the default scheme reject it.
            return false;
        }
    }
}

/// <summary>
/// Pure claim-mapping rules for Credentry-issued tokens (unit-tested in isolation).
/// </summary>
public static class CredentryClaimsMapper
{
    public const string RolePrefix = "NW.";
    public const string ProductClaim = "product";
    public const string RequiredProduct = "NUMBATWALLET";
    public const string UserTenantClaim = "tid";       // user (authorization-code) tokens
    public const string ClientTenantClaim = "tenant";  // tenant-bound M2M (client-credentials) tokens
    public const string TenantIdClaim = "tenant_id";   // NumbatWallet's tenant claim
    public const string PersonIdClaim = "person_id";   // resolved NumbatWallet person (user tokens)

    /// <summary>
    /// True when the token's product claim matches the required product. Credentry stamps
    /// every token with a product claim; tokens for other Credentry products are rejected.
    /// </summary>
    public static bool HasRequiredProduct(ClaimsPrincipal principal, string requiredProduct = RequiredProduct)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var product = principal.FindFirst(ProductClaim)?.Value;
        return string.Equals(product, requiredProduct, StringComparison.Ordinal);
    }

    /// <summary>
    /// Maps Credentry "role" claims using the NW.* convention into <see cref="ClaimTypes.Role"/>
    /// (prefix stripped) so existing authorization policies match. Credentry roles are
    /// tenant-scoped, not product-scoped — non-NW.* roles (SuperAdmin, TenantAdmin, …) are
    /// deliberately NOT mapped: they are Credentry platform roles, not NumbatWallet roles.
    /// Returns the number of roles mapped.
    /// </summary>
    public static int MapNwRoles(ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var nwRoles = identity.Claims
            .Where(c => c.Type is "role" or ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => v.StartsWith(RolePrefix, StringComparison.Ordinal))
            .Select(v => v[RolePrefix.Length..])
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var role in nwRoles)
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return nwRoles.Count;
    }

    /// <summary>
    /// The Credentry tenant id carried by the token: "tid" for user tokens,
    /// "tenant" for tenant-bound M2M tokens. Null when the token carries neither
    /// (e.g. a platform-wide M2M client).
    /// </summary>
    public static string? GetCredentryTenantId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst(UserTenantClaim)?.Value
            ?? principal.FindFirst(ClientTenantClaim)?.Value;
    }

    /// <summary>
    /// Translates a Credentry tenant id to a NumbatWallet tenant id via the supplied lookup
    /// (the Credentry:TenantMap configuration table) and adds the tenant_id claim.
    /// FAILS CLOSED: when the Credentry tenant is missing or unmapped, NO tenant claim is
    /// added — the ambient tenant stays unresolved and EF tenant filters match nothing.
    /// The two systems own different tenant GUIDs, so a translation table (not GUID adoption)
    /// is the deliberate decision; Credentry is the source of truth for org identity.
    /// </summary>
    public static bool TryMapTenant(ClaimsIdentity identity, string? credentryTenantId, Func<string, string?> resolveTenant)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(resolveTenant);

        if (string.IsNullOrWhiteSpace(credentryTenantId) ||
            !Guid.TryParse(credentryTenantId, out var credentryTenantGuid))
        {
            return false;
        }

        // Normalised "D" format so map keys match regardless of casing/braces in the token.
        var mapped = resolveTenant(credentryTenantGuid.ToString("D"));
        if (string.IsNullOrWhiteSpace(mapped) || !Guid.TryParse(mapped, out var numbatTenantGuid))
        {
            return false;
        }

        if (identity.FindFirst(TenantIdClaim) is null)
        {
            identity.AddClaim(new Claim(TenantIdClaim, numbatTenantGuid.ToString("D")));
        }

        return true;
    }

    /// <summary>
    /// Best-effort NumbatWallet person resolution for Credentry USER tokens. The Credentry
    /// "sub" is a CREDENTRY user id, NOT a NumbatWallet person Guid — resolvers that parse
    /// the subject as a person Guid will never match it. Where a Person exists with the
    /// token's email (deterministic encrypted-email search-token lookup), the person Guid is
    /// added as person_id and as <see cref="ClaimTypes.NameIdentifier"/> (which
    /// ICurrentUserService prefers over the raw "sub"), so person-bound resolvers work.
    /// The raw "sub" claim is left untouched — it remains the honest Credentry identity.
    /// FAIL-SOFT: no person / lookup error → the token still authenticates, person-bound
    /// resolvers simply find nothing. Returns true when a person was resolved.
    /// </summary>
    public static async Task<bool> TryEnrichPersonIdentityAsync(
        ClaimsIdentity identity,
        string? email,
        IPersonService personService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(personService);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var person = await personService.GetByEmailAsync(email, cancellationToken);
            if (person is null)
            {
                logger.LogInformation(
                    "Credentry user {Email} has no matching NumbatWallet person — person-bound resolvers will not match",
                    email);
                return false;
            }

            if (identity.FindFirst(PersonIdClaim) is null)
            {
                identity.AddClaim(new Claim(PersonIdClaim, person.Id.ToString("D")));
            }

            if (identity.FindFirst(ClaimTypes.NameIdentifier) is null)
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, person.Id.ToString("D")));
            }

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail-soft: person resolution is best-effort identity enrichment, never an auth gate.
            logger.LogWarning(ex, "Credentry person-by-email resolution failed (continuing without person identity)");
            return false;
        }
    }
}

public static class CredentryAuthenticationExtensions
{
    /// <summary>
    /// Registers the "CredentryJwt" bearer scheme: standard OIDC-discovery JWT validation
    /// against the Credentry issuer (Authority → metadata → JWKS), audience numbatwallet-api,
    /// plus the federation claim mapping on TokenValidated.
    /// The options are bound via the options pattern (resolved lazily, on first use of the
    /// scheme) rather than captured at startup, so per-environment/test configuration sources
    /// applied late still win — the same pattern the host uses for the production JwtBearer.
    /// </summary>
    public static AuthenticationBuilder AddCredentryJwt(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.AddJwtBearer(CredentryAuthenticationDefaults.AuthenticationScheme);

        builder.Services
            .AddOptions<JwtBearerOptions>(CredentryAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptionsMonitor<CredentryOptions>>((options, credentryMonitor) =>
            {
                // Resolved on first use of the scheme (not captured at startup), so late-applied
                // configuration sources still win — and CredentryOptionsValidator has already
                // fail-fast-validated this at host start (ValidateOnStart).
                var credentry = credentryMonitor.CurrentValue;
                var authority = credentry.Authority;
                if (string.IsNullOrWhiteSpace(authority))
                {
                    throw new InvalidOperationException(
                        "Credentry federation is enabled (Credentry:Enabled=true) but Credentry:Authority is not configured.");
                }

                var audience = credentry.Audience;

                options.Authority = authority;
                // Dev runs the Credentry IdP on http://localhost:5144; production authorities are HTTPS.
                options.RequireHttpsMetadata = credentry.RequireHttpsMetadata;
                // Keep the raw OIDC claim names (sub, tid, role, …) — the mapping below is explicit.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority.TrimEnd('/'),
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = OnCredentryTokenValidatedAsync,
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("CredentryJwt");
                        logger.LogWarning("Credentry token validation failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        return builder;
    }

    /// <summary>
    /// Registers the policy (selector) scheme used as the DEFAULT scheme when federation is
    /// enabled: requests whose bearer token was issued by Credentry (untrusted "iss" probe)
    /// are forwarded to "CredentryJwt"; everything else keeps flowing to the host's existing
    /// default scheme ("Test" in Development/Testing, "Bearer" otherwise) — so all
    /// pre-federation auth paths behave exactly as before.
    /// </summary>
    public static AuthenticationBuilder AddCredentrySelector(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string fallbackScheme)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackScheme);

        builder.AddPolicyScheme(
            CredentryAuthenticationDefaults.SelectorScheme,
            "Credentry-aware scheme selector",
            options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // Resolved per request (cheap) rather than captured at startup, so
                    // late-applied configuration sources (tests, reload) win.
                    var authority = context.RequestServices
                        .GetRequiredService<IOptionsMonitor<CredentryOptions>>()
                        .CurrentValue.Authority;
                    var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    return CredentryTokenInspector.IsCredentryToken(authorizationHeader, authority)
                        ? CredentryAuthenticationDefaults.AuthenticationScheme
                        : fallbackScheme;
                };
            });

        return builder;
    }

    private static async Task OnCredentryTokenValidatedAsync(TokenValidatedContext context)
    {
        var credentry = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<CredentryOptions>>()
            .CurrentValue;
        var requiredProduct = credentry.RequiredProduct;
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("CredentryJwt");

        var principal = context.Principal;
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Credentry token produced no identity.");
            return;
        }

        // 1. Product gate: every Credentry token is stamped with a product claim; only
        //    NUMBATWALLET tokens may use this API.
        if (!CredentryClaimsMapper.HasRequiredProduct(principal, requiredProduct))
        {
            context.Fail($"Credentry token is not issued for product {requiredProduct}.");
            return;
        }

        // 2. Roles: NW.* convention → ClaimTypes.Role (prefix stripped).
        CredentryClaimsMapper.MapNwRoles(identity);

        // 3. Tenant translation (fail closed on unmapped tenants).
        var credentryTenantId = CredentryClaimsMapper.GetCredentryTenantId(principal);
        var tenantMapped = CredentryClaimsMapper.TryMapTenant(
            identity,
            credentryTenantId,
            tid => credentry.TenantMap.GetValueOrDefault(tid));
        if (!tenantMapped)
        {
            logger.LogWarning(
                "Credentry token for subject {Subject} carries tenant '{CredentryTenant}' with no Credentry:TenantMap entry — " +
                "no tenant_id claim added (fail-closed: tenant-scoped queries will match nothing)",
                principal.FindFirst("sub")?.Value, credentryTenantId ?? "<none>");
        }

        // 4. Person identity (USER tokens only — M2M tokens carry no email and act like the
        //    API-key service principal). See TryEnrichPersonIdentityAsync for the honesty
        //    notes on "sub" vs person Guid. KNOWN LIMITATION: email is the join key; a person
        //    only resolves when their NumbatWallet email matches the Credentry account email.
        //    Requires a mapped tenant: the person lookup goes through the tenant-filtered
        //    repository, so an unmapped tenant cannot (and must not) resolve anyone.
        var email = principal.FindFirst("email")?.Value;
        if (tenantMapped && !string.IsNullOrWhiteSpace(email))
        {
            // The ambient tenant is resolved from HttpContext.User's tenant_id claim, so the
            // principal must be visible before the lookup. (The auth middleware assigns it
            // again right after this event completes.) Scope: once per request — JWT bearer
            // authentication runs a single time per request, so no extra caching is needed.
            context.HttpContext.User = principal;

            var personService = context.HttpContext.RequestServices.GetRequiredService<IPersonService>();
            await CredentryClaimsMapper.TryEnrichPersonIdentityAsync(
                identity, email, personService, logger, context.HttpContext.RequestAborted);
        }
    }
}
