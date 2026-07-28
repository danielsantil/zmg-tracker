using System.Net;
using System.Net.Http.Json;
using Zmg.Api.Contracts;
using Zmg.Domain;

namespace Zmg.Api.Tests;

/// <summary>
/// M55 — the real authentication pipeline, with the test scheme switched off.
///
/// Every other test in this suite runs authenticated so it can be about releases and songs. These run
/// anonymously, which is the only way to prove the thing that actually matters: that the app is shut
/// by default. A regression here would not fail anything else — it would just quietly open the door.
///
/// The Google round trip itself is not exercised: challenging the OIDC handler would fetch Google's
/// discovery document over the network, and this suite touches neither R2 nor the network by standing
/// rule. That leg is verified live in M59.
/// </summary>
public class AuthApiTests
{
    /// <summary>A factory with the stub scheme removed, so requests really are anonymous.</summary>
    private static ZmgApiFactory Anonymous() => new() { Authenticated = false };

    [Theory]
    [InlineData("/api/artists")]
    [InlineData("/api/releases")]
    [InlineData("/api/songs")]
    [InlineData("/api/templates")]
    [InlineData("/api/pending")]
    public async Task Business_endpoints_are_shut_to_anonymous_callers(string path)
    {
        using var factory = Anonymous();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task An_unauthenticated_api_call_answers_401_with_a_code_never_a_redirect()
    {
        using var factory = Anonymous();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync("/api/artists");

        // The distinction is the whole design of the cookie handler's OnRedirectToLogin: a 302 would
        // send the SPA's fetch chasing Google's consent page and failing while parsing HTML as JSON.
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Null(res.Headers.Location);

        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(AccessControl.RequiredCode, body!.Errors.Single().Code);
    }

    [Fact]
    public async Task Writes_are_shut_too_not_only_reads()
    {
        using var factory = Anonymous();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.PostAsJsonAsync("/api/artists", new ArtistInput("Anonymous Artist", null));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Health_stays_anonymous_so_the_platform_can_probe_it()
    {
        using var factory = Anonymous();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task The_spa_shell_stays_anonymous_because_it_renders_the_login_screen()
    {
        using var factory = Anonymous();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // A deep link a signed-out person might follow. It must return the shell, not a 401 — otherwise
        // there is nothing to show them the login gate in the first place.
        var res = await client.GetAsync("/catalog/8f2b1c4e-0000-0000-0000-000000000000");

        Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_answers_401_with_a_code_when_signed_out()
    {
        using var factory = Anonymous();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync("/api/auth/me");

        // Anonymous by design — this endpoint *is* the probe, so it has to be reachable to say "no".
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(AccessControl.RequiredCode, body!.Errors.Single().Code);
    }

    [Fact]
    public async Task Me_returns_the_signed_in_identity_and_nothing_resembling_a_role()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var me = await res.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.Equal(SeedData.AllowedUsers().Single().Email, me!.Email);

        // Authorization is flat: if this DTO ever grows a field, check it isn't a role in disguise.
        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("role", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("permission", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_get_cannot_sign_you_out()
    {
        using var factory = new ZmgApiFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Sign-out is POST because a GET one can be fired by any <img src> on any page the user
        // happens to visit. There is no GET handler, so this falls through to the SPA shell — a 200
        // with HTML rather than a 405, which is what MapFallbackToFile does with every unmatched
        // /api/* path. Harmless, and the point is what it is *not*: a 204, meaning nothing signed out.
        var get = await client.GetAsync("/api/auth/logout");
        Assert.NotEqual(HttpStatusCode.NoContent, get.StatusCode);

        var post = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);
    }

    [Fact]
    public async Task Auth_error_codes_are_distinct_and_namespaced()
    {
        // Cheap, but it pins the contract M46 cares about: these are permanent identifiers that map 1:1
        // onto i18next key paths, and MessageCodeApiTests proves both locales carry them.
        var codes = new[]
        {
            AccessControl.RequiredCode,
            AccessControl.NotAllowedCode,
            AccessControl.EmailUnverifiedCode,
        };

        Assert.Equal(codes.Length, codes.Distinct().Count());
        Assert.All(codes, c => Assert.StartsWith("error.auth.", c, StringComparison.Ordinal));
    }
}
