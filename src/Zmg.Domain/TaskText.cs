namespace Zmg.Domain;

/// <summary>
/// Per-locale checklist text, resolved by lookup rather than by rewriting rows (v2.8/M47). A release's
/// task titles stay the snapshot <see cref="TemplateCopy"/> wrote; the stable
/// <see cref="Entities.ReleaseTask.SourceCode"/> resolves an override at read time, so translating
/// never touches stored data and switching language never loses a user's edit.
/// </summary>
/// <remarks>
/// English is not a translation — it lives in the <c>Title</c> column, so the <c>en</c> locale needs no
/// rows at all and the fallback path is simply the column. That is why every miss here returns the
/// title: a null code (user-added task), an unknown locale, a code with no row yet, or a blank
/// translation. The one thing this must never return is a raw code or an empty string.
/// <para>
/// This is pure data lookup, deliberately: no <c>CultureInfo</c>, no <c>.resx</c>, no
/// <c>CurrentUICulture</c>. The container ships <c>InvariantGlobalization=true</c> on plain
/// <c>chiseled</c> (M41) and v2.8 must keep it that way.
/// </para>
/// </remarks>
public static class TaskText
{
    /// <summary>The locale carried by the <c>Title</c> column itself — never stored as a translation row.</summary>
    public const string DefaultLocale = "en";

    /// <summary>Locales the app serves. Adding one is a data question, not a code one.</summary>
    public static readonly IReadOnlyList<string> SupportedLocales = ["en", "es"];

    /// <summary>
    /// Normalizes an <c>X-Lang</c> / <c>Accept-Language</c> value to a supported locale: lowercased,
    /// first entry only, region stripped (<c>es-MX;q=0.9</c> → <c>es</c>), anything unrecognized →
    /// <see cref="DefaultLocale"/>. Ordinal throughout, so no culture data is touched.
    /// </summary>
    public static string NormalizeLocale(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DefaultLocale;

        var first = raw.Split(',')[0].Split(';')[0].Trim();
        var language = first.Split('-')[0].ToLowerInvariant();

        return SupportedLocales.Contains(language, StringComparer.Ordinal) ? language : DefaultLocale;
    }

    /// <summary>
    /// The text to show for a task: its translation when one exists for the request's locale, else the
    /// English <paramref name="fallbackTitle"/> the row already carries.
    /// </summary>
    /// <param name="code">The task's stable code, or null for a user-added/edited task (never translated).</param>
    /// <param name="fallbackTitle">The stored title — English, and the answer whenever lookup misses.</param>
    /// <param name="translations">code → text for one locale; null or empty means "nothing to apply".</param>
    public static string Resolve(string? code, string fallbackTitle, IReadOnlyDictionary<string, string>? translations)
    {
        if (string.IsNullOrEmpty(code) || translations is null) return fallbackTitle;

        return translations.TryGetValue(code, out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : fallbackTitle;
    }

    /// <summary>
    /// The text to show for a <b>release</b> task: its own snapshot row for the locale, else the stored
    /// English title. Deliberately does <i>not</i> consult the template — a release's checklist is a
    /// snapshot, so a later template edit must not reach it (the templates screen says as much).
    /// </summary>
    /// <param name="translations">The task's own rows; null/empty means English.</param>
    public static string Resolve(
        IEnumerable<Entities.ReleaseTaskTranslation>? translations, string locale, string fallbackTitle)
    {
        if (translations is null || string.Equals(locale, DefaultLocale, StringComparison.Ordinal))
            return fallbackTitle;

        foreach (var row in translations)
        {
            if (string.Equals(row.Locale, locale, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(row.Text))
                return row.Text;
        }

        return fallbackTitle;
    }
}
