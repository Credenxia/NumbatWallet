using System.Net;

namespace NumbatWallet.Web.Api.Security;

/// <summary>
/// Resolves the per-client partition key for the global rate limiter.
///
/// Behind the shared NGINX ingress the TCP peer (Connection.RemoteIpAddress) is the
/// ingress-controller pod, so keying on it collapses EVERY client into one shared
/// bucket (perf baseline 2026-06-12: 99.6% of a 100-VU ramp rejected 429). Prefer the
/// first X-Forwarded-For hop — the original client as recorded by the ingress — and
/// fall back to RemoteIpAddress when the header is absent, unparseable, or oversized.
///
/// When the ForwardedHeaders middleware has already consumed the header (it rewrites
/// RemoteIpAddress to the client and removes the processed entry), the fallback path
/// is taken and still yields the client address, so both pipelines partition correctly.
///
/// SECURITY TRADEOFF (documented, accepted for this topology): the X-Forwarded-For
/// value is trusted without a KnownProxies allow-list. This is safe in-cluster because
/// (a) the pod Service is only reachable from the ingress controller via NetworkPolicy,
/// and (b) the NGINX ingress (use-forwarded-headers=false, the default) REPLACES any
/// client-supplied X-Forwarded-For with the real peer address — an external caller
/// cannot choose its own bucket. If the API is ever exposed without such a proxy,
/// reintroduce KnownNetworks/KnownProxies before trusting the header.
/// </summary>
public static class RateLimitPartitionKeyResolver
{
    public const string UnknownKey = "unknown";

    /// <summary>
    /// Bound the parse: a legitimate ingress writes one short entry; anything larger
    /// is hostile or malformed and falls back to the connection address.
    /// </summary>
    private const int MaxForwardedForLength = 256;

    public static string Resolve(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor) && forwardedFor.Length <= MaxForwardedForLength)
        {
            var separatorIndex = forwardedFor.IndexOf(',');
            var firstHop = (separatorIndex >= 0 ? forwardedFor[..separatorIndex] : forwardedFor).Trim();

            if (TryParseAddress(firstHop, out var address))
            {
                return address.ToString();
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? UnknownKey;
    }

    private static bool TryParseAddress(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out var parsed))
        {
            address = parsed;
            return true;
        }

        // Some proxies append a port ("203.0.113.7:55123" / "[2001:db8::1]:443").
        if (IPEndPoint.TryParse(value, out var endpoint))
        {
            address = endpoint.Address;
            return true;
        }

        address = IPAddress.None;
        return false;
    }
}
