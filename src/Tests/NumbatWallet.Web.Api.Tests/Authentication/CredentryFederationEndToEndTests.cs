using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Authentication;

/// <summary>
/// End-to-end federation tests through the REAL NumbatWallet pipeline (full Program host:
/// selector scheme, CredentryJwt OIDC discovery + JWKS fetch, RS256 validation, claim
/// mapping, authorization policies), driven by a local stub IdP that issues tokens with the
/// federation contract's claims (06-NUMBATWALLET-FEDERATION-CONTRACT.md). This covers the
/// token shapes that cannot be minted against a real Credentry dev instance without
/// Credentry-side provisioning (tenant-bound M2M clients, NW.* role assignments).
/// Probe endpoints are deliberately DB-free:
///  - GET /api/v1/Authentication/validate  ([Authorize], echoes the mapped claims)
///  - GET /api/v1/wallet-templates         (AdminOnly policy; authorization runs pre-handler)
/// </summary>
public class CredentryFederationEndToEndTests :
    IClassFixture<StubCredentryIdpFixture>,
    IClassFixture<WebApplicationFactory<Program>>
{
    private const string CredentryTid = "34c1955b-485e-4aab-bf5c-08488f3e80b5";
    private const string UnmappedTid = "99999999-9999-9999-9999-999999999999";
    private const string NumbatTenantId = "00000000-0000-0000-0000-000000000001";
    private const string RoleClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private readonly StubCredentryIdpFixture _idp;
    private readonly WebApplicationFactory<Program> _factory;

    public CredentryFederationEndToEndTests(StubCredentryIdpFixture idp, WebApplicationFactory<Program> factory)
    {
        _idp = idp;
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Credentry:Enabled"] = "true",
                    ["Credentry:Authority"] = _idp.Issuer,
                    ["Credentry:RequireHttpsMetadata"] = "false",
                    [$"Credentry:TenantMap:{CredentryTid}"] = NumbatTenantId
                });
            });
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static HttpRequestMessage Get(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<Dictionary<string, List<string>>> ReadClaimsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var claims = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var claim in doc.RootElement.GetProperty("claims").EnumerateArray())
        {
            var type = claim.GetProperty("type").GetString()!;
            var value = claim.GetProperty("value").GetString()!;
            if (!claims.TryGetValue(type, out var values))
            {
                claims[type] = values = [];
            }

            values.Add(value);
        }

        return claims;
    }

    // --- M2M (client-credentials) shape --------------------------------------

    [Fact]
    public async Task M2MToken_TenantBoundWithNwAdmin_AuthenticatesWithMappedClaims()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),   // tenant-bound M2M claim
                new Claim("role", "NW.Admin")
            ]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await ReadClaimsAsync(response);
        claims[RoleClaimUri].Should().Contain("Admin", "NW.Admin must map to the Admin role");
        claims["tenant_id"].Should().ContainSingle().Which.Should().Be(NumbatTenantId,
            "the Credentry tenant must be translated via Credentry:TenantMap");
        claims["sub"].Should().ContainSingle().Which.Should().Be("numbatwallet-m2m",
            "the raw Credentry subject must stay honest/untouched");
        claims.Should().NotContainKey("person_id", "M2M tokens carry no email, so no person resolves");
    }

    [Fact]
    public async Task M2MToken_WithNwAdmin_SatisfiesAdminOnlyPolicy()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "NW.Admin")
            ]);

        var response = await client.SendAsync(Get("/api/v1/wallet-templates", token));

        // Authorization (AdminOnly) runs before the action; it must not be 401/403.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "an NW.Admin Credentry token must satisfy the AdminOnly policy");
    }

    [Fact]
    public async Task UserToken_WithTidAndNwRoles_MapsTenantAndRoles()
    {
        using var client = CreateClient();
        var credentryUserId = Guid.NewGuid().ToString();
        var token = _idp.MintToken(
            subject: credentryUserId, // Credentry USER id — NOT a NumbatWallet person Guid
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tid", CredentryTid),
                new Claim("email", "no-such-person@example.com"),
                new Claim("role", "NW.Officer"),
                new Claim("role", "TenantAdmin") // Credentry platform role — must NOT map
            ]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await ReadClaimsAsync(response);
        claims[RoleClaimUri].Should().Contain("Officer");
        claims[RoleClaimUri].Should().NotContain("TenantAdmin",
            "Credentry platform roles must not become NumbatWallet roles");
        claims["tenant_id"].Should().ContainSingle().Which.Should().Be(NumbatTenantId);
        claims["sub"].Should().ContainSingle().Which.Should().Be(credentryUserId);
        // Person resolution is fail-soft: no matching person -> no person_id claim, still 200.
        claims.Should().NotContainKey("person_id");
    }

    [Fact]
    public async Task Token_WithUnmappedTenant_FailsClosed_NoTenantClaim()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", UnmappedTid), // no Credentry:TenantMap entry
                new Claim("role", "NW.Admin")
            ]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "authentication still succeeds");
        var claims = await ReadClaimsAsync(response);
        claims.Should().NotContainKey("tenant_id",
            "an unmapped Credentry tenant must fail closed — no tenant claim, tenant filters match nothing");
    }

    [Fact]
    public async Task Token_WithoutNwRoles_IsAuthenticatedButForbiddenOnAdminPolicy()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "SuperAdmin") // platform role only — no NW.* mapping
            ]);

        var response = await client.SendAsync(Get("/api/v1/wallet-templates", token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "the token authenticates but Credentry platform roles must not satisfy NumbatWallet role policies");
    }

    // --- Rejections -----------------------------------------------------------

    [Fact]
    public async Task Token_ForOtherCredentryProduct_IsRejected()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "some-other-product",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "CREDENTRY-PORTAL"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "NW.Admin")
            ]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "tokens stamped for another Credentry product must fail the product gate");
    }

    [Fact]
    public async Task Token_WithWrongAudience_IsRejected()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "credentry-api", // missing the numbatwallet-api audience
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "NW.Admin")
            ]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredCredentryToken_IsRejected()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "NW.Admin")
            ],
            lifetime: TimeSpan.FromMinutes(-30));

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedCredentryToken_IsRejected()
    {
        using var client = CreateClient();
        var token = _idp.MintToken(
            subject: "numbatwallet-m2m",
            audience: "numbatwallet-api",
            claims:
            [
                new Claim("product", "NUMBATWALLET"),
                new Claim("tenant", CredentryTid),
                new Claim("role", "NW.Admin")
            ]);

        // Flip characters in the signature segment.
        var parts = token.Split('.');
        parts[2] = parts[2][..^2] + (parts[2][^2] == 'a' ? "bb" : "aa");

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", string.Join('.', parts)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Coexistence (migration period) ---------------------------------------

    [Fact]
    public async Task ExistingSelfIssuedHs256Token_StillAuthenticates_ThroughSelectorFallback()
    {
        // With federation enabled, a NumbatWallet self-issued HS256 token (the existing
        // dev/test scheme) must keep working — the selector probes the issuer and forwards
        // non-Credentry tokens to the pre-existing default scheme.
        using var client = CreateClient();
        var token = MintSelfIssuedHs256Token(roles: ["Admin"]);

        var response = await client.SendAsync(Get("/api/v1/Authentication/validate", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var claims = await ReadClaimsAsync(response);
        claims[RoleClaimUri].Should().Contain("Admin");
    }

    [Fact]
    public async Task MissingBearer_IsStillUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/Authentication/validate");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string MintSelfIssuedHs256Token(string[] roles)
    {
        // Mirrors HmacAccessTokenSigner: HS256 with the dev secret, full-URI claim names,
        // issuer "NumbatWallet" (≠ Credentry issuer → selector falls back to the Test scheme).
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisIsADevelopmentSecretKeyThatIs256BitsLong!!"));
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("tenant_id", NumbatTenantId)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwt = new JwtSecurityToken(
            issuer: "NumbatWallet",
            audience: "NumbatWallet.Api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

/// <summary>
/// Minimal local OIDC issuer: serves discovery + JWKS over loopback HTTP and mints RS256
/// access tokens, exactly the IdP shape (issuer/JWKS/claims) NumbatWallet validates
/// Credentry against.
/// </summary>
public sealed class StubCredentryIdpFixture : IAsyncLifetime
{
    private const string Kid = "stub-credentry-rs256";
    private WebApplication? _app;
    private readonly RSA _rsa = RSA.Create(2048);

    public string Issuer { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();

        app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer = Issuer,
            jwks_uri = $"{Issuer}/.well-known/jwks",
            token_endpoint = $"{Issuer}/connect/token",
            authorization_endpoint = $"{Issuer}/connect/authorize",
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        }));

        app.MapGet("/.well-known/jwks", () =>
        {
            var parameters = _rsa.ExportParameters(includePrivateParameters: false);
            return Results.Json(new
            {
                keys = new[]
                {
                    new
                    {
                        kty = "RSA",
                        use = "sig",
                        alg = "RS256",
                        kid = Kid,
                        n = Base64UrlEncoder.Encode(parameters.Modulus),
                        e = Base64UrlEncoder.Encode(parameters.Exponent)
                    }
                }
            });
        });

        await app.StartAsync();
        Issuer = app.Urls.First().TrimEnd('/');
        _app = app;
    }

    public string MintToken(string subject, string audience, IEnumerable<Claim> claims, TimeSpan? lifetime = null)
    {
        var allClaims = new List<Claim> { new("sub", subject) };
        allClaims.AddRange(claims);

        var signingKey = new RsaSecurityKey(_rsa) { KeyId = Kid };
        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime ?? TimeSpan.FromMinutes(10));

        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: allClaims,
            notBefore: expires < now ? expires.AddMinutes(-5) : now.AddMinutes(-1),
            expires: expires,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        _rsa.Dispose();
    }
}
