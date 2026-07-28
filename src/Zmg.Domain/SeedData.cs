using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

/// <summary>
/// The exact initial templates from build-plan.md section 5.4, plus the v1.1 single-template
/// deltas (build-plan-1.1.md M6). Ids are fixed so the EF migration seeds deterministic rows
/// (HasData needs stable keys).
/// </summary>
/// <remarks>
/// Both languages sit on the same line as of v2.9, so a missing translation is visible in the diff
/// rather than a hundred lines away in a parallel dictionary. The reviewed copy of record is
/// <c>plans/seed-checklist-text.md</c> — edit it there, then transcribe.
/// <para>
/// Two rules the copy follows. <b>Domain jargon stays English</b> — DSP, BMI, MLC, SoundExchange,
/// Musixmatch, Canvas, Artist Pick, Discovery Mode are proper nouns, and "smart link", "pre-save",
/// "waterfall", "multitracks", "master", "splits", "focus tracks", "tracklist", "pitch", "streams" and
/// "EPK" are the terms ZMG actually uses in English; translating them makes the checklist harder to
/// read, not easier. And <b>a null Spanish title is a valid state</b> the schema supports for
/// user-added tasks — but no <i>seeded</i> task uses it, which <c>SeedDataTests</c> pins.
/// </para>
/// </remarks>
public static class SeedData
{
    private static readonly Guid SingleTemplateId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AlbumTemplateId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>
    /// English text of the DSP-distribution task, exposed so tests can <i>locate</i> that row in a DTO.
    /// It is <b>just text</b>: identity is <see cref="TaskCodes.DistributeToDsps"/>, and no rule may key
    /// off this — a title is display copy that changes per language and at the user's whim.
    /// </summary>
    public const string DistributeToDspsEn = "Distribute to DSPs";

    // A seeded template task. Code is the stable identity; En/Es are its text. Timeframe (v1.1) is null
    // for all but the two Pre tasks that carry a 7-14 day window.
    private readonly record struct TaskSeed(
        Phase Phase, string Code, string En, string? Es = null,
        int? MinDaysBefore = null, int? MaxDaysBefore = null);

    // The base checklist, shared by both templates (album = this + album extras).
    private static readonly TaskSeed[] BaseTasks =
    [
        new(Phase.Pre, TaskCodes.MixMaster, "Mix/master", "Mezcla/master"),
        new(Phase.Pre, TaskCodes.DesignCover, "Design cover for DSPs", "Diseñar la portada para los DSPs"),
        new(Phase.Pre, TaskCodes.DistributeToDsps, DistributeToDspsEn, "Distribuir a los DSPs", 7, 14), // (v1.1), add 7-14 days window
        new(Phase.Pre, TaskCodes.YoutubeVideoAssets, "Make video for YouTube, thumbnail and additional YouTube resources", "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube"),
        new(Phase.Pre, TaskCodes.PitchAmazon, "Pitch to Amazon", "Pitch a Amazon"),
        new(Phase.Pre, TaskCodes.PitchSpotify, "Pitch to Spotify", "Pitch a Spotify", 7, 14), // (v1.1), add 7-14 days window

        new(Phase.Release, TaskCodes.SmartLink, "Setup smart link to all stores", "Configurar el smart link a todas las tiendas"),
        new(Phase.Release, TaskCodes.SmartLinkRedirect, "Setup smart link redirect from zionmusicgroup.com/<song-name>", "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>"),
        new(Phase.Release, TaskCodes.RegisterBmi, "Register composition to BMI", "Registrar la composición en BMI"),
        new(Phase.Release, TaskCodes.RegisterMlc, "Register composition to MLC", "Registrar la composición en MLC"),
        new(Phase.Release, TaskCodes.RegisterSoundExchange, "Register to SoundExchange", "Registrar en SoundExchange"),
        new(Phase.Release, TaskCodes.MusixmatchLyrics, "Musixmatch lyrics, add/sync", "Letra en Musixmatch: agregar/sincronizar"),
        new(Phase.Release, TaskCodes.CheckDeezer, "Check release in Deezer (wrong artist)", "Revisar el lanzamiento en Deezer (artista equivocado)"),
        new(Phase.Release, TaskCodes.CheckAmazon, "Check release in Amazon (wrong artist)", "Revisar el lanzamiento en Amazon (artista equivocado)"),
        new(Phase.Release, TaskCodes.CheckApple, "Check release in Apple (wrong artist)", "Revisar el lanzamiento en Apple (artista equivocado)"),
        new(Phase.Release, TaskCodes.SpotifyCanvas, "Spotify Canvas", "Spotify: agregar canvas"),
        new(Phase.Release, TaskCodes.SpotifyArtistPick, "Spotify Artist Pick", "Spotify: selección de artista"),
        new(Phase.Release, TaskCodes.YoutubeBanner, "Update YouTube banner", "Actualizar el banner de YouTube"),
        new(Phase.Release, TaskCodes.YoutubeHomeVideo, "Update YouTube home video", "Actualizar el video de inicio en canal de YouTube"),
        new(Phase.Release, TaskCodes.YoutubeCards, "Update cards in existing videos", "Actualizar las tarjetas en los videos existentes"),
        new(Phase.Release, TaskCodes.YoutubePinnedComment, "Update pinned comment in existing videos with link to new video", "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo"),
        new(Phase.Release, TaskCodes.InstagramBioYoutubeLink, "Update YouTube link on Instagram bios", "Actualizar el enlace de YouTube en las bios de Instagram"),
        new(Phase.Release, TaskCodes.InstagramBioSong, "Update song on Instagram bios", "Actualizar la canción en las bios de Instagram"),
        new(Phase.Release, TaskCodes.MasterSplits, "Send master splits to collaborators", "Enviar los splits de master a los colaboradores"),

        new(Phase.Post, TaskCodes.MetaAdsInitial, "Meta ads, initial release campaign", "Meta ads: campaña inicial de lanzamiento"),
        new(Phase.Post, TaskCodes.MetaAdsOngoing, "Meta ads, ongoing campaign", "Meta ads: campaña continua"),
        new(Phase.Post, TaskCodes.SpotifyDiscoveryMode, "Spotify Discovery Mode", "Spotify: campaña Discovery Mode"),
        new(Phase.Post, TaskCodes.YoutubeVideoAds, "YouTube video ads", "Anuncios de video en YouTube"),
        new(Phase.Post, TaskCodes.TiktokAds, "TikTok ads", "Anuncios en TikTok"),
        new(Phase.Post, TaskCodes.YoutubeLyricsVideo, "Create YouTube lyrics video", "Crear el video de letras para YouTube"),
        new(Phase.Post, TaskCodes.MultitracksSetup, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos", "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos")
    ];

    // Album template — the base list plus album-specific work (section 5.4). Untouched by v1.1 (albums out of scope).
    private static readonly TaskSeed[] AlbumExtraTasks =
    [
        new(Phase.Pre, TaskCodes.AlbumTracklistSequencing, "Finalize tracklist and sequencing (locked once submitted to distributor)", "Acordar el tracklist y orden de las canciones (queda fijo al enviarlo a la distribuidora)"),
        new(Phase.Pre, TaskCodes.AlbumIsrcUpcMetadata, "Confirm ISRC/UPC and per-track metadata/credits", "Confirmar ISRC/UPC y la metadata/créditos de cada canción"),
        new(Phase.Pre, TaskCodes.AlbumFocusTracksWaterfall, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)", "Elegir los focus tracks y planear 2-4 sencillos previos al álbum (waterfall: cada sencillo nuevo se reempaqueta con los anteriores y el álbum hereda sus streams)"),
        new(Phase.Pre, TaskCodes.AlbumPreSave, "Album pre-save campaign", "Campaña de pre-save del álbum"),
        new(Phase.Pre, TaskCodes.AlbumBioPressEpk, "Update artist bio / press release / EPK", "Actualizar la biografía del artista / comunicado de prensa / press kits"),
        new(Phase.Pre, TaskCodes.AlbumBatchContent, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)", "Producir contenido por lotes antes de la semana de lanzamiento (comentario canción por canción, videos de letras, versiones acústicas)"),
        new(Phase.Pre, TaskCodes.AlbumPhysicalMedia, "Physical media if applicable (vinyl/CD lead times are months)", "Medios físicos si aplica (los tiempos de producción de vinilo/CD son de meses)"),

        new(Phase.Release, TaskCodes.AlbumPerTrackRegistrations, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track", "Los registros (BMI, MLC, Musixmatch, splits) se repiten por cada canción"),

        new(Phase.Post, TaskCodes.AlbumRotateFocusTracks, "Rotate focus tracks every few weeks with per-track playlist pitching", "Rotar los focus tracks cada pocas semanas con pitching de playlists por canción"),
        new(Phase.Post, TaskCodes.AlbumRemainingLyricVideos, "Lyric videos for remaining tracks", "Videos de letras para las canciones restantes")
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
        return Templates().SelectMany(template => template.Tasks);
    }

    // ---- Authentication (v2.10/M54) ----

    private static readonly Guid BootstrapUserId = new("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// The one seeded whitelist entry, so a freshly migrated database is never locked out (v2.10/M54).
    /// Everyone else is a hand-written <c>INSERT</c> — there is no signup and no admin screen, by
    /// decision, and this exists only to solve the bootstrap: with an empty <c>AllowedUser</c> table
    /// nobody can sign in, including the person who would add the first row.
    /// </summary>
    /// <remarks>
    /// <c>CreatedAt</c> is a fixed instant, not <c>DateTime.UtcNow</c>. <c>HasData</c> values are baked
    /// into the migration and compared against the model snapshot on every scaffold, so a moving value
    /// would make EF detect a model change on every single <c>migrations add</c> — the same class of
    /// drift the M24 audit flagged. It must stay UTC: Npgsql maps <c>DateTimeKind.Utc</c> to
    /// <c>timestamptz</c> and throws on an Unspecified kind.
    /// </remarks>
    public static IEnumerable<AllowedUser> AllowedUsers()
    {
        yield return new AllowedUser
        {
            Id = BootstrapUserId,
            Email = EmailNormalization.Normalize("danielsantilh@gmail.com"),
            DisplayName = null, // filled from the provider profile on first sign-in
            CreatedAt = new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc),
            DisabledAt = null,
        };
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
                TitleEn = seed.En,
                TitleEs = seed.Es,
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
