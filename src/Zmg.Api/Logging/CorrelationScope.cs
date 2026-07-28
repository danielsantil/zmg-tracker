using System.Collections;

namespace Zmg.Api.Logging;

/// <summary>
/// The logging scope that carries the request id onto every line of a request (v2.10/M57).
///
/// A dedicated type rather than a <c>Dictionary</c> for two reasons, both visible in the output. The
/// JSON console writes each scope's <c>ToString()</c> alongside its key/value pairs, and a dictionary's
/// is the literal text <c>System.Collections.Generic.Dictionary`2[System.String,System.Object]</c> —
/// noise on every line. And it allocates once with no hashing, on a path that runs per request.
/// </summary>
internal readonly struct CorrelationScope(string requestId) : IReadOnlyList<KeyValuePair<string, object>>
{
    /// <summary>
    /// <b>Not</b> <c>RequestId</c>, deliberately. ASP.NET's own hosting scope already publishes a
    /// <c>RequestId</c> — Kestrel's per-connection identifier, which is set before any middleware runs
    /// and which nothing outside this process has ever seen. Reusing the name would put two different
    /// values under one key in the same <c>Scopes</c> array, and a KQL query would pick whichever it
    /// found first: wrong answers, no error. This one is the id ACA's ingress log also records, so it
    /// is the only one that joins anything.
    /// </summary>
    public const string Key = "CorrelationId";

    public int Count => 1;

    public KeyValuePair<string, object> this[int index] => index == 0
        ? new KeyValuePair<string, object>(Key, requestId)
        : throw new ArgumentOutOfRangeException(nameof(index));

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        yield return this[0];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"{Key}:{requestId}";
}
