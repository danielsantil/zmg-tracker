using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Zmg.Api.Contracts;
using Zmg.Domain;

namespace Zmg.Api.Endpoints;

/// <summary>
/// Sign-in, sign-out, and "who am I" (v2.10/M55).
///
/// Everything here is <c>AllowAnonymous</c>, which is the point: with a deny-by-default fallback
/// policy these are the only doors that answer without a session, and <c>/me</c> in particular
/// <em>is</em> the probe the SPA uses to decide whether to render the app or the login gate.
///
/// The callback is not routed here — the OIDC handler owns <see cref="AuthenticationExtensions.CallbackPath"/>
/// and intercepts it before endpoint routing, which is why there is no MapGet for it.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        // Kicks off the Google round trip. `returnUrl` is where to land afterwards, and it goes through
        // Redirects.SafeLocalPath first: unchecked, this is a textbook open redirect where the victim
        // sees a genuine app.zionmusicgroup.com link and a genuine Google consent screen before being
        // dropped somewhere else entirely.
        group.MapGet("/login", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = Redirects.SafeLocalPath(returnUrl) },
                [OpenIdConnectDefaults.AuthenticationScheme]));

        // The SPA's auth probe. 200 with the user, or 401 with a code — never a redirect, so a fetch
        // gets a clean answer instead of an HTML login page it would try to parse as JSON.
        group.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Json(
                    new ValidationErrorResponse([new Message(AccessControl.RequiredCode)]),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new AuthUserDto(
                user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                user.FindFirstValue(ClaimTypes.Name)));
        });

        // POST, not GET: a GET sign-out can be triggered by any <img> on any page. Deletes the session
        // row via the ticket store, so the session is gone server-side rather than merely forgotten by
        // the browser.
        group.MapPost("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }
}
