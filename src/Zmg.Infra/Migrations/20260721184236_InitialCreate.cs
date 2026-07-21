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
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Title = table.Column<string>(type: "text", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    Title = table.Column<string>(type: "text", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    MinDaysBefore = table.Column<int>(type: "integer", nullable: true),
                    MaxDaysBefore = table.Column<int>(type: "integer", nullable: true),
                    SourceTemplateTaskId = table.Column<Guid>(type: "uuid", nullable: true)
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
                columns: new[] { "Id", "ChecklistTemplateId", "MaxDaysBefore", "MinDaysBefore", "Phase", "SortOrder", "Title" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111110100"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 0, 0, "Mix/master" },
                    { new Guid("11111111-1111-1111-1111-111111110140"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 0, "Setup smart link to all stores" },
                    { new Guid("11111111-1111-1111-1111-111111110180"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 0, "Meta ads, initial release campaign" },
                    { new Guid("11111111-1111-1111-1111-111111110201"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 0, 1, "Design cover for DSPs" },
                    { new Guid("11111111-1111-1111-1111-111111110241"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 1, "Setup smart link redirect from zionmusicgroup.com/<song-name>" },
                    { new Guid("11111111-1111-1111-1111-111111110281"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 1, "Meta ads, ongoing campaign" },
                    { new Guid("11111111-1111-1111-1111-111111110302"), new Guid("11111111-1111-1111-1111-111111111111"), 14, 7, 0, 2, "Distribute to DSPs" },
                    { new Guid("11111111-1111-1111-1111-111111110342"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 2, "Register composition to BMI" },
                    { new Guid("11111111-1111-1111-1111-111111110382"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 2, "Spotify Discovery Mode" },
                    { new Guid("11111111-1111-1111-1111-111111110403"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 0, 3, "Make video for YouTube, thumbnail and additional YouTube resources" },
                    { new Guid("11111111-1111-1111-1111-111111110443"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 3, "Register composition to MLC" },
                    { new Guid("11111111-1111-1111-1111-111111110483"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 3, "YouTube video ads" },
                    { new Guid("11111111-1111-1111-1111-111111110504"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 0, 4, "Pitch to Amazon" },
                    { new Guid("11111111-1111-1111-1111-111111110544"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 4, "Register to SoundExchange" },
                    { new Guid("11111111-1111-1111-1111-111111110584"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 4, "TikTok ads" },
                    { new Guid("11111111-1111-1111-1111-111111110605"), new Guid("11111111-1111-1111-1111-111111111111"), 14, 7, 0, 5, "Pitch to Spotify" },
                    { new Guid("11111111-1111-1111-1111-111111110645"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 5, "Musixmatch lyrics, add/sync" },
                    { new Guid("11111111-1111-1111-1111-111111110685"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 5, "Create YouTube lyrics video" },
                    { new Guid("11111111-1111-1111-1111-111111110746"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 6, "Check release in Deezer (wrong artist)" },
                    { new Guid("11111111-1111-1111-1111-111111110786"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 2, 6, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos" },
                    { new Guid("11111111-1111-1111-1111-111111110847"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 7, "Check release in Amazon (wrong artist)" },
                    { new Guid("11111111-1111-1111-1111-111111110948"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 8, "Check release in Apple (wrong artist)" },
                    { new Guid("11111111-1111-1111-1111-111111110a49"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 9, "Spotify Canvas" },
                    { new Guid("11111111-1111-1111-1111-111111110b4a"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 10, "Spotify Artist Pick" },
                    { new Guid("11111111-1111-1111-1111-111111110c4b"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 11, "Update YouTube banner" },
                    { new Guid("11111111-1111-1111-1111-111111110d4c"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 12, "Update YouTube home video" },
                    { new Guid("11111111-1111-1111-1111-111111110e4d"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 13, "Update cards in existing videos" },
                    { new Guid("11111111-1111-1111-1111-111111110f4e"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 14, "Update pinned comment in existing videos with link to new video" },
                    { new Guid("11111111-1111-1111-1111-11111111104f"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 15, "Update YouTube link on Instagram bios" },
                    { new Guid("11111111-1111-1111-1111-111111111150"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 16, "Update song on Instagram bios" },
                    { new Guid("11111111-1111-1111-1111-111111111251"), new Guid("11111111-1111-1111-1111-111111111111"), null, null, 1, 17, "Send master splits to collaborators" },
                    { new Guid("22222222-2222-2222-2222-222222220100"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 0, "Mix/master" },
                    { new Guid("22222222-2222-2222-2222-222222220140"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 0, "Setup smart link to all stores" },
                    { new Guid("22222222-2222-2222-2222-222222220180"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 0, "Meta ads, initial release campaign" },
                    { new Guid("22222222-2222-2222-2222-222222220201"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 1, "Design cover for DSPs" },
                    { new Guid("22222222-2222-2222-2222-222222220241"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 1, "Setup smart link redirect from zionmusicgroup.com/<song-name>" },
                    { new Guid("22222222-2222-2222-2222-222222220281"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 1, "Meta ads, ongoing campaign" },
                    { new Guid("22222222-2222-2222-2222-222222220302"), new Guid("22222222-2222-2222-2222-222222222222"), 14, 7, 0, 2, "Distribute to DSPs" },
                    { new Guid("22222222-2222-2222-2222-222222220342"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 2, "Register composition to BMI" },
                    { new Guid("22222222-2222-2222-2222-222222220382"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 2, "Spotify Discovery Mode" },
                    { new Guid("22222222-2222-2222-2222-222222220403"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 3, "Make video for YouTube, thumbnail and additional YouTube resources" },
                    { new Guid("22222222-2222-2222-2222-222222220443"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 3, "Register composition to MLC" },
                    { new Guid("22222222-2222-2222-2222-222222220483"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 3, "YouTube video ads" },
                    { new Guid("22222222-2222-2222-2222-222222220504"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 4, "Pitch to Amazon" },
                    { new Guid("22222222-2222-2222-2222-222222220544"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 4, "Register to SoundExchange" },
                    { new Guid("22222222-2222-2222-2222-222222220584"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 4, "TikTok ads" },
                    { new Guid("22222222-2222-2222-2222-222222220605"), new Guid("22222222-2222-2222-2222-222222222222"), 14, 7, 0, 5, "Pitch to Spotify" },
                    { new Guid("22222222-2222-2222-2222-222222220645"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 5, "Musixmatch lyrics, add/sync" },
                    { new Guid("22222222-2222-2222-2222-222222220685"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 5, "Create YouTube lyrics video" },
                    { new Guid("22222222-2222-2222-2222-222222220706"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 6, "Finalize tracklist and sequencing (locked once submitted to distributor)" },
                    { new Guid("22222222-2222-2222-2222-222222220746"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 6, "Check release in Deezer (wrong artist)" },
                    { new Guid("22222222-2222-2222-2222-222222220786"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 6, "Set up multitracks: Ableton project, Google Drive upload, new entry in zionmusicgroup.com/recursos" },
                    { new Guid("22222222-2222-2222-2222-222222220807"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 7, "Confirm ISRC/UPC and per-track metadata/credits" },
                    { new Guid("22222222-2222-2222-2222-222222220847"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 7, "Check release in Amazon (wrong artist)" },
                    { new Guid("22222222-2222-2222-2222-222222220887"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 7, "Rotate focus tracks every few weeks with per-track playlist pitching" },
                    { new Guid("22222222-2222-2222-2222-222222220908"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 8, "Pick focus tracks and plan 2-4 pre-release singles (waterfall: each new single re-packaged with prior ones, album inherits their streams)" },
                    { new Guid("22222222-2222-2222-2222-222222220948"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 8, "Check release in Apple (wrong artist)" },
                    { new Guid("22222222-2222-2222-2222-222222220988"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 2, 8, "Lyric videos for remaining tracks" },
                    { new Guid("22222222-2222-2222-2222-222222220a09"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 9, "Album pre-save campaign" },
                    { new Guid("22222222-2222-2222-2222-222222220a49"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 9, "Spotify Canvas" },
                    { new Guid("22222222-2222-2222-2222-222222220b0a"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 10, "Update artist bio / press release / EPK" },
                    { new Guid("22222222-2222-2222-2222-222222220b4a"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 10, "Spotify Artist Pick" },
                    { new Guid("22222222-2222-2222-2222-222222220c0b"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 11, "Batch-produce content before release week (track-by-track commentary, lyric videos, acoustic cuts)" },
                    { new Guid("22222222-2222-2222-2222-222222220c4b"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 11, "Update YouTube banner" },
                    { new Guid("22222222-2222-2222-2222-222222220d0c"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 0, 12, "Physical media if applicable (vinyl/CD lead times are months)" },
                    { new Guid("22222222-2222-2222-2222-222222220d4c"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 12, "Update YouTube home video" },
                    { new Guid("22222222-2222-2222-2222-222222220e4d"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 13, "Update cards in existing videos" },
                    { new Guid("22222222-2222-2222-2222-222222220f4e"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 14, "Update pinned comment in existing videos with link to new video" },
                    { new Guid("22222222-2222-2222-2222-22222222104f"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 15, "Update YouTube link on Instagram bios" },
                    { new Guid("22222222-2222-2222-2222-222222221150"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 16, "Update song on Instagram bios" },
                    { new Guid("22222222-2222-2222-2222-222222221251"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 17, "Send master splits to collaborators" },
                    { new Guid("22222222-2222-2222-2222-222222221352"), new Guid("22222222-2222-2222-2222-222222222222"), null, null, 1, 18, "Registrations (BMI, MLC, Musixmatch, splits) repeat per track" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                table: "Artists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_MainArtistId",
                table: "Releases",
                column: "MainArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleaseTasks_ReleaseId",
                table: "ReleaseTasks",
                column: "ReleaseId");

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
                name: "IX_TemplateTasks_ChecklistTemplateId",
                table: "TemplateTasks",
                column: "ChecklistTemplateId");

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
