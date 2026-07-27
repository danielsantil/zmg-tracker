namespace Zmg.Domain.Entities;

/// <summary>
/// One template task's text in one non-English locale (v2.8/M47). Composite key
/// <c>(TemplateTaskId, Locale)</c>, cascading from the template task.
/// </summary>
/// <remarks>
/// A child table rather than a <c>jsonb</c> column on <see cref="TemplateTask"/>: it is
/// provider-agnostic and indexable on both providers, and the integration tests run <b>SQLite</b>,
/// where <c>jsonb</c> querying isn't available. English is not stored here — it lives in
/// <see cref="TemplateTask.Title"/>, so <c>en</c> needs no rows and the fallback path is the column
/// (see <see cref="TaskText"/>).
/// </remarks>
public class TemplateTaskTranslation
{
    public Guid TemplateTaskId { get; set; }
    public TemplateTask? TemplateTask { get; set; }

    /// <summary>Lowercase, region-free (<c>es</c>, never <c>es-ES</c>) — see <see cref="TaskText.SupportedLocales"/>.</summary>
    public string Locale { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
