using Zmg.Api.Logging;

namespace Zmg.Api.Tests;

/// <summary>
/// M57 — the request id is taken from a client-controlled header, so it is normalized rather than
/// trusted. Same shape as <c>RedirectsTests</c>: a short allow-list with several ways to get it wrong,
/// and every wrong version is invisible until someone abuses it.
/// </summary>
public class CorrelationIdTests
{
    [Theory]
    [InlineData("1f5a0c0e-9d0a-4a4b-8a3f-7d2f9c1e77b2")]   // Envoy's shape at ACA ingress
    [InlineData("9f86d081884c7d659a2feaa0c55ad015")]        // ours
    [InlineData("a_b.c-D9")]
    public void A_sane_supplied_id_is_adopted_so_it_joins_the_ingress_record(string supplied)
    {
        Assert.Equal(supplied, CorrelationId.Normalize(supplied));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void A_missing_id_gets_a_fresh_one(string? supplied)
    {
        var id = CorrelationId.Normalize(supplied);

        Assert.False(string.IsNullOrWhiteSpace(id));
    }

    [Theory]
    [InlineData("abc\r\nX-Injected: 1")]   // header splitting, the reason this echoes nothing raw
    [InlineData("abc\ndef")]
    [InlineData("{\"level\":\"error\"}")]  // forging a whole extra JSON log entry
    [InlineData("id with spaces")]
    [InlineData("id/../..")]
    public void An_id_that_could_forge_a_header_or_a_log_line_is_replaced(string supplied)
    {
        Assert.NotEqual(supplied, CorrelationId.Normalize(supplied));
    }

    [Fact]
    public void An_unbounded_id_is_replaced_rather_than_truncated()
    {
        // Truncating would keep an attacker-chosen prefix on every line of the request; a fresh id
        // keeps nothing. The bound also stops one header inflating an entire request's logs.
        var supplied = new string('a', CorrelationId.MaxLength + 1);

        var id = CorrelationId.Normalize(supplied);

        Assert.NotEqual(supplied, id);
        Assert.True(id.Length <= CorrelationId.MaxLength);
    }

    [Fact]
    public void The_scope_publishes_the_id_under_its_own_key_not_the_frameworks()
    {
        // ASP.NET's hosting scope already publishes "RequestId" — Kestrel's per-connection identifier,
        // which nothing outside this process has ever seen. Two different values under one key in the
        // same Scopes array is a KQL query that picks one at random and reports no error, so the names
        // must stay distinct. M58's cookbook queries this key by name.
        var scope = new CorrelationScope("abc123");

        var pair = Assert.Single(scope);
        Assert.Equal("CorrelationId", pair.Key);
        Assert.Equal("abc123", pair.Value);
        Assert.Equal("CorrelationId:abc123", scope.ToString());
    }

    [Fact]
    public void Generated_ids_are_unique()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => CorrelationId.Generate()).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
