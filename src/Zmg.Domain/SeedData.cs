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

    /// <summary>
    /// English text of the DSP-distribution task. Since M47 this is <b>just the text</b> — identity
    /// moved to <see cref="TaskCodes.DistributeToDsps"/>, because a title is display copy that changes
    /// per language and a rule can't key off it.
    /// </summary>
    public const string DistributeToDspsTitle = "Distribute to DSPs";

    // A seeded template task. Code is the stable identity (M47); Title is its English text. Timeframe
    // (v1.1) is null for all but the two single-template Pre tasks below.
    private readonly record struct TaskSeed(
        Phase Phase, string Code, string Title, int? MinDaysBefore = null, int? MaxDaysBefore = null);

    // The base checklist, shared by both templates (album = this + album extras). No timeframes here;
    // the single template's v1.1 timeframes and the "Distribute to DSPs" insertion are applied in SingleTasks().
    private static readonly TaskSeed[] BaseTasks =
    [
        new(Phase.Pre, TaskCodes.MixMaster, "Mix/master"),
        new(Phase.Pre, TaskCodes.DesignCover, "Design cover for DSPs"),
        new(Phase.Pre, TaskCodes.DistributeToDsps, DistributeToDspsTitle, 7, 14), // (v1.1), add 7-14 days window
        new(Phase.Pre, TaskCodes.YoutubeVideoAssets, "Make video for YouTube, thumbnail and additional YouTube resources"),
        new(Phase.Pre, TaskCodes.PitchAmazon, "Pitch to Amazon"),
        new(Phase.Pre, TaskCodes.PitchSpotify, "Pitch to Spotify", 7, 14), // (v1.1), add 7-14 days window

        new(Phase.Release, TaskCodes.SmartLink, "Setup smart link to all stores"),
        new(Phase.Release, TaskCodes.SmartLinkRedirect, "Setup smart link redirect from zionmusicgroup.com/<song-name>"),
        new(Phase.Release, TaskCodes.RegisterBmi, "Register composition to BMI"),
        new(Phase.Release, TaskCodes.RegisterMlc, "Register composition to MLC"),
        new(Phase.Release, TaskCodes.RegisterSoundExchange, "Register to SoundExchange"),
        new(Phase.Release, TaskCodes.MusixmatchLyrics, "Musixmatch lyrics, add/sync"),
        new(Phase.Release, TaskCodes.CheckDeezer, "Check release in Deezer (wrong artist)"),
        new(Phase.Release, TaskCodes.CheckAmazon, "Check release in Amazon (wrong artist)"),
        new(Phase.Release, TaskCodes.CheckApple, "Check release in Apple (wrong artist)"),
        new(Phase.Release, TaskCodes.SpotifyCanvas, "Spotify Canvas"),
        new(Phase.Release, TaskCodes.SpotifyArtistPick, "Spotify Artist Pick"),
        new(Phase.Release, TaskCodes.YoutubeBanner, "Update YouTube banner"),
        new(Phase.Release, TaskCodes.YoutubeHomeVideo, "Update YouTube home video"),
        new(Phase.Release, TaskCodes.YoutubeCards, "Update cards in existing videos"),
        new(Phase.Release, TaskCodes.YoutubePinnedComment, "Update pinned comment in existing videos with link to new video"),
        new(Phase.Release, TaskCodes.InstagramBioYoutubeLink, "Update YouTube link on Instagram bios"),
        new(Phase.Release, TaskCodes.InstagramBioSong, "Update song on Instagram bios"),
        new(Phase.Release, TaskCodes.MasterSplits, "Send master splits to collaborators"),

        new(Phase.Post, TaskCodes.MetaAdsInitial, "Meta ads, initial release campaign"),
        new(Phase.Post, TaskCodes.MetaAdsOngoing, "Meta ads, ongoing campaign"),
        new(Phase.Post, TaskCodes.SpotifyDiscoveryMode, "Spotify Discovery Mode"),
        new(Phase.Post, TaskCodes.YoutubeVideoAds, "YouTube video ads"),
        new(Phase.Post, TaskCodes.TiktokAds, "TikTok ads"),
        new(Phase.Post, TaskCodes.YoutubeLyricsVideo, "Create YouTube lyrics video"),
        new(Phase.Post, TaskCodes.MultitracksSetup, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos")
    ];

    // Album template — the base list plus album-specific work (section 5.4). Untouched by v1.1 (albums out of scope).
    private static readonly TaskSeed[] AlbumExtraTasks =
    [
        new(Phase.Pre, TaskCodes.AlbumTracklistSequencing, "Finalize tracklist and sequencing (locked once submitted to distributor)"),
        new(Phase.Pre, TaskCodes.AlbumIsrcUpcMetadata, "Confirm ISRC/UPC and per-track metadata/credits"),
        new(Phase.Pre, TaskCodes.AlbumFocusTracksWaterfall, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)"),
        new(Phase.Pre, TaskCodes.AlbumPreSave, "Album pre-save campaign"),
        new(Phase.Pre, TaskCodes.AlbumBioPressEpk, "Update artist bio / press release / EPK"),
        new(Phase.Pre, TaskCodes.AlbumBatchContent, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)"),
        new(Phase.Pre, TaskCodes.AlbumPhysicalMedia, "Physical media if applicable (vinyl/CD lead times are months)"),

        new(Phase.Release, TaskCodes.AlbumPerTrackRegistrations, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track"),

        new(Phase.Post, TaskCodes.AlbumRotateFocusTracks, "Rotate focus tracks every few weeks with per-track playlist pitching"),
        new(Phase.Post, TaskCodes.AlbumRemainingLyricVideos, "Lyric videos for remaining tracks")
    ];


    /// <summary>
    /// Spanish checklist copy (v2.8/M48), keyed by <see cref="TaskCodes"/> slug. Seeded as
    /// <c>TemplateTaskTranslation</c> rows, so it is deterministic, versioned in the repo and reviewable
    /// in a diff — and, once the templates editor grows a per-locale field, correctable in the app
    /// without a migration.
    /// </summary>
    /// <remarks>
    /// Two deliberate rules. <b>Domain jargon stays English</b> — DSP, BMI, MLC, SoundExchange,
    /// Musixmatch, Canvas, Artist Pick, Discovery Mode are proper nouns, and "smart link", "pre-save",
    /// "waterfall", "multitracks", "master", "splits", "focus tracks", "tracklist", "pitch", "streams"
    /// and "EPK" are the terms ZMG actually uses in English. Translating them would make the checklist
    /// *harder* to read, not easier.
    /// <para>
    /// And <b>a task with no entry here is not a gap</b>: three titles (Spotify Canvas, Spotify Artist
    /// Pick, Spotify Discovery Mode) are proper nouns end to end, so they get no row at all and fall
    /// back to the English column — which is cheaper and more honest than storing a "translation"
    /// identical to its fallback.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string> SpanishTitles = new()
    {
        // Base checklist — Pre
        [TaskCodes.MixMaster] = "Mezcla/master",
        [TaskCodes.DesignCover] = "Diseñar la portada para los DSPs",
        [TaskCodes.DistributeToDsps] = "Distribuir a los DSPs",
        [TaskCodes.YoutubeVideoAssets] = "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube",
        [TaskCodes.PitchAmazon] = "Pitch a Amazon",
        [TaskCodes.PitchSpotify] = "Pitch a Spotify",

        // Base checklist — Release
        [TaskCodes.SmartLink] = "Configurar el smart link a todas las tiendas",
        [TaskCodes.SmartLinkRedirect] = "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>",
        [TaskCodes.RegisterBmi] = "Registrar la composición en BMI",
        [TaskCodes.RegisterMlc] = "Registrar la composición en MLC",
        [TaskCodes.RegisterSoundExchange] = "Registrar en SoundExchange",
        [TaskCodes.MusixmatchLyrics] = "Letra en Musixmatch: agregar/sincronizar",
        [TaskCodes.CheckDeezer] = "Revisar el lanzamiento en Deezer (artista equivocado)",
        [TaskCodes.CheckAmazon] = "Revisar el lanzamiento en Amazon (artista equivocado)",
        [TaskCodes.CheckApple] = "Revisar el lanzamiento en Apple (artista equivocado)",
        // SpotifyCanvas / SpotifyArtistPick: proper nouns, no row — see the remarks above.
        [TaskCodes.YoutubeBanner] = "Actualizar el banner de YouTube",
        [TaskCodes.YoutubeHomeVideo] = "Actualizar el video destacado de YouTube",
        [TaskCodes.YoutubeCards] = "Actualizar las tarjetas en los videos existentes",
        [TaskCodes.YoutubePinnedComment] = "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo",
        [TaskCodes.InstagramBioYoutubeLink] = "Actualizar el enlace de YouTube en las biografías de Instagram",
        [TaskCodes.InstagramBioSong] = "Actualizar la canción en las biografías de Instagram",
        [TaskCodes.MasterSplits] = "Enviar los master splits a los colaboradores",

        // Base checklist — Post
        [TaskCodes.MetaAdsInitial] = "Meta ads: campaña inicial de lanzamiento",
        [TaskCodes.MetaAdsOngoing] = "Meta ads: campaña continua",
        // SpotifyDiscoveryMode: proper noun, no row.
        [TaskCodes.YoutubeVideoAds] = "Anuncios de video en YouTube",
        [TaskCodes.TiktokAds] = "Anuncios en TikTok",
        [TaskCodes.YoutubeLyricsVideo] = "Crear el video con letra para YouTube",
        [TaskCodes.MultitracksSetup] = "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos",

        // Album extras
        [TaskCodes.AlbumTracklistSequencing] = "Cerrar el tracklist y el orden de las canciones (queda fijo al enviarlo a la distribuidora)",
        [TaskCodes.AlbumIsrcUpcMetadata] = "Confirmar ISRC/UPC y los metadatos/créditos de cada canción",
        [TaskCodes.AlbumFocusTracksWaterfall] = "Elegir los focus tracks y planear 2-4 sencillos previos al álbum (waterfall: cada sencillo nuevo se reempaqueta con los anteriores y el álbum hereda sus streams)",
        [TaskCodes.AlbumPreSave] = "Campaña de pre-save del álbum",
        [TaskCodes.AlbumBioPressEpk] = "Actualizar la biografía del artista / el comunicado de prensa / el EPK",
        [TaskCodes.AlbumBatchContent] = "Producir contenido por lotes antes de la semana de lanzamiento (comentario canción por canción, videos con letra, versiones acústicas)",
        [TaskCodes.AlbumPhysicalMedia] = "Medios físicos si aplica (los tiempos de producción de vinilo/CD son de meses)",
        [TaskCodes.AlbumPerTrackRegistrations] = "Los registros (BMI, MLC, Musixmatch, splits) se repiten por cada canción",
        [TaskCodes.AlbumRotateFocusTracks] = "Rotar los focus tracks cada pocas semanas con pitching de playlists por canción",
        [TaskCodes.AlbumRemainingLyricVideos] = "Videos con letra para las canciones restantes",
    };

    /// <summary>
    /// Flat translation rows for EF <c>HasData</c>. One per (template task, locale): the base checklist
    /// is seeded into <i>both</i> templates, so a shared title is written once here and lands as two
    /// rows — the key is <c>(TemplateTaskId, Locale)</c>, which is already deterministic.
    /// </summary>
    public static IEnumerable<TemplateTaskTranslation> AllTemplateTaskTranslations()
    {
        foreach (var task in AllTemplateTasks())
        {
            if (task.Code is { } code && SpanishTitles.TryGetValue(code, out var text))
            {
                yield return new TemplateTaskTranslation
                {
                    TemplateTaskId = task.Id,
                    Locale = "es",
                    Text = text,
                };
            }
        }
    }

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
        return Templates().SelectMany(template => template.Tasks);
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
                Code = seed.Code,
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
