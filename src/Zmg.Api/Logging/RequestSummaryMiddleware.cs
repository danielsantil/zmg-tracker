using System.Diagnostics;

namespace Zmg.Api.Logging;

/// <summary>
/// Times every request and logs the ones <see cref="RequestSummary"/> says are worth a line
/// (v2.10/M57) — failures, and anything slower than <c>Logging:SlowRequestMs</c>.
///
/// Sits <em>outside</em> the exception handler so it observes the status that was actually sent: an
/// unhandled exception has already become a 500 by the time this middleware's <c>finally</c> runs, and
/// is reported as a failed request rather than escaping unmeasured.
/// </summary>
internal sealed class RequestSummaryMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<RequestSummaryMiddleware> logger)
{
    private readonly long _slowMs = configuration.GetValue("Logging:SlowRequestMs", (long)RequestSummary.DefaultSlowMs);

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var status = context.Response.StatusCode;
            var method = context.Request.Method;
            // Path without the query string, deliberately — see the rule on Log.
            var path = context.Request.Path.Value ?? "/";

            switch (RequestSummary.Classify(path, status, elapsedMs, _slowMs))
            {
                case RequestOutcome.Failed:
                    Log.RequestFailed(logger, method, path, status, elapsedMs);
                    break;
                case RequestOutcome.Slow:
                    Log.RequestSlow(logger, method, path, status, elapsedMs);
                    break;
            }
        }
    }
}
