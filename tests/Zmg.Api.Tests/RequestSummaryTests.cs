using Zmg.Api.Logging;

namespace Zmg.Api.Tests;

/// <summary>
/// M57 — which finished requests earn a log line. Pure, and worth pinning because both failure modes
/// are silent in production: too quiet and the log has nothing when you need it, too loud and nobody
/// reads it at all.
/// </summary>
public class RequestSummaryTests
{
    private const long SlowMs = 1000;

    [Fact]
    public void A_fast_success_says_nothing_because_ingress_already_recorded_it()
    {
        Assert.Equal(RequestOutcome.Silent, RequestSummary.Classify("/api/releases", 200, 12, SlowMs));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public void Any_error_status_is_reported_even_when_it_was_fast(int status)
    {
        Assert.Equal(RequestOutcome.Failed, RequestSummary.Classify("/api/releases", status, 3, SlowMs));
    }

    [Fact]
    public void A_slow_success_is_reported()
    {
        Assert.Equal(RequestOutcome.Slow, RequestSummary.Classify("/api/releases", 200, SlowMs, SlowMs));
    }

    [Fact]
    public void Just_under_the_threshold_stays_silent()
    {
        Assert.Equal(RequestOutcome.Silent, RequestSummary.Classify("/api/releases", 200, SlowMs - 1, SlowMs));
    }

    [Fact]
    public void A_failure_reads_as_failed_rather_than_slow_when_it_is_both()
    {
        // One line per request, and "it returned 500" is the part worth leading with.
        Assert.Equal(RequestOutcome.Failed, RequestSummary.Classify("/api/releases", 500, SlowMs * 5, SlowMs));
    }

    [Theory]
    [InlineData(RequestSummary.AuthProbePath)]
    [InlineData("/API/Auth/Me")]
    public void The_signed_out_auth_probe_is_not_a_failure(string path)
    {
        // /api/auth/me answers 401 on every cold visit — that 401 *is* the answer "signed out". Logging
        // it as a failure would make the most common line in the file one that never needs acting on.
        Assert.Equal(RequestOutcome.Silent, RequestSummary.Classify(path, 401, 5, SlowMs));
    }

    [Fact]
    public void The_probe_is_excused_only_for_that_one_status()
    {
        Assert.Equal(RequestOutcome.Failed, RequestSummary.Classify(RequestSummary.AuthProbePath, 500, 5, SlowMs));
    }

    [Fact]
    public void A_slow_probe_is_still_worth_a_line()
    {
        Assert.Equal(RequestOutcome.Slow, RequestSummary.Classify(RequestSummary.AuthProbePath, 401, SlowMs, SlowMs));
    }
}
