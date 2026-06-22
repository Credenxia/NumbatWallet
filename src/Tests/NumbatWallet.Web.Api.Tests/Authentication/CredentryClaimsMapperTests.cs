using FluentAssertions;
using NumbatWallet.Web.Api.Authentication;
using System.Security.Claims;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.Authentication;

public class CredentryClaimsMapperTests
{
    private const string CredentryTid = "6f1a2b3c-4d5e-6f70-8192-a3b4c5d6e7f8";
    private const string NumbatTenantId = "00000000-0000-0000-0000-000000000001";

    private static ClaimsIdentity CreateIdentity(params Claim[] claims) =>
        new(claims, authenticationType: "CredentryJwt");

    // --- Product gate -------------------------------------------------------

    [Fact]
    public void HasRequiredProduct_NumbatWalletProduct_ReturnsTrue()
    {
        var principal = new ClaimsPrincipal(CreateIdentity(new Claim("product", "NUMBATWALLET")));

        CredentryClaimsMapper.HasRequiredProduct(principal).Should().BeTrue();
    }

    [Theory]
    [InlineData("CREDENTRY-PORTAL")]
    [InlineData("numbatwallet")] // case-sensitive by contract
    [InlineData("")]
    public void HasRequiredProduct_WrongProduct_ReturnsFalse(string product)
    {
        var principal = new ClaimsPrincipal(CreateIdentity(new Claim("product", product)));

        CredentryClaimsMapper.HasRequiredProduct(principal).Should().BeFalse();
    }

    [Fact]
    public void HasRequiredProduct_MissingProductClaim_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(CreateIdentity());

        CredentryClaimsMapper.HasRequiredProduct(principal).Should().BeFalse();
    }

    // --- NW.* role mapping --------------------------------------------------

    [Fact]
    public void MapNwRoles_NwPrefixedRoles_StripsPrefixIntoRoleClaims()
    {
        var identity = CreateIdentity(
            new Claim("role", "NW.Admin"),
            new Claim("role", "NW.Officer"));

        var mapped = CredentryClaimsMapper.MapNwRoles(identity);

        mapped.Should().Be(2);
        identity.HasClaim(ClaimTypes.Role, "Admin").Should().BeTrue();
        identity.HasClaim(ClaimTypes.Role, "Officer").Should().BeTrue();
    }

    [Fact]
    public void MapNwRoles_CredentryPlatformRoles_AreNotMapped()
    {
        // SuperAdmin/TenantAdmin etc. are Credentry PLATFORM roles (tenant-scoped, cross-product);
        // only the NW.* product convention maps into NumbatWallet authorization.
        var identity = CreateIdentity(
            new Claim("role", "SuperAdmin"),
            new Claim("role", "TenantAdmin"),
            new Claim("role", "NW.Citizen"));

        var mapped = CredentryClaimsMapper.MapNwRoles(identity);

        mapped.Should().Be(1);
        identity.HasClaim(ClaimTypes.Role, "Citizen").Should().BeTrue();
        identity.HasClaim(ClaimTypes.Role, "SuperAdmin").Should().BeFalse();
        identity.HasClaim(ClaimTypes.Role, "TenantAdmin").Should().BeFalse();
    }

    [Fact]
    public void MapNwRoles_DuplicateAndEmptySuffixRoles_AreDeduplicatedAndSkipped()
    {
        var identity = CreateIdentity(
            new Claim("role", "NW.Admin"),
            new Claim("role", "NW.Admin"),
            new Claim("role", "NW."));

        var mapped = CredentryClaimsMapper.MapNwRoles(identity);

        mapped.Should().Be(1);
        identity.FindAll(ClaimTypes.Role).Should().ContainSingle(c => c.Value == "Admin");
    }

    [Fact]
    public void MapNwRoles_NoRoleClaims_MapsNothing()
    {
        var identity = CreateIdentity(new Claim("sub", "numbatwallet-m2m"));

        CredentryClaimsMapper.MapNwRoles(identity).Should().Be(0);
        identity.FindAll(ClaimTypes.Role).Should().BeEmpty();
    }

    // --- Tenant resolution ----------------------------------------------------

    [Fact]
    public void GetCredentryTenantId_UserToken_ReadsTid()
    {
        var principal = new ClaimsPrincipal(CreateIdentity(new Claim("tid", CredentryTid)));

        CredentryClaimsMapper.GetCredentryTenantId(principal).Should().Be(CredentryTid);
    }

    [Fact]
    public void GetCredentryTenantId_M2MToken_ReadsTenant()
    {
        var principal = new ClaimsPrincipal(CreateIdentity(new Claim("tenant", CredentryTid)));

        CredentryClaimsMapper.GetCredentryTenantId(principal).Should().Be(CredentryTid);
    }

    [Fact]
    public void GetCredentryTenantId_NeitherClaim_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(CreateIdentity());

        CredentryClaimsMapper.GetCredentryTenantId(principal).Should().BeNull();
    }

    [Fact]
    public void TryMapTenant_MappedTenant_AddsTenantIdClaim()
    {
        var identity = CreateIdentity();

        var result = CredentryClaimsMapper.TryMapTenant(
            identity, CredentryTid, tid => tid == CredentryTid ? NumbatTenantId : null);

        result.Should().BeTrue();
        identity.FindFirst("tenant_id")!.Value.Should().Be(NumbatTenantId);
    }

    [Fact]
    public void TryMapTenant_TenantIdIsNormalizedBeforeLookup()
    {
        // Token may carry the GUID upper-cased or braced; map keys are canonical "D" lowercase.
        var identity = CreateIdentity();

        var result = CredentryClaimsMapper.TryMapTenant(
            identity, CredentryTid.ToUpperInvariant(), tid => tid == CredentryTid ? NumbatTenantId : null);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryMapTenant_UnmappedTenant_FailsClosedWithoutClaim()
    {
        var identity = CreateIdentity();

        var result = CredentryClaimsMapper.TryMapTenant(identity, CredentryTid, _ => null);

        result.Should().BeFalse();
        identity.FindFirst("tenant_id").Should().BeNull();
    }

    [Fact]
    public void TryMapTenant_MissingCredentryTenant_FailsClosedWithoutClaim()
    {
        var identity = CreateIdentity();

        var result = CredentryClaimsMapper.TryMapTenant(identity, null, _ => NumbatTenantId);

        result.Should().BeFalse();
        identity.FindFirst("tenant_id").Should().BeNull();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public void TryMapTenant_InvalidCredentryTenant_FailsClosed(string tid)
    {
        var identity = CreateIdentity();

        CredentryClaimsMapper.TryMapTenant(identity, tid, _ => NumbatTenantId).Should().BeFalse();
        identity.FindFirst("tenant_id").Should().BeNull();
    }

    [Fact]
    public void TryMapTenant_MapEntryNotAGuid_FailsClosed()
    {
        var identity = CreateIdentity();

        CredentryClaimsMapper.TryMapTenant(identity, CredentryTid, _ => "garbage").Should().BeFalse();
        identity.FindFirst("tenant_id").Should().BeNull();
    }

    [Fact]
    public void TryMapTenant_ExistingTenantClaim_IsNotDuplicated()
    {
        var identity = CreateIdentity(new Claim("tenant_id", NumbatTenantId));

        CredentryClaimsMapper.TryMapTenant(identity, CredentryTid, _ => NumbatTenantId).Should().BeTrue();
        identity.FindAll("tenant_id").Should().HaveCount(1);
    }
}
