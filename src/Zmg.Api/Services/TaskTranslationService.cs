using Microsoft.EntityFrameworkCore;
using Zmg.Api.Services.Interfaces;
using Zmg.Domain;
using Zmg.Infra.Data;

namespace Zmg.Api.Services;

/// <summary>
/// Loads the checklist-text overrides for the request's locale, keyed by <see cref="TaskCodes"/> slug
/// (v2.8/M47). Scoped, with a per-request memo — a single release detail resolves ~31 tasks and a
/// templates page ~72, and they all want the same map.
/// </summary>
/// <remarks>
/// Keyed by <b>code</b>, not by template-task id, deliberately: a release task carries
/// <c>SourceCode</c> rather than a live FK, so lookup has to work without the template task still
/// existing. The base checklist is seeded into both templates, so two rows share each base code —
/// same text by construction, and the map keeps whichever it sees first.
/// <para>
/// <c>en</c> short-circuits to an empty map: English is the <c>Title</c> column, never a translation
/// row, so there is nothing to query and nothing to override.
/// </para>
/// </remarks>
public sealed class TaskTranslationService(ZmgDbContext db, ILocaleAccessor locale) : ITaskTranslationService
{
    private IReadOnlyDictionary<string, string>? _cached;

    public string Locale => locale.Locale;

    public async Task<IReadOnlyDictionary<string, string>> ForRequestLocaleAsync(CancellationToken ct = default)
    {
        if (_cached is not null) return _cached;

        if (locale.Locale == TaskText.DefaultLocale)
            return _cached = new Dictionary<string, string>(0);

        var rows = await db.TemplateTaskTranslations
            .AsNoTracking()
            .Where(t => t.Locale == locale.Locale && t.TemplateTask!.Code != null)
            .Select(t => new { Code = t.TemplateTask!.Code!, t.Text })
            .ToListAsync(ct);

        var map = new Dictionary<string, string>(rows.Count, StringComparer.Ordinal);
        foreach (var row in rows) map.TryAdd(row.Code, row.Text);
        return _cached = map;
    }
}

/// <summary>
/// The locale for the current request: <c>X-Lang</c> first (the SPA sets it from <c>i18n.language</c>,
/// so it always matches what the user is actually reading), then <c>Accept-Language</c>, then English.
/// </summary>
public sealed class LocaleAccessor(IHttpContextAccessor accessor) : ILocaleAccessor
{
    private string? _resolved;

    public string Locale => _resolved ??= Resolve();

    private string Resolve()
    {
        var headers = accessor.HttpContext?.Request.Headers;
        if (headers is null) return TaskText.DefaultLocale;

        var explicitLang = headers["X-Lang"].ToString();
        return string.IsNullOrWhiteSpace(explicitLang)
            ? TaskText.NormalizeLocale(headers.AcceptLanguage.ToString())
            : TaskText.NormalizeLocale(explicitLang);
    }
}
