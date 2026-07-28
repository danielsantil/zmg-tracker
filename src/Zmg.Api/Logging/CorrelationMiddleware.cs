namespace Zmg.Api.Logging;

/// <summary>
/// Stamps one id onto everything a single request produces (v2.10/M57).
///
/// Three things happen to that id, and each is a different consumer:
/// <list type="bullet">
/// <item><description><b>A logging scope</b>, so every line written anywhere downstream carries
/// <c>RequestId</c> without a single call site having to pass it along.</description></item>
/// <item><description><b><see cref="HttpContext.TraceIdentifier"/></b>, so framework-emitted lines
/// use it too and the rest of the app has one place to read it from.</description></item>
/// <item><description><b>A response header</b>, so a partner staring at an error screen can quote
/// something that finds the request.</description></item>
/// </list>
///
/// It must be the outermost middleware: an exception logged by the handler further in is only useful
/// if it already carries the id.
/// </summary>
internal sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = CorrelationId.Normalize(context.Request.Headers[CorrelationId.HeaderName]);
        context.TraceIdentifier = requestId;

        // Deferred to OnStarting rather than written now, and that is load-bearing: the exception
        // handler middleware calls Response.Clear() before invoking its handlers, which drops every
        // header set up to that point. A callback runs at flush time instead, so a 500 carries the id
        // exactly as a 200 does — which is the response that most needs it.
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            ctx.Response.Headers[CorrelationId.HeaderName] = ctx.TraceIdentifier;
            return Task.CompletedTask;
        }, context);

        // CorrelationScope, not an anonymous object: the JSON console writes scope entries as real
        // key/value pairs only for IEnumerable<KeyValuePair<string, object>>, and an anonymous type
        // would land in the log as one opaque ToString().
        using (logger.BeginScope(new CorrelationScope(requestId)))
        {
            await next(context);
        }
    }
}
