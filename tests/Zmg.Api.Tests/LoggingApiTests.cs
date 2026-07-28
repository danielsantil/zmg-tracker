using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zmg.Api.Contracts;
using Zmg.Api.Logging;
using Zmg.Api.Services;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;

namespace Zmg.Api.Tests;

/// <summary>
/// M57 — the two things about logging a test can actually observe from outside: what an unhandled
/// exception becomes, and that a request id survives the round trip.
///
/// The log lines themselves aren't asserted on. They're source-generated from
/// <see cref="Zmg.Api.Logging.Log"/>, so what would be under test is the framework's formatter rather
/// than any decision of ours — and pinning message text would freeze the one thing event ids exist to
/// stop mattering.
/// </summary>
public class LoggingApiTests : IClassFixture<ThrowingApiFactory>
{
    private readonly ThrowingApiFactory _factory;

    public LoggingApiTests(ThrowingApiFactory factory) => _factory = factory;

    // ---- Unhandled exceptions ----

    [Fact]
    public async Task An_unhandled_exception_answers_a_coded_500_rather_than_whatever_the_host_does()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/pending");

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(ServiceErrors.Unexpected, body!.Errors.Single().Code);
    }

    [Fact]
    public async Task A_500_tells_the_browser_nothing_about_the_exception()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/pending");

        var raw = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ThrowingPendingService.Detail, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Zmg.Api.Tests", raw, StringComparison.Ordinal);   // no stack, no type names
    }

    [Fact]
    public async Task A_500_carries_the_request_id_in_both_the_header_and_the_body()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/pending");

        // The header survives despite the exception-handler middleware calling Response.Clear() — that
        // is what CorrelationMiddleware's OnStarting callback is for, and this is the response that
        // most needs the id.
        var header = Assert.Single(res.Headers.GetValues(CorrelationId.HeaderName));
        var body = await res.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Equal(header, body!.Errors.Single().Args!["requestId"]);
    }

    // ---- Correlation ----

    [Fact]
    public async Task A_supplied_request_id_is_echoed_rather_than_replaced()
    {
        // ACA's ingress sets this and logs it against the same request, so adopting the caller's value
        // is exactly what makes an app log line and its ingress record joinable.
        const string supplied = "1f5a0c0e-9d0a-4a4b-8a3f-7d2f9c1e77b2";
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add(CorrelationId.HeaderName, supplied);

        var res = await client.SendAsync(request);

        Assert.Equal(supplied, Assert.Single(res.Headers.GetValues(CorrelationId.HeaderName)));
    }

    [Fact]
    public async Task A_request_without_an_id_is_given_one()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/health");

        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(res.Headers.GetValues(CorrelationId.HeaderName))));
    }

    [Fact]
    public async Task An_id_that_would_forge_a_log_entry_never_makes_it_back_out()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        // HttpClient rejects CR/LF outright, so the case reachable over HTTP is the structural one:
        // a value shaped like the JSON the console formatter writes.
        request.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, "{\"Level\":\"Error\"}");

        var res = await client.SendAsync(request);

        Assert.Equal("{\"Level\":\"Error\"}", request.Headers.GetValues(CorrelationId.HeaderName).Single());
        Assert.DoesNotContain("{", Assert.Single(res.Headers.GetValues(CorrelationId.HeaderName)), StringComparison.Ordinal);
    }
}

/// <summary>
/// The real app with one service swapped for one that throws — the only honest way to reach the
/// exception handler, since no endpoint is supposed to be able to.
/// </summary>
public class ThrowingApiFactory : ZmgApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPendingService>();
            services.AddScoped<IPendingService, ThrowingPendingService>();
        });
    }
}

public sealed class ThrowingPendingService : IPendingService
{
    /// <summary>Something distinctive, so a test can prove it did *not* reach the browser.</summary>
    public const string Detail = "pending-service-exploded-with-a-secret";

    public Task<IReadOnlyList<PendingAction>> ListAsync(CancellationToken ct = default) =>
        throw new InvalidOperationException(Detail);

    public Task<IReadOnlyList<PendingAction>> ListByReleaseIdAsync(Guid releaseId, CancellationToken ct = default) =>
        throw new InvalidOperationException(Detail);
}
