using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MainArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoverUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Upc = table.Column<string>(type: "text", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Artists_MainArtistId",
                        column: x => x.MainArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Songs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    MainArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Isrc = table.Column<string>(type: "text", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Songs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Songs_Artists_MainArtistId",
                        column: x => x.MainArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecklistTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: false),
                    TitleEs = table.Column<string>(type: "text", nullable: true),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    MinDaysBefore = table.Column<int>(type: "integer", nullable: true),
                    MaxDaysBefore = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateTasks_ChecklistTemplates_ChecklistTemplateId",
                        column: x => x.ChecklistTemplateId,
                        principalTable: "ChecklistTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleEn = table.Column<string>(type: "text", nullable: false),
                    TitleEs = table.Column<string>(type: "text", nullable: true),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MinDaysBefore = table.Column<int>(type: "integer", nullable: true),
                    MaxDaysBefore = table.Column<int>(type: "integer", nullable: true),
                    SourceTemplateTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReleaseTasks_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongArtists",
                columns: table => new
                {
                    SongId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongArtists", x => new { x.SongId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_SongArtists_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SongArtists_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    ReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SongId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackNumber = table.Column<int>(type: "integer", nullable: false),
                    IsFocusTrack = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => new { x.ReleaseId, x.SongId });
                    table.ForeignKey(
                        name: "FK_Tracks_Releases_ReleaseId",
                        column: x => x.ReleaseId,
                        principalTable: "Releases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tracks_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ChecklistTemplates",
                columns: new[] { "Id", "Type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 0 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 1 }
                });

            migrationBuilder.InsertData(
                table: "TemplateTasks",
                columns: new[] { "Id", "ChecklistTemplateId", "Code", "MaxDaysBefore", "MinDaysBefore", "Phase", "SortOrder", "TitleEn", "TitleEs" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111110100"), new Guid("11111111-1111-1111-1111-111111111111"), "mix-master", null, null, 0, 0, "Mix/master", "Mezcla/master" },
                    { new Guid("11111111-1111-1111-1111-111111110140"), new Guid("11111111-1111-1111-1111-111111111111"), "smart-link", null, null, 1, 0, "Setup smart link to all stores", "Configurar el smart link a todas las tiendas" },
                    { new Guid("11111111-1111-1111-1111-111111110180"), new Guid("11111111-1111-1111-1111-111111111111"), "meta-ads-initial", null, null, 2, 0, "Meta ads, initial release campaign", "Meta ads: campaña inicial de lanzamiento" },
                    { new Guid("11111111-1111-1111-1111-111111110201"), new Guid("11111111-1111-1111-1111-111111111111"), "design-cover", null, null, 0, 1, "Design cover for DSPs", "Diseñar la portada para los DSPs" },
                    { new Guid("11111111-1111-1111-1111-111111110241"), new Guid("11111111-1111-1111-1111-111111111111"), "smart-link-redirect", null, null, 1, 1, "Setup smart link redirect from zionmusicgroup.com/<song-name>", "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>" },
                    { new Guid("11111111-1111-1111-1111-111111110281"), new Guid("11111111-1111-1111-1111-111111111111"), "meta-ads-ongoing", null, null, 2, 1, "Meta ads, ongoing campaign", "Meta ads: campaña continua" },
                    { new Guid("11111111-1111-1111-1111-111111110302"), new Guid("11111111-1111-1111-1111-111111111111"), "distribute-to-dsps", 14, 7, 0, 2, "Distribute to DSPs", "Distribuir a los DSPs" },
                    { new Guid("11111111-1111-1111-1111-111111110342"), new Guid("11111111-1111-1111-1111-111111111111"), "register-bmi", null, null, 1, 2, "Register composition to BMI", "Registrar la composición en BMI" },
                    { new Guid("11111111-1111-1111-1111-111111110382"), new Guid("11111111-1111-1111-1111-111111111111"), "spotify-discovery-mode", null, null, 2, 2, "Spotify Discovery Mode", "Spotify: campaña Discovery Mode" },
                    { new Guid("11111111-1111-1111-1111-111111110403"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-video-assets", null, null, 0, 3, "Make video for YouTube, thumbnail and additional YouTube resources", "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube" },
                    { new Guid("11111111-1111-1111-1111-111111110443"), new Guid("11111111-1111-1111-1111-111111111111"), "register-mlc", null, null, 1, 3, "Register composition to MLC", "Registrar la composición en MLC" },
                    { new Guid("11111111-1111-1111-1111-111111110483"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-video-ads", null, null, 2, 3, "YouTube video ads", "Anuncios de video en YouTube" },
                    { new Guid("11111111-1111-1111-1111-111111110504"), new Guid("11111111-1111-1111-1111-111111111111"), "pitch-amazon", null, null, 0, 4, "Pitch to Amazon", "Pitch a Amazon" },
                    { new Guid("11111111-1111-1111-1111-111111110544"), new Guid("11111111-1111-1111-1111-111111111111"), "register-soundexchange", null, null, 1, 4, "Register to SoundExchange", "Registrar en SoundExchange" },
                    { new Guid("11111111-1111-1111-1111-111111110584"), new Guid("11111111-1111-1111-1111-111111111111"), "tiktok-ads", null, null, 2, 4, "TikTok ads", "Anuncios en TikTok" },
                    { new Guid("11111111-1111-1111-1111-111111110605"), new Guid("11111111-1111-1111-1111-111111111111"), "pitch-spotify", 14, 7, 0, 5, "Pitch to Spotify", "Pitch a Spotify" },
                    { new Guid("11111111-1111-1111-1111-111111110645"), new Guid("11111111-1111-1111-1111-111111111111"), "musixmatch-lyrics", null, null, 1, 5, "Musixmatch lyrics, add/sync", "Letra en Musixmatch: agregar/sincronizar" },
                    { new Guid("11111111-1111-1111-1111-111111110685"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-lyrics-video", null, null, 2, 5, "Create YouTube lyrics video", "Crear el video de letras para YouTube" },
                    { new Guid("11111111-1111-1111-1111-111111110746"), new Guid("11111111-1111-1111-1111-111111111111"), "check-deezer", null, null, 1, 6, "Check release in Deezer (wrong artist)", "Revisar el lanzamiento en Deezer (artista equivocado)" },
                    { new Guid("11111111-1111-1111-1111-111111110786"), new Guid("11111111-1111-1111-1111-111111111111"), "multitracks-setup", null, null, 2, 6, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos", "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos" },
                    { new Guid("11111111-1111-1111-1111-111111110847"), new Guid("11111111-1111-1111-1111-111111111111"), "check-amazon", null, null, 1, 7, "Check release in Amazon (wrong artist)", "Revisar el lanzamiento en Amazon (artista equivocado)" },
                    { new Guid("11111111-1111-1111-1111-111111110948"), new Guid("11111111-1111-1111-1111-111111111111"), "check-apple", null, null, 1, 8, "Check release in Apple (wrong artist)", "Revisar el lanzamiento en Apple (artista equivocado)" },
                    { new Guid("11111111-1111-1111-1111-111111110a49"), new Guid("11111111-1111-1111-1111-111111111111"), "spotify-canvas", null, null, 1, 9, "Spotify Canvas", "Spotify: agregar canvas" },
                    { new Guid("11111111-1111-1111-1111-111111110b4a"), new Guid("11111111-1111-1111-1111-111111111111"), "spotify-artist-pick", null, null, 1, 10, "Spotify Artist Pick", "Spotify: selección de artista" },
                    { new Guid("11111111-1111-1111-1111-111111110c4b"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-banner", null, null, 1, 11, "Update YouTube banner", "Actualizar el banner de YouTube" },
                    { new Guid("11111111-1111-1111-1111-111111110d4c"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-home-video", null, null, 1, 12, "Update YouTube home video", "Actualizar el video de inicio en canal de YouTube" },
                    { new Guid("11111111-1111-1111-1111-111111110e4d"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-cards", null, null, 1, 13, "Update cards in existing videos", "Actualizar las tarjetas en los videos existentes" },
                    { new Guid("11111111-1111-1111-1111-111111110f4e"), new Guid("11111111-1111-1111-1111-111111111111"), "youtube-pinned-comment", null, null, 1, 14, "Update pinned comment in existing videos with link to new video", "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo" },
                    { new Guid("11111111-1111-1111-1111-11111111104f"), new Guid("11111111-1111-1111-1111-111111111111"), "instagram-bio-youtube-link", null, null, 1, 15, "Update YouTube link on Instagram bios", "Actualizar el enlace de YouTube en las bios de Instagram" },
                    { new Guid("11111111-1111-1111-1111-111111111150"), new Guid("11111111-1111-1111-1111-111111111111"), "instagram-bio-song", null, null, 1, 16, "Update song on Instagram bios", "Actualizar la canción en las bios de Instagram" },
                    { new Guid("11111111-1111-1111-1111-111111111251"), new Guid("11111111-1111-1111-1111-111111111111"), "master-splits", null, null, 1, 17, "Send master splits to collaborators", "Enviar los splits de master a los colaboradores" },
                    { new Guid("22222222-2222-2222-2222-222222220100"), new Guid("22222222-2222-2222-2222-222222222222"), "mix-master", null, null, 0, 0, "Mix/master", "Mezcla/master" },
                    { new Guid("22222222-2222-2222-2222-222222220140"), new Guid("22222222-2222-2222-2222-222222222222"), "smart-link", null, null, 1, 0, "Setup smart link to all stores", "Configurar el smart link a todas las tiendas" },
                    { new Guid("22222222-2222-2222-2222-222222220180"), new Guid("22222222-2222-2222-2222-222222222222"), "meta-ads-initial", null, null, 2, 0, "Meta ads, initial release campaign", "Meta ads: campaña inicial de lanzamiento" },
                    { new Guid("22222222-2222-2222-2222-222222220201"), new Guid("22222222-2222-2222-2222-222222222222"), "design-cover", null, null, 0, 1, "Design cover for DSPs", "Diseñar la portada para los DSPs" },
                    { new Guid("22222222-2222-2222-2222-222222220241"), new Guid("22222222-2222-2222-2222-222222222222"), "smart-link-redirect", null, null, 1, 1, "Setup smart link redirect from zionmusicgroup.com/<song-name>", "Configurar la redirección del smart link desde zionmusicgroup.com/<song-name>" },
                    { new Guid("22222222-2222-2222-2222-222222220281"), new Guid("22222222-2222-2222-2222-222222222222"), "meta-ads-ongoing", null, null, 2, 1, "Meta ads, ongoing campaign", "Meta ads: campaña continua" },
                    { new Guid("22222222-2222-2222-2222-222222220302"), new Guid("22222222-2222-2222-2222-222222222222"), "distribute-to-dsps", 14, 7, 0, 2, "Distribute to DSPs", "Distribuir a los DSPs" },
                    { new Guid("22222222-2222-2222-2222-222222220342"), new Guid("22222222-2222-2222-2222-222222222222"), "register-bmi", null, null, 1, 2, "Register composition to BMI", "Registrar la composición en BMI" },
                    { new Guid("22222222-2222-2222-2222-222222220382"), new Guid("22222222-2222-2222-2222-222222222222"), "spotify-discovery-mode", null, null, 2, 2, "Spotify Discovery Mode", "Spotify: campaña Discovery Mode" },
                    { new Guid("22222222-2222-2222-2222-222222220403"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-video-assets", null, null, 0, 3, "Make video for YouTube, thumbnail and additional YouTube resources", "Hacer el video para YouTube, la miniatura y los demás recursos de YouTube" },
                    { new Guid("22222222-2222-2222-2222-222222220443"), new Guid("22222222-2222-2222-2222-222222222222"), "register-mlc", null, null, 1, 3, "Register composition to MLC", "Registrar la composición en MLC" },
                    { new Guid("22222222-2222-2222-2222-222222220483"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-video-ads", null, null, 2, 3, "YouTube video ads", "Anuncios de video en YouTube" },
                    { new Guid("22222222-2222-2222-2222-222222220504"), new Guid("22222222-2222-2222-2222-222222222222"), "pitch-amazon", null, null, 0, 4, "Pitch to Amazon", "Pitch a Amazon" },
                    { new Guid("22222222-2222-2222-2222-222222220544"), new Guid("22222222-2222-2222-2222-222222222222"), "register-soundexchange", null, null, 1, 4, "Register to SoundExchange", "Registrar en SoundExchange" },
                    { new Guid("22222222-2222-2222-2222-222222220584"), new Guid("22222222-2222-2222-2222-222222222222"), "tiktok-ads", null, null, 2, 4, "TikTok ads", "Anuncios en TikTok" },
                    { new Guid("22222222-2222-2222-2222-222222220605"), new Guid("22222222-2222-2222-2222-222222222222"), "pitch-spotify", 14, 7, 0, 5, "Pitch to Spotify", "Pitch a Spotify" },
                    { new Guid("22222222-2222-2222-2222-222222220645"), new Guid("22222222-2222-2222-2222-222222222222"), "musixmatch-lyrics", null, null, 1, 5, "Musixmatch lyrics, add/sync", "Letra en Musixmatch: agregar/sincronizar" },
                    { new Guid("22222222-2222-2222-2222-222222220685"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-lyrics-video", null, null, 2, 5, "Create YouTube lyrics video", "Crear el video de letras para YouTube" },
                    { new Guid("22222222-2222-2222-2222-222222220706"), new Guid("22222222-2222-2222-2222-222222222222"), "album-tracklist-sequencing", null, null, 0, 6, "Finalize tracklist and sequencing (locked once submitted to distributor)", "Acordar el tracklist y orden de las canciones (queda fijo al enviarlo a la distribuidora)" },
                    { new Guid("22222222-2222-2222-2222-222222220746"), new Guid("22222222-2222-2222-2222-222222222222"), "check-deezer", null, null, 1, 6, "Check release in Deezer (wrong artist)", "Revisar el lanzamiento en Deezer (artista equivocado)" },
                    { new Guid("22222222-2222-2222-2222-222222220786"), new Guid("22222222-2222-2222-2222-222222222222"), "multitracks-setup", null, null, 2, 6, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos", "Preparar los multitracks: proyecto de Ableton, subida a Google Drive, nueva entrada en zionmusicgroup.com/recursos" },
                    { new Guid("22222222-2222-2222-2222-222222220807"), new Guid("22222222-2222-2222-2222-222222222222"), "album-isrc-upc-metadata", null, null, 0, 7, "Confirm ISRC/UPC and per-track metadata/credits", "Confirmar ISRC/UPC y la metadata/créditos de cada canción" },
                    { new Guid("22222222-2222-2222-2222-222222220847"), new Guid("22222222-2222-2222-2222-222222222222"), "check-amazon", null, null, 1, 7, "Check release in Amazon (wrong artist)", "Revisar el lanzamiento en Amazon (artista equivocado)" },
                    { new Guid("22222222-2222-2222-2222-222222220887"), new Guid("22222222-2222-2222-2222-222222222222"), "album-rotate-focus-tracks", null, null, 2, 7, "Rotate focus tracks every few weeks with per-track playlist pitching", "Rotar los focus tracks cada pocas semanas con pitching de playlists por canción" },
                    { new Guid("22222222-2222-2222-2222-222222220908"), new Guid("22222222-2222-2222-2222-222222222222"), "album-focus-tracks-waterfall", null, null, 0, 8, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)", "Elegir los focus tracks y planear 2-4 sencillos previos al álbum (waterfall: cada sencillo nuevo se reempaqueta con los anteriores y el álbum hereda sus streams)" },
                    { new Guid("22222222-2222-2222-2222-222222220948"), new Guid("22222222-2222-2222-2222-222222222222"), "check-apple", null, null, 1, 8, "Check release in Apple (wrong artist)", "Revisar el lanzamiento en Apple (artista equivocado)" },
                    { new Guid("22222222-2222-2222-2222-222222220988"), new Guid("22222222-2222-2222-2222-222222222222"), "album-remaining-lyric-videos", null, null, 2, 8, "Lyric videos for remaining tracks", "Videos de letras para las canciones restantes" },
                    { new Guid("22222222-2222-2222-2222-222222220a09"), new Guid("22222222-2222-2222-2222-222222222222"), "album-pre-save", null, null, 0, 9, "Album pre-save campaign", "Campaña de pre-save del álbum" },
                    { new Guid("22222222-2222-2222-2222-222222220a49"), new Guid("22222222-2222-2222-2222-222222222222"), "spotify-canvas", null, null, 1, 9, "Spotify Canvas", "Spotify: agregar canvas" },
                    { new Guid("22222222-2222-2222-2222-222222220b0a"), new Guid("22222222-2222-2222-2222-222222222222"), "album-bio-press-epk", null, null, 0, 10, "Update artist bio / press release / EPK", "Actualizar la biografía del artista / comunicado de prensa / press kits" },
                    { new Guid("22222222-2222-2222-2222-222222220b4a"), new Guid("22222222-2222-2222-2222-222222222222"), "spotify-artist-pick", null, null, 1, 10, "Spotify Artist Pick", "Spotify: selección de artista" },
                    { new Guid("22222222-2222-2222-2222-222222220c0b"), new Guid("22222222-2222-2222-2222-222222222222"), "album-batch-content", null, null, 0, 11, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)", "Producir contenido por lotes antes de la semana de lanzamiento (comentario canción por canción, videos de letras, versiones acústicas)" },
                    { new Guid("22222222-2222-2222-2222-222222220c4b"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-banner", null, null, 1, 11, "Update YouTube banner", "Actualizar el banner de YouTube" },
                    { new Guid("22222222-2222-2222-2222-222222220d0c"), new Guid("22222222-2222-2222-2222-222222222222"), "album-physical-media", null, null, 0, 12, "Physical media if applicable (vinyl/CD lead times are months)", "Medios físicos si aplica (los tiempos de producción de vinilo/CD son de meses)" },
                    { new Guid("22222222-2222-2222-2222-222222220d4c"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-home-video", null, null, 1, 12, "Update YouTube home video", "Actualizar el video de inicio en canal de YouTube" },
                    { new Guid("22222222-2222-2222-2222-222222220e4d"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-cards", null, null, 1, 13, "Update cards in existing videos", "Actualizar las tarjetas en los videos existentes" },
                    { new Guid("22222222-2222-2222-2222-222222220f4e"), new Guid("22222222-2222-2222-2222-222222222222"), "youtube-pinned-comment", null, null, 1, 14, "Update pinned comment in existing videos with link to new video", "Actualizar el comentario fijado en los videos existentes con el enlace al video nuevo" },
                    { new Guid("22222222-2222-2222-2222-22222222104f"), new Guid("22222222-2222-2222-2222-222222222222"), "instagram-bio-youtube-link", null, null, 1, 15, "Update YouTube link on Instagram bios", "Actualizar el enlace de YouTube en las bios de Instagram" },
                    { new Guid("22222222-2222-2222-2222-222222221150"), new Guid("22222222-2222-2222-2222-222222222222"), "instagram-bio-song", null, null, 1, 16, "Update song on Instagram bios", "Actualizar la canción en las bios de Instagram" },
                    { new Guid("22222222-2222-2222-2222-222222221251"), new Guid("22222222-2222-2222-2222-222222222222"), "master-splits", null, null, 1, 17, "Send master splits to collaborators", "Enviar los splits de master a los colaboradores" },
                    { new Guid("22222222-2222-2222-2222-222222221352"), new Guid("22222222-2222-2222-2222-222222222222"), "album-per-track-registrations", null, null, 1, 18, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track", "Los registros (BMI, MLC, Musixmatch, splits) se repiten por cada canción" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                table: "Artists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseTasks_ReleaseId",
                table: "ReleaseTasks",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_MainArtistId",
                table: "Releases",
                column: "MainArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_SongArtists_ArtistId",
                table: "SongArtists",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_MainArtistId",
                table: "Songs",
                column: "MainArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Songs_Title",
                table: "Songs",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateTasks_ChecklistTemplateId_Code",
                table: "TemplateTasks",
                columns: new[] { "ChecklistTemplateId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_SongId",
                table: "Tracks",
                column: "SongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseTasks");

            migrationBuilder.DropTable(
                name: "SongArtists");

            migrationBuilder.DropTable(
                name: "TemplateTasks");

            migrationBuilder.DropTable(
                name: "Tracks");

            migrationBuilder.DropTable(
                name: "ChecklistTemplates");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "Songs");

            migrationBuilder.DropTable(
                name: "Artists");
        }
    }
}
