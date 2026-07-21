using System.Net;
using System.Net.Sockets;

namespace Zmg.Domain;

/// <summary>
/// The rules for accepting a cover image (M31): which types are allowed, how big, what the bytes must
/// actually be, and which URLs the server is willing to fetch. Pure and I/O-free on purpose — the
/// SSRF guards are the security boundary for <c>POST /api/uploads/cover-from-url</c>, so they belong
/// where they can be unit-tested exhaustively rather than inside a service that needs a socket.
/// </summary>
public static class CoverImage
{
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>How many <c>Location</c> hops the fetch will follow before giving up.</summary>
    public const int MaxRedirects = 3;

    public const string PngContentType = "image/png";
    public const string JpegContentType = "image/jpeg";
    public const string WebpContentType = "image/webp";

    public static readonly IReadOnlyList<string> AllowedContentTypes =
        new[] { PngContentType, JpegContentType, WebpContentType };

    public const string InvalidTypeMessage = "Cover must be a PNG, JPEG or WebP image.";
    public const string TooLargeMessage = "Cover image must be 5 MB or smaller.";
    public const string InvalidUrlMessage = "Enter an http(s) image URL.";
    public const string BlockedUrlMessage = "That URL can't be fetched.";
    public const string UnreachableUrlMessage = "Couldn't download an image from that URL.";

    /// <summary>
    /// True for a declared content type we accept. Tolerates parameters and casing
    /// (<c>Image/PNG; charset=binary</c>), since browsers and remote servers both send them.
    /// </summary>
    public static bool IsAllowedContentType(string? contentType) =>
        Normalize(contentType) is { } type && AllowedContentTypes.Contains(type);

    /// <summary>Lowercased media type without parameters, or null when there isn't one.</summary>
    public static string? Normalize(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return null;
        var value = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return value.Length == 0 ? null : value;
    }

    public static string ExtensionFor(string contentType) => Normalize(contentType) switch
    {
        PngContentType => ".png",
        JpegContentType => ".jpg",
        WebpContentType => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Not an allowed cover type."),
    };

    /// <summary>
    /// The real type of the bytes, from their magic number — the declared content type is attacker-
    /// controlled on both ingest paths, so this is what actually decides. Null when the bytes are
    /// not one of the allowed formats.
    /// </summary>
    public static string? SniffContentType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
        {
            return PngContentType;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return JpegContentType;
        }

        // RIFF????WEBP
        if (content.Length >= 12 &&
            content[0] == (byte)'R' && content[1] == (byte)'I' && content[2] == (byte)'F' && content[3] == (byte)'F' &&
            content[8] == (byte)'W' && content[9] == (byte)'E' && content[10] == (byte)'B' && content[11] == (byte)'P')
        {
            return WebpContentType;
        }

        return null;
    }

    /// <summary>
    /// A syntactically fetchable cover URL: absolute, http(s) only. Rejects the schemes that turn a
    /// server-side fetch into a file read or a protocol probe (<c>file:</c>, <c>gopher:</c>, …).
    /// </summary>
    public static bool IsFetchableUrl(string? url, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return false;

        uri = parsed;
        return true;
    }

    /// <summary>
    /// True for an address the server must never dial on a user's behalf: loopback, the RFC1918 /
    /// CGNAT ranges, link-local (which covers the 169.254.169.254 cloud metadata endpoint), unique
    /// local IPv6, multicast and the unspecified address. IPv4-mapped IPv6 is unwrapped first so
    /// <c>::ffff:127.0.0.1</c> can't sneak past the IPv4 checks.
    /// </summary>
    public static bool IsBlockedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)) return true;
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 0                                      // "this network"
                || b[0] == 10                                     // 10.0.0.0/8
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)      // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                   // 192.168.0.0/16
                || (b[0] == 169 && b[1] == 254)                   // 169.254.0.0/16 (incl. cloud metadata)
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)     // 100.64.0.0/10 CGNAT
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)        // 192.0.0.0/24 IETF protocol assignments
                || b[0] >= 224;                                   // multicast + reserved
        }

        return address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6Multicast
            || address.IsIPv6UniqueLocal;
    }

    /// <summary>True when every resolved address is safe to dial (and there is at least one).</summary>
    public static bool AreAllAddressesAllowed(IEnumerable<IPAddress> addresses)
    {
        var any = false;
        foreach (var address in addresses)
        {
            any = true;
            if (IsBlockedAddress(address)) return false;
        }
        return any;
    }

    /// <summary>The object key a stored cover gets. Random name — never the caller's filename.</summary>
    public static string KeyFor(Guid id, string contentType) => $"covers/{id:N}{ExtensionFor(contentType)}";
}
