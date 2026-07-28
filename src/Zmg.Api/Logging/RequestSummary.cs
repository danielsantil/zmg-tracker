namespace Zmg.Api.Logging;

/// <summary>What, if anything, a finished request is worth saying out loud.</summary>
internal enum RequestOutcome
{
    /// <summary>Nothing to report — the ingress log already has this request.</summary>
    Silent,
    Slow,
    Failed,
}

/// <summary>
/// Decides whether a finished request deserves a log line (v2.10/M57).
///
/// The happy path stays silent on purpose: ACA's ingress logs already record method, path, status and
/// duration for *every* request, better than the app can, so an app-side line per request would be
/// pure duplicate ingestion. What ingress cannot tell you is why — so the app speaks only for the
/// requests worth looking at, and its line joins the ingress record on the request id.
///
/// Pure and separate from the middleware for the usual reason: it is a two-line rule with several ways
/// to get it subtly wrong, and the wrong versions are invisible in production (too quiet to notice,
/// or too loud to read).
/// </summary>
internal static class RequestSummary
{
    /// <summary>Overridable via <c>Logging:SlowRequestMs</c>.</summary>
    public const int DefaultSlowMs = 1000;

    /// <summary>
    /// The SPA's auth probe. Its 401 is not a failure — it is the answer "signed out", and the SPA
    /// asks on every cold visit before it can render the login gate. Logging it as a failed request
    /// would mean the most common line in the file is one that never needs acting on, which is how a
    /// log stops being read.
    /// </summary>
    public const string AuthProbePath = "/api/auth/me";

    public static RequestOutcome Classify(string path, int statusCode, long elapsedMs, long slowMs)
    {
        if (statusCode >= 400 && !IsExpectedStatus(path, statusCode)) return RequestOutcome.Failed;
        if (elapsedMs >= slowMs) return RequestOutcome.Slow;
        return RequestOutcome.Silent;
    }

    private static bool IsExpectedStatus(string path, int statusCode) =>
        statusCode == StatusCodes.Status401Unauthorized
        && string.Equals(path, AuthProbePath, StringComparison.OrdinalIgnoreCase);
}
