using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zmg.Domain;

namespace Zmg.Api.Tests;

/// <summary>
/// Signs every request in as the seeded whitelist user (v2.10/M55).
///
/// The suite predates authentication by 220 tests, all of which are about releases, songs and
/// checklists rather than about who is asking. Stubbing the scheme keeps them saying what they meant
/// before, instead of every one of them growing a sign-in dance that tests nothing.
///
/// It stubs <em>authentication</em> only. The fallback authorization policy still runs and is simply
/// satisfied, so this cannot hide an endpoint that forgot to be protected — <c>AuthApiTests</c> runs
/// the real pipeline for that.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Deliberately the seeded user rather than an invented one: claims that don't correspond to a
        // real AllowedUser row would let a test pass while the same code failed in production.
        var seeded = SeedData.AllowedUsers().Single();

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, seeded.Id.ToString()),
                new Claim(ClaimTypes.Email, seeded.Email),
                new Claim(ClaimTypes.Name, "Test User"),
            ],
            SchemeName,
            ClaimTypes.Name,
            ClaimTypes.Role);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
