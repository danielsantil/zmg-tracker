namespace Zmg.Api.Logging;

/// <summary>
/// Every event this app logs on purpose, in one place (v2.10/M57).
///
/// Source-generated <c>[LoggerMessage]</c> methods rather than <c>logger.LogInformation("…")</c> call
/// sites, for two reasons. They carry a <b>named, numbered event id</b>, so a KQL query filters on
/// <c>EventId == 1001</c> instead of matching message text that a later edit will quietly break. And
/// the generator emits the formatting inline, skipping the boxing and the format-string parse when the
/// level is disabled — which matters most for the per-request events.
///
/// The numbering is the grouping: <b>1000</b> authentication, <b>2000</b> uploads, <b>3000</b>
/// requests. Ids are permanent identifiers, same rule as the message codes and the integer enums:
/// queries and any future alert are written against them, so renumber nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Never logged, and this is a rule rather than a preference:</b> the session cookie's protected
/// value, the Google client secret, tokens of any kind, the connection string, R2 keys, and query
/// strings. Paths are logged without their query — ACA's own HTTP log documentation warns that a path
/// can carry secrets when clients put them there; ours don't, and logging without the query is what
/// keeps that true.
/// </para>
/// <para>
/// <b>The email is the one deliberate exception to "no user attribution".</b> Business writes record
/// nothing about who made them. Auth events do, because a failed-login spike is otherwise
/// unactionable — you cannot tell a partner fat-fingering their address from someone probing the door.
/// </para>
/// </remarks>
internal static partial class Log
{
    // ---- 1000 · authentication ----

    /// <summary>
    /// The session id here is the <c>AuthSession</c> row key, which is what makes the line useful:
    /// revoking that session is a <c>DELETE</c> against exactly this id. It is not a bearer token —
    /// the browser holds it only inside a Data-Protection-encrypted cookie, and the key ring lives in
    /// Postgres — so it cannot be replayed by someone reading the logs.
    /// </summary>
    [LoggerMessage(EventId = 1000, EventName = "auth.login.ok", Level = LogLevel.Information,
        Message = "auth.login.ok {Email} session {SessionId}")]
    public static partial void AuthLoginOk(ILogger logger, string email, string sessionId);

    /// <summary>
    /// <paramref name="reason"/> is <c>not_listed</c>, <c>disabled</c> or <c>email_unverified</c>. The
    /// browser is told none of that (one code for all three — see <c>AccessControl.NotAllowedCode</c>);
    /// the distinction exists only here, where the operator is already trusted.
    /// </summary>
    [LoggerMessage(EventId = 1001, EventName = "auth.login.denied", Level = LogLevel.Information,
        Message = "auth.login.denied {Email} {Reason}")]
    public static partial void AuthLoginDenied(ILogger logger, string email, string reason);

    [LoggerMessage(EventId = 1002, EventName = "auth.logout", Level = LogLevel.Information,
        Message = "auth.logout {Email}")]
    public static partial void AuthLogout(ILogger logger, string email);

    [LoggerMessage(EventId = 1003, EventName = "auth.session.swept", Level = LogLevel.Information,
        Message = "auth.session.swept {Count} expired session(s)")]
    public static partial void AuthSessionsSwept(ILogger logger, int count);

    // ---- 2000 · uploads ----

    /// <summary>
    /// One line per finished upload attempt. The byte pair is the point: M33 normalizes to a bounded
    /// WebP, and "4.3 MB in, 2.9 MB out" is exactly how the lossless-encoder bug would have announced
    /// itself in production, where the unit tests stayed green.
    /// </summary>
    [LoggerMessage(EventId = 2000, EventName = "cover.upload", Level = LogLevel.Information,
        Message = "cover.upload {Outcome} {SourceBytes}b -> {StoredBytes}b in {ElapsedMs} ms")]
    public static partial void CoverUpload(ILogger logger, string outcome, int sourceBytes, int storedBytes, long elapsedMs);

    /// <summary>
    /// An SSRF guard (M31) turned a fetch away. Worth an <c>Information</c> line even though it is a
    /// 400 to the caller: repeated blocks are the difference between a partner pasting a bad link and
    /// someone walking the guard looking for a hole.
    /// </summary>
    [LoggerMessage(EventId = 2001, EventName = "cover.fetch.blocked", Level = LogLevel.Information,
        Message = "cover.fetch.blocked {Reason} {Host}")]
    public static partial void CoverFetchBlocked(ILogger logger, string reason, string host);

    /// <summary>Host only, never the URL — the same reason the user is told nothing: it is a probe oracle.</summary>
    [LoggerMessage(EventId = 2002, EventName = "cover.fetch.failed", Level = LogLevel.Information,
        Message = "cover.fetch.failed {Host}")]
    public static partial void CoverFetchFailed(ILogger logger, Exception exception, string host);

    // ---- 3000 · requests ----

    [LoggerMessage(EventId = 3000, EventName = "request.slow", Level = LogLevel.Information,
        Message = "request.slow {Method} {Path} {StatusCode} in {ElapsedMs} ms")]
    public static partial void RequestSlow(ILogger logger, string method, string path, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 3001, EventName = "request.failed", Level = LogLevel.Information,
        Message = "request.failed {Method} {Path} {StatusCode} in {ElapsedMs} ms")]
    public static partial void RequestFailed(ILogger logger, string method, string path, int statusCode, long elapsedMs);

    /// <summary>
    /// The only <c>Error</c> in the set, and the only place an unhandled exception is written — once,
    /// with its stack, by <see cref="GlobalExceptionHandler"/>. Anything that catches and continues
    /// must not also log at this level, or a recovered blip reads like an outage.
    /// </summary>
    [LoggerMessage(EventId = 3002, EventName = "request.unhandled", Level = LogLevel.Error,
        Message = "request.unhandled {Method} {Path}")]
    public static partial void RequestUnhandled(ILogger logger, Exception exception, string method, string path);

    /// <summary>
    /// The request never reached an endpoint — too large a body, a malformed part. A client fault, so
    /// <c>Warning</c> and no stack: the type and the status are the whole story, and a stack trace per
    /// malformed request is how an error feed stops being read.
    /// </summary>
    [LoggerMessage(EventId = 3003, EventName = "request.rejected", Level = LogLevel.Warning,
        Message = "request.rejected {Method} {Path} {StatusCode} {Reason}")]
    public static partial void RequestRejected(ILogger logger, string method, string path, int statusCode, string reason);
}
