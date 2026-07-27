namespace Zmg.Api.Services.Interfaces;

/// <summary>The request's locale, resolved once from <c>X-Lang</c> / <c>Accept-Language</c> (M47).</summary>
public interface ILocaleAccessor
{
    /// <summary>A supported, region-free locale — <c>en</c> or <c>es</c>, never null.</summary>
    string Locale { get; }
}

/// <summary>
/// Per-locale checklist text for the current request, keyed by <c>TaskCodes</c> slug (M47). Pair with
/// <c>TaskText.Resolve</c>, which falls back to the stored English title on any miss.
/// </summary>
public interface ITaskTranslationService
{
    string Locale { get; }

    /// <summary>code → text for the request's locale; empty for <c>en</c>, which lives in the Title column.</summary>
    Task<IReadOnlyDictionary<string, string>> ForRequestLocaleAsync(CancellationToken ct = default);
}
