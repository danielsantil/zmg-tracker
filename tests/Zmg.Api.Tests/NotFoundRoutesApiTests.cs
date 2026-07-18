using System.Net;
using System.Text;

namespace Zmg.Api.Tests;

/// <summary>
/// 404-on-unknown-id was proven for only a handful of routes (M25 task 11). This Theory sweeps the
/// mutating/detail routes that load-then-check: an unknown id must surface a clean 404 before any body
/// validation runs. No mutation happens (every id is unknown), so one shared host serves them all.
/// </summary>
public class NotFoundRoutesApiTests(ZmgApiFactory factory) : IClassFixture<ZmgApiFactory>
{
    // (HTTP method, route with a fresh unknown id already substituted, sends a JSON body?)
    public static IEnumerable<object[]> Routes()
    {
        var id = Guid.NewGuid();
        var other = Guid.NewGuid();
        yield return ["GET", $"/api/artists/{id}", false];
        yield return ["PUT", $"/api/artists/{id}", true];
        yield return ["DELETE", $"/api/artists/{id}", false];
        yield return ["GET", $"/api/releases/{id}", false];
        yield return ["PUT", $"/api/releases/{id}", true];
        yield return ["DELETE", $"/api/releases/{id}", false];
        yield return ["POST", $"/api/releases/{id}/archive", false];
        yield return ["GET", $"/api/releases/{id}/archive-preview", false];
        yield return ["POST", $"/api/releases/{id}/tasks", true];
        yield return ["PUT", $"/api/releases/{id}/tasks/order", true];
        yield return ["PUT", $"/api/tasks/{id}", true];
        yield return ["PATCH", $"/api/tasks/{id}/toggle", false];
        yield return ["DELETE", $"/api/tasks/{id}", false];
        yield return ["GET", $"/api/songs/{id}", false];
        yield return ["PUT", $"/api/songs/{id}", true];
        yield return ["POST", $"/api/releases/{id}/tracks", true];
        yield return ["PUT", $"/api/releases/{id}/tracks/order", true];
        yield return ["PATCH", $"/api/releases/{id}/tracks/{other}/focus", false];
        yield return ["DELETE", $"/api/releases/{id}/tracks/{other}", false];
        yield return ["PUT", $"/api/template-tasks/{id}", true];
        yield return ["DELETE", $"/api/template-tasks/{id}", false];
        yield return ["PUT", $"/api/templates/{id}/tasks/order", true];
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Unknown_id_returns_404(string method, string url, bool sendsBody)
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (sendsBody)
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var res = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
