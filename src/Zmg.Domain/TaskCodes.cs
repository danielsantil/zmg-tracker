namespace Zmg.Domain;

/// <summary>
/// The stable slug for each seeded checklist task (v2.8/M47). A task's <i>title</i> is display text
/// that changes per language and can be edited by hand; its <b>code</b> is the identity — it survives
/// translation, a rename in the templates editor, and the template task being renumbered or deleted.
/// </summary>
/// <remarks>
/// Two things depend on this. Translation: <c>TemplateTaskTranslation</c> hangs off a template task,
/// and a release task resolves its per-locale text through the <c>SourceCode</c> it was stamped with.
/// And <c>Release.IsDistributed</c>, which used to compare against the literal English title
/// <c>"Distribute to DSPs"</c> — one Spanish title and the UPC warning, the pending engine and the
/// past-date backfill would all have stopped firing, silently.
/// <para>
/// Codes are <b>permanent identifiers</b>, like the message codes and the integer enums: renaming one
/// orphans every translation row and every already-stamped release task. Tasks a user adds in the
/// editor get a <c>null</c> code and are simply never translated — they're user content.
/// </para>
/// </remarks>
public static class TaskCodes
{
    // ---- Base checklist, shared by both templates (31 tasks) ----

    // Pre
    public const string MixMaster = "mix-master";
    public const string DesignCover = "design-cover";
    /// <summary>Load-bearing: <see cref="Entities.Release.IsDistributed"/> keys off this.</summary>
    public const string DistributeToDsps = "distribute-to-dsps";
    public const string YoutubeVideoAssets = "youtube-video-assets";
    public const string PitchAmazon = "pitch-amazon";
    public const string PitchSpotify = "pitch-spotify";

    // Release
    public const string SmartLink = "smart-link";
    public const string SmartLinkRedirect = "smart-link-redirect";
    public const string RegisterBmi = "register-bmi";
    public const string RegisterMlc = "register-mlc";
    public const string RegisterSoundExchange = "register-soundexchange";
    public const string MusixmatchLyrics = "musixmatch-lyrics";
    public const string CheckDeezer = "check-deezer";
    public const string CheckAmazon = "check-amazon";
    public const string CheckApple = "check-apple";
    public const string SpotifyCanvas = "spotify-canvas";
    public const string SpotifyArtistPick = "spotify-artist-pick";
    public const string YoutubeBanner = "youtube-banner";
    public const string YoutubeHomeVideo = "youtube-home-video";
    public const string YoutubeCards = "youtube-cards";
    public const string YoutubePinnedComment = "youtube-pinned-comment";
    public const string InstagramBioYoutubeLink = "instagram-bio-youtube-link";
    public const string InstagramBioSong = "instagram-bio-song";
    public const string MasterSplits = "master-splits";

    // Post
    public const string MetaAdsInitial = "meta-ads-initial";
    public const string MetaAdsOngoing = "meta-ads-ongoing";
    public const string SpotifyDiscoveryMode = "spotify-discovery-mode";
    public const string YoutubeVideoAds = "youtube-video-ads";
    public const string TiktokAds = "tiktok-ads";
    public const string YoutubeLyricsVideo = "youtube-lyrics-video";
    public const string MultitracksSetup = "multitracks-setup";

    // ---- Album-only extras (10 tasks) ----

    // Pre
    public const string AlbumTracklistSequencing = "album-tracklist-sequencing";
    public const string AlbumIsrcUpcMetadata = "album-isrc-upc-metadata";
    public const string AlbumFocusTracksWaterfall = "album-focus-tracks-waterfall";
    public const string AlbumPreSave = "album-pre-save";
    public const string AlbumBioPressEpk = "album-bio-press-epk";
    public const string AlbumBatchContent = "album-batch-content";
    public const string AlbumPhysicalMedia = "album-physical-media";

    // Release
    public const string AlbumPerTrackRegistrations = "album-per-track-registrations";

    // Post
    public const string AlbumRotateFocusTracks = "album-rotate-focus-tracks";
    public const string AlbumRemainingLyricVideos = "album-remaining-lyric-videos";
}
