namespace Zmg.Domain;

/// <summary>
/// The open-redirect guard for <c>?returnUrl=</c> (v2.10/M54).
///
/// Sign-in takes a destination from the query string and sends the browser there afterwards. Left
/// unchecked that is a textbook open redirect: an attacker sends
/// <c>/api/auth/login?returnUrl=https://evil.example</c>, the victim sees a genuine
/// <c>app.zionmusicgroup.com</c> link and a genuine Google consent screen, and lands somewhere else
/// still believing they are on our site. It is also a phishing primitive worth more than it looks,
/// because the first two hops are real.
///
/// So the rule is an allow-list, not a block-list: a value is either a rooted same-origin path or it
/// is replaced with "/". Three lines of string checks with three separate ways to get it wrong, which
/// is exactly why it is pure, isolated and tested rather than inlined at the call site.
/// </summary>
public static class Redirects
{
    /// <summary>Where anything rejected — or absent — goes.</summary>
    public const string Default = "/";

    /// <summary>
    /// Returns <paramref name="returnUrl"/> if it is a safe same-origin path, else <see cref="Default"/>.
    /// Never throws and never returns null, so call sites can use the result directly.
    /// </summary>
    public static string SafeLocalPath(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return Default;

        // Control characters are checked on the RAW input, before trimming, and this order is
        // load-bearing. CR/LF are both whitespace and control characters, so trimming first would
        // silently *repair* a trailing "\r" while still rejecting an embedded one — the same input
        // class handled two different ways depending on where the byte landed. A legitimate return
        // path never contains one, so reject outright rather than sanitize: failing closed on input
        // that should not exist beats quietly fixing it and hiding whatever produced it.
        foreach (var c in returnUrl)
        {
            if (char.IsControl(c)) return Default;
        }

        var path = returnUrl.Trim();

        // Must be rooted. Anything else is absolute ("https://evil.example") or origin-relative
        // ("evil.example"), and both resolve off-site.
        if (path[0] != '/') return Default;

        // "//host" and "/\host" are protocol-relative: the browser reads them as a different origin
        // despite the leading slash. The backslash form counts because browsers normalize "\" to "/",
        // and it is the variant that slips past a naive StartsWith("//") check.
        if (path.Length > 1 && (path[1] == '/' || path[1] == '\\')) return Default;

        return path;
    }
}
