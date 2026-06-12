using System.Net;
using Microsoft.AspNetCore.Http;
using NumbatWallet.Web.Api.Security;

namespace NumbatWallet.Web.Api.Tests.Security;

/// <summary>
/// Tests for the proxy-aware rate-limit partition key. Regression guard for the AKS
/// perf defect where the global limiter partitioned on Connection.RemoteIpAddress,
/// which behind the NGINX ingress is always the ingress pod — collapsing every client
/// into a single 100 req/min bucket (99.6% 429s under load).
/// </summary>
public class RateLimitPartitionKeyResolverTests
{
    private static DefaultHttpContext CreateContext(string? remoteIp = null, string? forwardedFor = null)
    {
        var context = new DefaultHttpContext();
        if (remoteIp is not null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }
        return context;
    }

    [Fact]
    public void Resolve_WithNoForwardedHeader_UsesRemoteIpAddress()
    {
        var context = CreateContext(remoteIp: "10.224.0.42");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("10.224.0.42");
    }

    [Fact]
    public void Resolve_WithNoForwardedHeaderAndNoRemoteIp_ReturnsUnknown()
    {
        var context = CreateContext();

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be(RateLimitPartitionKeyResolver.UnknownKey);
    }

    [Fact]
    public void Resolve_WithSingleForwardedHop_UsesForwardedAddress()
    {
        // Typical NGINX-ingress shape: XFF carries the client, RemoteIpAddress is the ingress pod.
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "203.0.113.7");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Resolve_WithMultipleForwardedHops_UsesFirstHop()
    {
        // First hop = the original client; later entries are intermediate proxies.
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "203.0.113.7, 198.51.100.10, 10.224.0.5");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Resolve_TrimsWhitespaceAroundForwardedHop()
    {
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "  203.0.113.7 , 10.0.0.1");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Resolve_WithForwardedHopIncludingPort_UsesAddressOnly()
    {
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "203.0.113.7:55123");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void Resolve_WithIpv6ForwardedHop_UsesForwardedAddress()
    {
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "2001:db8::1");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("2001:db8::1");
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("unknown")] // RFC 7239 obfuscated identifier
    [InlineData("<script>alert(1)</script>")]
    public void Resolve_WithUnparseableForwardedHop_FallsBackToRemoteIp(string forwardedFor)
    {
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: forwardedFor);

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("10.224.0.42");
    }

    [Fact]
    public void Resolve_WithAbsurdlyLongForwardedHeader_FallsBackToRemoteIp()
    {
        // Bound the parse: a hostile client must not make us process megabyte headers.
        var huge = string.Join(",", Enumerable.Repeat("203.0.113.7", 200));
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: huge);

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("10.224.0.42");
    }

    [Fact]
    public void Resolve_WithEmptyForwardedHeader_FallsBackToRemoteIp()
    {
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("10.224.0.42");
    }

    [Fact]
    public void Resolve_NormalisesParsedAddress()
    {
        // Leading zeros / non-canonical forms partition into the same bucket.
        var context = CreateContext(remoteIp: "10.224.0.42", forwardedFor: "2001:0db8:0000:0000:0000:0000:0000:0001");

        RateLimitPartitionKeyResolver.Resolve(context).Should().Be("2001:db8::1");
    }
}
