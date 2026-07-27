using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class TaskCodesAndTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateTasks_ChecklistTemplateId",
                table: "TemplateTasks");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TemplateTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "ReleaseTasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TemplateTaskTranslations",
                columns: table => new
                {
                    TemplateTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateTaskTranslations", x => new { x.TemplateTaskId, x.Locale });
                    table.ForeignKey(
                        name: "FK_TemplateTaskTranslations_TemplateTasks_TemplateTaskId",
                        column: x => x.TemplateTaskId,
                        principalTable: "TemplateTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110100"),
                column: "Code",
                value: "mix-master");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110140"),
                column: "Code",
                value: "smart-link");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110180"),
                column: "Code",
                value: "meta-ads-initial");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110201"),
                column: "Code",
                value: "design-cover");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110241"),
                column: "Code",
                value: "smart-link-redirect");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110281"),
                column: "Code",
                value: "meta-ads-ongoing");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110302"),
                column: "Code",
                value: "distribute-to-dsps");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110342"),
                column: "Code",
                value: "register-bmi");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110382"),
                column: "Code",
                value: "spotify-discovery-mode");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110403"),
                column: "Code",
                value: "youtube-video-assets");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110443"),
                column: "Code",
                value: "register-mlc");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110483"),
                column: "Code",
                value: "youtube-video-ads");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110504"),
                column: "Code",
                value: "pitch-amazon");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110544"),
                column: "Code",
                value: "register-soundexchange");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110584"),
                column: "Code",
                value: "tiktok-ads");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110605"),
                column: "Code",
                value: "pitch-spotify");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110645"),
                column: "Code",
                value: "musixmatch-lyrics");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110685"),
                column: "Code",
                value: "youtube-lyrics-video");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110746"),
                column: "Code",
                value: "check-deezer");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110786"),
                column: "Code",
                value: "multitracks-setup");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110847"),
                column: "Code",
                value: "check-amazon");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110948"),
                column: "Code",
                value: "check-apple");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110a49"),
                column: "Code",
                value: "spotify-canvas");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110b4a"),
                column: "Code",
                value: "spotify-artist-pick");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110c4b"),
                column: "Code",
                value: "youtube-banner");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110d4c"),
                column: "Code",
                value: "youtube-home-video");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110e4d"),
                column: "Code",
                value: "youtube-cards");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110f4e"),
                column: "Code",
                value: "youtube-pinned-comment");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111104f"),
                column: "Code",
                value: "instagram-bio-youtube-link");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111150"),
                column: "Code",
                value: "instagram-bio-song");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111251"),
                column: "Code",
                value: "master-splits");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220100"),
                column: "Code",
                value: "mix-master");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220140"),
                column: "Code",
                value: "smart-link");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220180"),
                column: "Code",
                value: "meta-ads-initial");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220201"),
                column: "Code",
                value: "design-cover");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220241"),
                column: "Code",
                value: "smart-link-redirect");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220281"),
                column: "Code",
                value: "meta-ads-ongoing");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220302"),
                column: "Code",
                value: "distribute-to-dsps");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220342"),
                column: "Code",
                value: "register-bmi");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220382"),
                column: "Code",
                value: "spotify-discovery-mode");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220403"),
                column: "Code",
                value: "youtube-video-assets");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220443"),
                column: "Code",
                value: "register-mlc");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220483"),
                column: "Code",
                value: "youtube-video-ads");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220504"),
                column: "Code",
                value: "pitch-amazon");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220544"),
                column: "Code",
                value: "register-soundexchange");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220584"),
                column: "Code",
                value: "tiktok-ads");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220605"),
                column: "Code",
                value: "pitch-spotify");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220645"),
                column: "Code",
                value: "musixmatch-lyrics");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220685"),
                column: "Code",
                value: "youtube-lyrics-video");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220706"),
                column: "Code",
                value: "album-tracklist-sequencing");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220746"),
                column: "Code",
                value: "check-deezer");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220786"),
                column: "Code",
                value: "multitracks-setup");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220807"),
                column: "Code",
                value: "album-isrc-upc-metadata");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220847"),
                column: "Code",
                value: "check-amazon");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220887"),
                column: "Code",
                value: "album-rotate-focus-tracks");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220908"),
                column: "Code",
                value: "album-focus-tracks-waterfall");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220948"),
                column: "Code",
                value: "check-apple");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220988"),
                column: "Code",
                value: "album-remaining-lyric-videos");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220a09"),
                column: "Code",
                value: "album-pre-save");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220a49"),
                column: "Code",
                value: "spotify-canvas");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220b0a"),
                column: "Code",
                value: "album-bio-press-epk");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220b4a"),
                column: "Code",
                value: "spotify-artist-pick");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220c0b"),
                column: "Code",
                value: "album-batch-content");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220c4b"),
                column: "Code",
                value: "youtube-banner");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220d0c"),
                column: "Code",
                value: "album-physical-media");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220d4c"),
                column: "Code",
                value: "youtube-home-video");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220e4d"),
                column: "Code",
                value: "youtube-cards");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220f4e"),
                column: "Code",
                value: "youtube-pinned-comment");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-22222222104f"),
                column: "Code",
                value: "instagram-bio-youtube-link");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221150"),
                column: "Code",
                value: "instagram-bio-song");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221251"),
                column: "Code",
                value: "master-splits");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221352"),
                column: "Code",
                value: "album-per-track-registrations");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateTasks_ChecklistTemplateId_Code",
                table: "TemplateTasks",
                columns: new[] { "ChecklistTemplateId", "Code" });

            // Backfill SourceCode on every release task already in the database, by joining the lineage
            // GUID TemplateCopy has always written to the template task this migration just coded.
            // Without this, every pre-existing release silently reports NOT distributed — IsDistributed
            // keys off SourceCode now — which takes the UPC warning, the pending engine and the
            // past-date backfill down with it. The UpdateData block above runs first, so the codes are
            // there by the time this reads them. Tasks the user added by hand have a null
            // SourceTemplateTaskId and correctly stay null: they are user content, never translated.
            // A correlated subquery rather than UPDATE…FROM: migrations are Postgres-specific, but the
            // API integration tests get their schema by running them against SQLite, so hand-written
            // SQL has to be portable. This form is standard on both.
            migrationBuilder.Sql("""
                UPDATE "ReleaseTasks"
                SET "SourceCode" = (
                    SELECT "TemplateTasks"."Code"
                    FROM "TemplateTasks"
                    WHERE "TemplateTasks"."Id" = "ReleaseTasks"."SourceTemplateTaskId"
                )
                WHERE "SourceTemplateTaskId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateTaskTranslations");

            migrationBuilder.DropIndex(
                name: "IX_TemplateTasks_ChecklistTemplateId_Code",
                table: "TemplateTasks");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TemplateTasks");

            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "ReleaseTasks");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateTasks_ChecklistTemplateId",
                table: "TemplateTasks",
                column: "ChecklistTemplateId");
        }
    }
}
