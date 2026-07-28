using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zmg.Api.Contracts;
using Zmg.Api.Services;
using Zmg.Domain;
using Zmg.Infra.Data;

namespace Zmg.Api.Extensions;

/// <summary>
/// Google SSO over a revocable server-side session (v2.10/M55).
///
/// This is a BFF: the API is the OAuth client, tokens never reach JavaScript, and the browser holds
/// nothing but an opaque session id. Google's OIDC discovery document drives the handler, so PKCE,
/// <c>state</c>, <c>nonce</c> and full <c>id_token</c> validation are Microsoft's code rather than
/// hand-rolled here — the parts of OAuth that are dangerous to write yourself.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>Google's OIDC issuer. Discovery hangs off this; no endpoint URLs are hardcoded.</summary>
    private const string GoogleAuthority = "https://accounts.google.com";

    public const string CallbackPath = "/api/auth/google/callback";

    public static void AddZmgAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var auth = BindAuthOptions(configuration);
        services.Configure<AuthOptions>(o =>
        {
            o.SessionDays = auth.SessionDays;
            o.AllowedHosts = auth.AllowedHosts;
            o.PostLoginOrigin = auth.PostLoginOrigin;
            o.GoogleClientId = auth.GoogleClientId;
            o.GoogleClientSecret = auth.GoogleClientSecret;
        });

        // Keys in Postgres, not on disk. Not optional: the container filesystem is ephemeral on ACA, so
        // with min_replicas=0 every scale-from-zero would mint a fresh key ring and silently invalidate
        // every session cookie. SetApplicationName is pinned because the default derives from the
        // content-root path, which differs between the container and a laptop — a mismatch decrypts
        // nothing and looks exactly like "everyone got logged out".
        services.AddDataProtection()
            .PersistKeysToDbContext<ZmgDbContext>()
            .SetApplicationName("zmg-tracker");

        services.AddSingleton<ITicketStore, PostgresTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((o, store) => o.SessionStore = store);

        services
            .AddAuthentication(o =>
            {
                // Cookie is the default for *everything*, challenge included. Pointing the default
                // challenge at OpenIdConnect would make an unauthenticated /api/* call 302 to Google
                // instead of answering 401 — the SPA's fetch would then chase a cross-origin consent
                // page. Sign-in names the OIDC scheme explicitly, and it is the only thing that does.
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o => ConfigureCookie(o, auth, env))
            .AddOpenIdConnect(o => ConfigureGoogle(o, auth));

        // Deny by default. A new endpoint is protected unless it explicitly opts out, so forgetting
        // `.RequireAuthorization()` on something added in six months is a non-event rather than a hole.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    /// <summary>Reads the two config sections into one options object. Also used by startup validation.</summary>
    public static AuthOptions BindAuthOptions(IConfiguration configuration)
    {
        var auth = new AuthOptions();
        configuration.GetSection(AuthOptions.SectionName).Bind(auth);
        auth.GoogleClientId = configuration["Authentication:Google:ClientId"];
        auth.GoogleClientSecret = configuration["Authentication:Google:ClientSecret"];
        return auth;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions o, AuthOptions auth, IWebHostEnvironment env)
    {
        o.Cookie.Name = "zmg_session";
        o.Cookie.HttpOnly = true;
        // Always in prod; SameAsRequest in dev because the API is plain http on localhost and an
        // Always cookie would simply never be set there.
        o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        // Lax, not Strict: the return leg from Google is a top-level cross-site navigation, and Strict
        // would withhold the cookie on exactly that request. Lax is also the CSRF control — it keeps the
        // session off cross-site state-changing requests, which is why there is no antiforgery token.
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromDays(auth.SessionDays);
        // Absolute, never rolling: a sliding window means a stolen cookie never expires while it is
        // being used.
        o.SlidingExpiration = false;

        o.Events.OnRedirectToLogin = ctx => ChallengeResponse(ctx, AccessControl.RequiredCode);
        o.Events.OnRedirectToAccessDenied = ctx => ChallengeResponse(ctx, AccessControl.NotAllowedCode);
    }

    /// <summary>
    /// An unauthenticated <c>/api/*</c> call answers <b>401 with a code</b> instead of redirecting to a
    /// login page. A 302 would send the SPA's <c>fetch</c> chasing an HTML document and failing while
    /// parsing it as JSON — a confusing error for a well-understood state. Non-API paths still redirect,
    /// though in practice the SPA shell is anonymous and renders the gate itself.
    /// </summary>
    private static Task ChallengeResponse(RedirectContext<CookieAuthenticationOptions> ctx, string code)
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsJsonAsync(new ValidationErrorResponse([new Message(code)]));
        }

        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }

    private static void ConfigureGoogle(OpenIdConnectOptions o, AuthOptions auth)
    {
        o.Authority = GoogleAuthority;
        o.ClientId = auth.GoogleClientId;
        o.ClientSecret = auth.GoogleClientSecret;
        o.ResponseType = "code";           // authorization code flow; UsePkce defaults to true on .NET 8
        o.CallbackPath = CallbackPath;
        o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        o.Scope.Clear();
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.Scope.Add("profile");

        // The email arrives inside the validated id_token, so there is no userinfo round trip. Nothing
        // needs Google's access token afterwards, so it is never persisted into the session either.
        o.GetClaimsFromUserInfoEndpoint = false;
        o.SaveTokens = false;

        // Keep Google's claim names as Google spells them, instead of ASP.NET's legacy WS-Fed URIs.
        // The principal is rebuilt below anyway; this just makes the reads say what they mean.
        o.MapInboundClaims = false;

        o.Events = new OpenIdConnectEvents
        {
            OnTicketReceived = OnTicketReceived,
            OnRemoteFailure = ctx =>
            {
                // A failed exchange (user cancelled, state mismatch, clock skew) must not surface a
                // framework exception page. Send them back to the gate.
                ctx.HandleResponse();
                ctx.Response.Redirect("/?denied=1");
                return Task.CompletedTask;
            },
        };
    }

    /// <summary>
    /// The whitelist gate. Google has proved who they are; this decides whether that identity gets in,
    /// and replaces Google's principal with a minimal one carrying exactly what the app needs.
    /// </summary>
    private static async Task OnTicketReceived(TicketReceivedContext ctx)
    {
        var services = ctx.HttpContext.RequestServices;
        var options = services.GetRequiredService<IOptions<AuthOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Zmg.Api.Auth");

        var claims = ctx.Principal?.Claims.ToList() ?? [];
        var email = EmailNormalization.Normalize(claims.FirstOrDefault(c => c.Type == "email")?.Value);
        var verified = claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
        var name = claims.FirstOrDefault(c => c.Type == "name")?.Value;

        // An unverified address would let anyone who can set one on some account impersonate a listed
        // person. Google normally verifies, so this is cheap insurance rather than a common path.
        if (!string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase))
        {
            Deny(ctx, logger, email, "email_unverified", options);
            return;
        }

        var db = services.GetRequiredService<ZmgDbContext>();
        var user = await db.AllowedUsers.FirstOrDefaultAsync(u => u.Email == email);

        if (!AccessControl.IsAllowed(user))
        {
            // One reason recorded, two states: never listed, or listed and disabled. The browser is told
            // neither — see AccessControl.NotAllowedCode.
            Deny(ctx, logger, email, user is null ? "not_listed" : "disabled", options);
            return;
        }

        // Cosmetic, and the only write this path makes to the user row.
        if (!string.IsNullOrWhiteSpace(name) && user!.DisplayName != name)
        {
            user.DisplayName = name;
            await db.SaveChangesAsync();
        }

        // Google's full token payload does not get serialized into the session — the app needs an id,
        // an address and a name, so that is all the session carries.
        ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user!.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role));

        ctx.Properties!.IsPersistent = true;
        ctx.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(options.SessionDays);
        ctx.Properties.RedirectUri = PostLoginUri(ctx.Properties.RedirectUri, options);

        logger.LogInformation("auth.login.ok {Email}", user.Email);
    }

    private static void Deny(TicketReceivedContext ctx, ILogger logger, string email, string reason, AuthOptions options)
    {
        // The address is logged — the one deliberate exception to "no user attribution in logs" — because
        // a failed-login spike is otherwise unactionable: you cannot tell a partner fat-fingering from
        // someone probing the door.
        logger.LogInformation("auth.login.denied {Email} {Reason}", email, reason);

        ctx.HandleResponse();
        // No email in the redirect URL, deliberately. ACA's ingress HTTP logs record the full path
        // *including the query string*, so putting it there would push an address into Log Analytics on
        // every denial — contradicting the same decision that keeps attribution off business writes.
        ctx.Response.Redirect(PostLoginUri("/?denied=1", options));
    }

    /// <summary>Prefixes the dev origin when configured, so dev lands on the Vite server, not the API.</summary>
    private static string PostLoginUri(string? path, AuthOptions options)
    {
        var local = Redirects.SafeLocalPath(path);
        return string.IsNullOrWhiteSpace(options.PostLoginOrigin)
            ? local
            : options.PostLoginOrigin.TrimEnd('/') + local;
    }

    /// <summary>
    /// Trusts <c>X-Forwarded-Host</c>/<c>-Proto</c> from the Cloudflare Worker, constrained to known
    /// hostnames. Must run before authentication: the OIDC handler builds <c>redirect_uri</c> from
    /// <c>Request.Scheme</c> + <c>Request.Host</c>, and the Worker rewrites <c>Host</c> to the ACA FQDN
    /// because the ingress routes on it.
    /// </summary>
    public static void UseZmgForwardedHeaders(this WebApplication app)
    {
        var auth = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
        };

        // The proxy is Cloudflare, whose egress IPs are neither loopback nor enumerable, so the default
        // known-proxy check cannot be satisfied and has to go. AllowedHosts replaces it as the guard.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        // X-Forwarded-Host is enabled ONLY when there is a host allow-list to constrain it.
        //
        // This is not defensive padding. ForwardedHeadersMiddleware treats an *empty* AllowedHosts as
        // "allow every host" (`if (AllowedHosts.Count == 0) _allowAllHosts = true;`), so a deploy that
        // forgot to configure it would accept any forged X-Forwarded-Host — silently, and with the ACA
        // FQDN publicly reachable, from anyone. Leaving the flag off instead fails closed: the OIDC
        // redirect_uri is then built from the real Host, Google rejects it as unregistered, and sign-in
        // breaks loudly rather than trusting an attacker's header.
        if (auth.AllowedHosts.Length > 0)
        {
            options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;
            foreach (var host in auth.AllowedHosts) options.AllowedHosts.Add(host);
        }
        else
        {
            app.Logger.LogWarning(
                "Auth:AllowedHosts is empty — X-Forwarded-Host will be ignored. Sign-in behind the " +
                "Cloudflare Worker will fail until it is configured.");
        }

        app.UseForwardedHeaders(options);
    }
}
