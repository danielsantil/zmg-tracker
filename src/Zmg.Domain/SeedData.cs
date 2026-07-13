using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// The exact initial templates from build-plan.md section 5.4, plus the v1.1 single-template
/// deltas (build-plan-1.1.md M6). Ids are fixed so the EF migration seeds deterministic rows
/// (HasData needs stable keys).
/// </summary>
public static class SeedData
{
    private static readonly Guid SingleTemplateId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AlbumTemplateId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>Title of the DSP-distribution task; drives the v1.1 identifier warning and pending logic.</summary>
    public const string DistributeToDspsTitle = "Distribute to DSPs";

    // A seeded template task. Timeframe (v1.1) is null for all but the two single-template Pre tasks below.
    private readonly record struct TaskSeed(Phase Phase, string Title, int? MinDaysBefore = null, int? MaxDaysBefore = null);

    // The base checklist, shared by both templates (album = this + album extras). No timeframes here;
    // the single template's v1.1 timeframes and the "Distribute to DSPs" insertion are applied in SingleTasks().
    private static readonly TaskSeed[] BaseTasks =
    [
        new(Phase.Pre, "Mix/master"),
        new(Phase.Pre, "Design cover for DSPs"),
        new(Phase.Pre, DistributeToDspsTitle, 7, 14), // (v1.1), add 7-14 days window
        new(Phase.Pre, "Make video for YouTube, thumbnail and additional YouTube resources"),
        new(Phase.Pre, "Pitch to Amazon"),
        new(Phase.Pre, "Pitch to Spotify", 7, 14), // (v1.1), add 7-14 days window

        new(Phase.Release, "Setup smart link to all stores"),
        new(Phase.Release, "Setup smart link redirect from zionmusicgroup.com/<song-name>"),
        new(Phase.Release, "Register composition to BMI"),
        new(Phase.Release, "Register composition to MLC"),
        new(Phase.Release, "Register to SoundExchange"),
        new(Phase.Release, "Musixmatch lyrics, add/sync"),
        new(Phase.Release, "Check release in Deezer (wrong artist)"),
        new(Phase.Release, "Check release in Amazon (wrong artist)"),
        new(Phase.Release, "Check release in Apple (wrong artist)"),
        new(Phase.Release, "Spotify Canvas"),
        new(Phase.Release, "Spotify Artist Pick"),
        new(Phase.Release, "Update YouTube banner"),
        new(Phase.Release, "Update YouTube home video"),
        new(Phase.Release, "Update cards in existing videos"),
        new(Phase.Release, "Update pinned comment in existing videos with link to new video"),
        new(Phase.Release, "Update YouTube link on Instagram bios"),
        new(Phase.Release, "Update song on Instagram bios"),
        new(Phase.Release, "Send master splits to collaborators"),

        new(Phase.Post, "Meta ads, initial release campaign"),
        new(Phase.Post, "Meta ads, ongoing campaign"),
        new(Phase.Post, "Spotify Discovery Mode"),
        new(Phase.Post, "YouTube video ads"),
        new(Phase.Post, "TikTok ads"),
        new(Phase.Post, "Create YouTube lyrics video"),
        new(Phase.Post, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos")
    ];

    // Album template — the base list plus album-specific work (section 5.4). Untouched by v1.1 (albums out of scope).
    private static readonly TaskSeed[] AlbumExtraTasks =
    [
        new(Phase.Pre, "Finalize tracklist and sequencing (locked once submitted to distributor)"),
        new(Phase.Pre, "Confirm ISRC/UPC and per-track metadata/credits"),
        new(Phase.Pre, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)"),
        new(Phase.Pre, "Album pre-save campaign"),
        new(Phase.Pre, "Update artist bio / press release / EPK"),
        new(Phase.Pre, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)"),
        new(Phase.Pre, "Physical media if applicable (vinyl/CD lead times are months)"),

        new(Phase.Release, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track"),

        new(Phase.Post, "Rotate focus tracks every few weeks with per-track playlist pitching"),
        new(Phase.Post, "Lyric videos for remaining tracks")
    ];

    public static IReadOnlyList<ChecklistTemplate> Templates()
    {
        return
        [
            BuildTemplate(SingleTemplateId, ReleaseType.Single, BaseTasks),
            BuildTemplate(AlbumTemplateId, ReleaseType.Album, BaseTasks.Concat(AlbumExtraTasks))
        ];
    }

    /// <summary>Flat (templateId, task) rows for EF <c>HasData</c> seeding with deterministic ids.</summary>
    public static IEnumerable<TemplateTask> AllTemplateTasks()
    {
        foreach (var template in Templates())
            foreach (var task in template.Tasks)
                yield return task;
    }

    private static ChecklistTemplate BuildTemplate(
        Guid templateId, ReleaseType type, IEnumerable<TaskSeed> tasks)
    {
        var template = new ChecklistTemplate { Id = templateId, Type = type };
        var perPhaseOrder = new Dictionary<Phase, int>();

        foreach (var seed in tasks)
        {
            int order = perPhaseOrder.GetValueOrDefault(seed.Phase, 0);
            perPhaseOrder[seed.Phase] = order + 1;

            template.Tasks.Add(new TemplateTask
            {
                Id = DeterministicTaskId(templateId, seed.Phase, order),
                ChecklistTemplateId = templateId,
                Title = seed.Title,
                Phase = seed.Phase,
                SortOrder = order,
                MinDaysBefore = seed.MinDaysBefore,
                MaxDaysBefore = seed.MaxDaysBefore,
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
