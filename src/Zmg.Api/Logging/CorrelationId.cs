namespace Zmg.Api.Logging;

/// <summary>
/// The request id that joins an app log line to the ingress record for the same request (v2.10/M57).
///
/// ACA's Envoy ingress sets <c>x-request-id</c> on every inbound request and records it in
/// <c>ContainerAppHTTPLogs</c>, so adopting *that* value rather than minting our own is what makes the
/// two sides joinable — and it is also what a partner can read off an error screen and quote.
///
/// It arrives as a client-controlled header, though, which is why it is normalized rather than
/// trusted. An unbounded, arbitrary-byte string echoed into a response header and into every log line
/// of the request is a header-injection and log-forging primitive for free: a CR/LF splits the header,
/// and a crafted value can fake a whole extra JSON log entry. So this is an allow-list — a short,
/// boring token or a fresh one — in the same spirit as <c>Redirects.SafeLocalPath</c>.
/// </summary>
internal static class CorrelationId
{
    /// <summary>Envoy's own header name, lower-cased as it sends it.</summary>
    public const string HeaderName = "x-request-id";

    /// <summary>
    /// Long enough for a UUID (Envoy's format, 36 chars) with room to spare, short enough that the
    /// value can't bloat every line of a request's logs.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// The supplied id if it is safe to echo and log, else a fresh one. Never throws, never returns
    /// null or empty, so callers can use the result directly.
    /// </summary>
    public static string Normalize(string? supplied) =>
        IsAcceptable(supplied) ? supplied! : Generate();

    /// <summary>A new id, in the same shape the session keys use — hex, no separators.</summary>
    public static string Generate() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Allow-list: non-empty, bounded, and drawn from the characters an id is actually made of.
    /// Control characters are excluded by construction, so no separate CR/LF check is needed.
    /// </summary>
    private static bool IsAcceptable(string? supplied)
    {
        if (string.IsNullOrEmpty(supplied) || supplied.Length > MaxLength) return false;

        foreach (var c in supplied)
        {
            var allowed = (c is >= 'a' and <= 'z')
                || (c is >= 'A' and <= 'Z')
                || (c is >= '0' and <= '9')
                || c is '-' or '_' or '.';
            if (!allowed) return false;
        }

        return true;
    }
}
