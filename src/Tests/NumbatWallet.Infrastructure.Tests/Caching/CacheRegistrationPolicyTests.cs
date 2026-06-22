using NumbatWallet.Infrastructure.Caching;

namespace NumbatWallet.Infrastructure.Tests.Caching;

/// <summary>
/// Tests for the distributed-cache backend selection policy. Regression guard for the
/// AKS perf defect where an unconfigured deployment fell through to the base
/// appsettings "localhost:6379" default and every authenticated request stalled the
/// full 5000ms StackExchange.Redis syncTimeout on the token-blacklist lookup.
/// </summary>
public class CacheRegistrationPolicyTests
{
    // --- UseRedis: connection-string gating (any environment) ---

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void UseRedis_WithNullConnectionString_ReturnsFalse(string environment)
    {
        CacheRegistrationPolicy.UseRedis(environment, null).Should().BeFalse();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void UseRedis_WithEmptyConnectionString_ReturnsFalse(string environment)
    {
        CacheRegistrationPolicy.UseRedis(environment, string.Empty).Should().BeFalse();
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Production")]
    public void UseRedis_WithWhitespaceConnectionString_ReturnsFalse(string environment)
    {
        CacheRegistrationPolicy.UseRedis(environment, "   ").Should().BeFalse();
    }

    [Theory]
    [InlineData("Testing")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void UseRedis_WithConnectionStringInNonDevelopmentEnvironment_ReturnsTrue(string environment)
    {
        CacheRegistrationPolicy.UseRedis(environment, "redis-master.shared-services.svc.cluster.local:6379")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("DEVELOPMENT")]
    public void UseRedis_InDevelopment_ReturnsFalse_EvenWithConnectionString(string environment)
    {
        // Development deliberately stays on the in-memory cache: the local Aspire Redis
        // connection string (ssl=true against a non-TLS container) is unreliable for dev.
        CacheRegistrationPolicy.UseRedis(environment, "localhost:6379").Should().BeFalse();
    }

    [Fact]
    public void UseRedis_WithNullEnvironment_UsesConnectionStringOnly()
    {
        CacheRegistrationPolicy.UseRedis(null, "localhost:6379").Should().BeTrue();
        CacheRegistrationPolicy.UseRedis(null, null).Should().BeFalse();
    }

    // --- BuildRedisOptions: bounded fail-open timeouts ---

    [Fact]
    public void BuildRedisOptions_WithBareEndpoint_AppliesBoundedDefaults()
    {
        var options = CacheRegistrationPolicy.BuildRedisOptions("redis-host:6379");

        options.AbortOnConnectFail.Should().BeFalse("a dead Redis must fail open, not break startup/requests");
        options.ConnectTimeout.Should().Be(CacheRegistrationPolicy.DefaultConnectTimeoutMs);
        options.SyncTimeout.Should().Be(CacheRegistrationPolicy.DefaultSyncTimeoutMs);
    }

    [Fact]
    public void BuildRedisOptions_DefaultTimeouts_AreSubSecond()
    {
        // The token blacklist does an HMGET on EVERY authenticated request; a dead Redis
        // must degrade to ~sub-second fail-open, never the 5000ms library default.
        CacheRegistrationPolicy.DefaultConnectTimeoutMs.Should().BeLessThanOrEqualTo(1000);
        CacheRegistrationPolicy.DefaultSyncTimeoutMs.Should().BeLessThanOrEqualTo(1000);
    }

    [Fact]
    public void BuildRedisOptions_HonoursExplicitTimeoutsFromConnectionString()
    {
        var options = CacheRegistrationPolicy.BuildRedisOptions(
            "redis-host:6379,connectTimeout=2500,syncTimeout=1500");

        options.ConnectTimeout.Should().Be(2500);
        options.SyncTimeout.Should().Be(1500);
    }

    [Fact]
    public void BuildRedisOptions_ExplicitTimeoutDetection_IsCaseInsensitive()
    {
        var options = CacheRegistrationPolicy.BuildRedisOptions(
            "redis-host:6379,CONNECTTIMEOUT=3000,SyncTimeout=2000");

        options.ConnectTimeout.Should().Be(3000);
        options.SyncTimeout.Should().Be(2000);
    }

    [Fact]
    public void BuildRedisOptions_ForcesAbortConnectFalse_EvenWhenConnectionStringSetsTrue()
    {
        var options = CacheRegistrationPolicy.BuildRedisOptions("redis-host:6379,abortConnect=true");

        options.AbortOnConnectFail.Should().BeFalse();
    }

    [Fact]
    public void BuildRedisOptions_PreservesEndpoint()
    {
        var options = CacheRegistrationPolicy.BuildRedisOptions("redis-host:6380");

        options.EndPoints.Should().ContainSingle()
            .Which.ToString().Should().Contain("redis-host").And.Contain("6380");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildRedisOptions_WithMissingConnectionString_Throws(string? connectionString)
    {
        var act = () => CacheRegistrationPolicy.BuildRedisOptions(connectionString!);

        act.Should().Throw<ArgumentException>();
    }

    // --- CacheBackendSelection: startup-log payload ---

    [Fact]
    public void DescribeSelection_ForRedis_NamesRedisAndEndpoint_WithoutSecrets()
    {
        var selection = CacheRegistrationPolicy.DescribeSelection(
            "Testing", "redis-host:6379,password=SuperSecret123");

        selection.UsesRedis.Should().BeTrue();
        selection.Description.Should().Contain("Redis").And.Contain("redis-host:6379");
        selection.Description.Should().NotContain("SuperSecret123", "connection-string secrets must not be logged");
    }

    [Fact]
    public void DescribeSelection_WithoutConnectionString_SelectsInMemory()
    {
        var selection = CacheRegistrationPolicy.DescribeSelection("Testing", null);

        selection.UsesRedis.Should().BeFalse();
        selection.Description.Should().ContainEquivalentOf("in-memory");
        selection.Description.Should().ContainEquivalentOf("no Redis connection string");
    }

    [Fact]
    public void DescribeSelection_InDevelopmentWithConnectionString_SelectsInMemory()
    {
        var selection = CacheRegistrationPolicy.DescribeSelection("Development", "localhost:6379");

        selection.UsesRedis.Should().BeFalse();
        selection.Description.Should().ContainEquivalentOf("in-memory");
    }
}
