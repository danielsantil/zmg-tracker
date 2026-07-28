using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace Zmg.Api.Tests;

/// <summary>
/// M59 — the built SPA's own files must reach an anonymous browser.
///
/// This exists because the opposite shipped. <c>UseStaticFiles</c> sat *after* <c>UseAuthorization</c>,
/// and an asset path matches no endpoint (<c>MapFallbackToFile</c>'s catch-all carries the
/// <c>nonfile</c> constraint, so anything with a dot is excluded on purpose). "No endpoint" is exactly
/// when the fallback authorization policy applies — so every <c>/assets/*.js</c> answered a signed-out
/// visitor with a 302 to <c>/Account/Login</c>. The shell loaded, its scripts did not, and the login
/// screen the shell exists to render never appeared.
///
/// Production hid it completely: Cloudflare serves those files from the edge, so only the container
/// was broken — and the container is the documented rollback target, the one thing you reach for on
/// the day the edge is not an option.
/// </summary>
public class StaticFileAuthApiTests : IClassFixture<StaticFileApiFactory>
{
    private readonly StaticFileApiFactory _factory;

    public StaticFileAuthApiTests(StaticFileApiFactory factory) => _factory = factory;

    [Fact]
    public async Task An_asset_is_served_to_an_anonymous_browser_not_redirected_at_it()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync($"/assets/{StaticFileApiFactory.AssetName}");

        // The 302 is the specific failure: a browser asked for JavaScript and got a redirect, so the
        // SPA never booted and the page stayed blank with nothing in the console to explain it.
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Null(res.Headers.Location);
        Assert.Equal(StaticFileApiFactory.AssetBody, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_shell_is_served_to_an_anonymous_browser_too()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var res = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains(StaticFileApiFactory.ShellMarker, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Serving_files_before_the_auth_middleware_did_not_open_the_api()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        // The whole point of moving static files earlier is that it changes nothing else. If this ever
        // goes green as a 200, the reordering took the API's deny-by-default with it.
        var res = await client.GetAsync("/api/releases");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}

/// <summary>
/// Anonymous, with a throwaway web root holding one asset and one shell. Self-contained on purpose:
/// pointing at the real <c>wwwroot</c> would make this pass or fail on whether someone had run
/// <c>pnpm build</c>, and CI runs <c>dotnet test</c> without it.
/// </summary>
public class StaticFileApiFactory : ZmgApiFactory
{
    public const string AssetName = "app-1a2b3c.js";
    public const string AssetBody = "console.log('zmg');";
    public const string ShellMarker = "<!-- zmg-test-shell -->";

    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"zmg-webroot-{Guid.NewGuid():N}");

    public StaticFileApiFactory()
    {
        // The stub sign-in scheme off, so requests really are anonymous — otherwise the fallback
        // policy is satisfied and this whole file proves nothing.
        Authenticated = false;

        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", AssetName), AssetBody);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), $"<html><body>{ShellMarker}</body></html>");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseWebRoot(_webRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot)) Directory.Delete(_webRoot, recursive: true);
    }
}
