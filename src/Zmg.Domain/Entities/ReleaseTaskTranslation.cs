namespace Zmg.Domain.Entities;

/// <summary>
/// One release task's text in one non-English locale — the release's **own copy**, stamped by
/// <see cref="TemplateCopy"/> at creation. Composite key <c>(ReleaseTaskId, Locale)</c>, cascading
/// from the task.
/// </summary>
/// <remarks>
/// This exists so the snapshot rule holds in every language. English has always been snapshotted in
/// <see cref="ReleaseTask.Title"/>, but M47 resolved other locales live from the *template's*
/// translation rows via the shared task code — so editing a template's Spanish silently rewrote the
/// Spanish of every release already created from it, while its English stayed put. Copying the text
/// down to the release restores the symmetry: editing a template shapes future releases only, exactly
/// as the templates screen promises.
/// </remarks>
public class ReleaseTaskTranslation
{
    public Guid ReleaseTaskId { get; set; }
    public ReleaseTask? ReleaseTask { get; set; }

    /// <summary>Lowercase, region-free (<c>es</c>) — see <see cref="TaskText.SupportedLocales"/>.</summary>
    public string Locale { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
