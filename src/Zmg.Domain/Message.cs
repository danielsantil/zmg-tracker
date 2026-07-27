namespace Zmg.Domain;

/// <summary>
/// One server-minted user-facing message, as a culture-invariant **code** plus the values it
/// interpolates — never prose (v2.8/M46). The SPA owns every sentence: <see cref="Code"/> maps 1:1
/// onto an i18next key path, so rendering is <c>t(code, args)</c> with no translation table in
/// between, and the server keeps <c>InvariantGlobalization=true</c> while the user reads Spanish.
/// </summary>
/// <remarks>
/// Codes are <b>permanent identifiers</b>: renaming one is a breaking change on both sides at once,
/// the same rule the integer enums carry. There is deliberately no parallel <c>message</c> field —
/// a second prose channel is exactly the thing that drifts (see the two-warning-channels rule).
/// <paramref name="Args"/> values are already-formatted strings (an artist name, a release title),
/// so no culture is needed to produce them.
/// </remarks>
public readonly record struct Message(string Code, IReadOnlyDictionary<string, string>? Args = null)
{
    /// <summary>Sugar for the interpolating messages: <c>Message.With("error.x", ("name", n))</c>.</summary>
    public static Message With(string code, params (string Key, string Value)[] args) =>
        new(code, args.ToDictionary(a => a.Key, a => a.Value));

    /// <summary>Implicit lift so a code-only message reads as just its code at the call sites.</summary>
    public static implicit operator Message(string code) => new(code);
}
