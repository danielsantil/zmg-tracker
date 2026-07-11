namespace Zmg.Domain;

/// <summary>
/// The exact initial templates from build-plan.md section 5.4. Ids are fixed so the
/// EF migration seeds deterministic rows (HasData needs stable keys).
/// </summary>
public static class SeedData
{
    public static readonly Guid SingleTemplateId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AlbumTemplateId = new("22222222-2222-2222-2222-222222222222");

    // Single template — seeded verbatim from the current checklist.
    private static readonly (Phase Phase, string Title)[] SingleTasks =
    {
        (Phase.Pre, "Mix/master"),
        (Phase.Pre, "Design cover for DSPs"),
        (Phase.Pre, "Make video for YouTube, thumbnail and additional YouTube resources"),
        (Phase.Pre, "Pitch to Amazon"),
        (Phase.Pre, "Pitch to Spotify"),

        (Phase.Release, "Setup smart link to all stores"),
        (Phase.Release, "Setup smart link redirect from zionmusicgroup.com/<song-name>"),
        (Phase.Release, "Register composition to BMI"),
        (Phase.Release, "Register composition to MLC"),
        (Phase.Release, "Register to SoundExchange"),
        (Phase.Release, "Musixmatch lyrics, add/sync"),
        (Phase.Release, "Check release in Deezer (wrong artist)"),
        (Phase.Release, "Check release in Amazon (wrong artist)"),
        (Phase.Release, "Check release in Apple (wrong artist)"),
        (Phase.Release, "Spotify Canvas"),
        (Phase.Release, "Spotify Artist Pick"),
        (Phase.Release, "Update YouTube banner"),
        (Phase.Release, "Update YouTube home video"),
        (Phase.Release, "Update cards in existing videos"),
        (Phase.Release, "Update pinned comment in existing videos with link to new video"),
        (Phase.Release, "Update YouTube link on Instagram bios"),
        (Phase.Release, "Update song on Instagram bios"),
        (Phase.Release, "Send master splits to collaborators"),

        (Phase.Post, "Meta ads, initial release campaign"),
        (Phase.Post, "Meta ads, ongoing campaign"),
        (Phase.Post, "Spotify Discovery Mode"),
        (Phase.Post, "YouTube video ads"),
        (Phase.Post, "TikTok ads"),
        (Phase.Post, "Create YouTube lyrics video"),
        (Phase.Post, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos"),
    };

    // Album template — the single list plus album-specific work (section 5.4).
    private static readonly (Phase Phase, string Title)[] AlbumExtraTasks =
    {
        (Phase.Pre, "Finalize tracklist and sequencing (locked once submitted to distributor)"),
        (Phase.Pre, "Confirm ISRC/UPC and per-track metadata/credits"),
        (Phase.Pre, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)"),
        (Phase.Pre, "Album pre-save campaign"),
        (Phase.Pre, "Update artist bio / press release / EPK"),
        (Phase.Pre, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)"),
        (Phase.Pre, "Physical media if applicable (vinyl/CD lead times are months)"),

        (Phase.Release, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track"),

        (Phase.Post, "Rotate focus tracks every few weeks with per-track playlist pitching"),
        (Phase.Post, "Lyric videos for remaining tracks"),
    };

    public static IReadOnlyList<ChecklistTemplate> Templates()
    {
        return new[]
        {
            BuildTemplate(SingleTemplateId, ReleaseType.Single, SingleTasks),
            BuildTemplate(AlbumTemplateId, ReleaseType.Album, SingleTasks.Concat(AlbumExtraTasks)),
        };
    }

    /// <summary>Flat (templateId, task) rows for EF <c>HasData</c> seeding with deterministic ids.</summary>
    public static IEnumerable<TemplateTask> AllTemplateTasks()
    {
        foreach (var template in Templates())
            foreach (var task in template.Tasks)
                yield return task;
    }

    private static ChecklistTemplate BuildTemplate(
        Guid templateId, ReleaseType type, IEnumerable<(Phase Phase, string Title)> tasks)
    {
        var template = new ChecklistTemplate { Id = templateId, Type = type };
        var perPhaseOrder = new Dictionary<Phase, int>();

        foreach (var (phase, title) in tasks)
        {
            int order = perPhaseOrder.TryGetValue(phase, out var current) ? current : 0;
            perPhaseOrder[phase] = order + 1;

            template.Tasks.Add(new TemplateTask
            {
                Id = DeterministicTaskId(templateId, phase, order),
                ChecklistTemplateId = templateId,
                Title = title,
                Phase = phase,
                SortOrder = order,
            });
        }

        return template;
    }

    // Deterministic GUID per (template, phase, order) so re-running migrations is stable.
    private static Guid DeterministicTaskId(Guid templateId, Phase phase, int order)
    {
        var bytes = templateId.ToByteArray();
        bytes[15] = (byte)(((int)phase << 6) ^ (order & 0x3F));
        bytes[14] = (byte)(order + 1);
        return new Guid(bytes);
    }
}
