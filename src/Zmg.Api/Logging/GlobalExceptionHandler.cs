using Microsoft.AspNetCore.Diagnostics;
using Zmg.Api.Contracts;
using Zmg.Api.Services;
using Zmg.Domain;

namespace Zmg.Api.Logging;

/// <summary>
/// The one place an unhandled exception is turned into a response (v2.10/M57).
///
/// Without it, an escaped exception produces whatever the host does by default — an empty body from
/// Kestrel, a developer exception page elsewhere — and the SPA's <c>api/client.ts</c> falls back to a
/// generic sentence because there was no <c>errors</c> array to read. Here it is logged exactly once
/// with its stack, method and path, and answered with the same coded envelope every other failure uses
/// (M46), so the client needs no special case for "the server broke".
/// </summary>
/// <remarks>
/// <para>
/// The exception itself never reaches the browser: the response carries a code and the request id, and
/// that id is what turns "it broke" into a log query. Development is unaffected —
/// <c>WebApplication</c> puts the developer exception page ahead of this in the pipeline, which is
/// what you want at a keyboard.
/// </para>
/// <para>
/// <b><see cref="BadHttpRequestException"/> is not an outage and must not become a 500.</b> Kestrel
/// maps it to its own status, and the reachable case here is a real one: an upload past Kestrel's
/// request-body limit arrives as a 413, and cover upload is exactly where a user meets it. Blanket
/// 500ing it would turn a clear "too large" into "something broke on our side", and would page whoever
/// eventually watches the error rate for a client mistake.
/// </para>
/// </remarks>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var method = httpContext.Request.Method;
        // Path without its query string, deliberately — see the rule on Log.
        var path = httpContext.Request.Path.Value ?? "/";

        // Nothing can be written once the response is on the wire; logging it is all that's left, and
        // trying to write would throw a second exception out of the exception handler.
        if (httpContext.Response.HasStarted)
        {
            Log.RequestUnhandled(logger, exception, method, path);
            return true;
        }

        var (status, code) = exception is BadHttpRequestException bad
            ? (bad.StatusCode, ServiceErrors.BadRequest)
            : (StatusCodes.Status500InternalServerError, ServiceErrors.Unexpected);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            Log.RequestUnhandled(logger, exception, method, path);
        }
        else
        {
            // Client fault: worth a line, not an Error one, and without the stack — the type and status
            // say everything, and a stack per malformed request is how an error feed becomes unreadable.
            Log.RequestRejected(logger, method, path, status, exception.GetType().Name);
        }

        httpContext.Response.StatusCode = status;
        // The id also rides the x-request-id header (re-applied by CorrelationMiddleware's OnStarting
        // callback, which survives the Response.Clear() this middleware performs before getting here).
        // It is in the body as well so the sentence the user reads carries something they can quote.
        await httpContext.Response.WriteAsJsonAsync(
            new ValidationErrorResponse([Message.With(code, ("requestId", httpContext.TraceIdentifier))]),
            cancellationToken);

        return true;
    }
}
