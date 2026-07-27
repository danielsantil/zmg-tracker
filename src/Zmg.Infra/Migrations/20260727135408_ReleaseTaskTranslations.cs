using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ReleaseTaskTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseTaskTranslations",
                columns: table => new
                {
                    ReleaseTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseTaskTranslations", x => new { x.ReleaseTaskId, x.Locale });
                    table.ForeignKey(
                        name: "FK_ReleaseTaskTranslations_ReleaseTasks_ReleaseTaskId",
                        column: x => x.ReleaseTaskId,
                        principalTable: "ReleaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Give every existing release the snapshot it should always have had, taken from the
            // template task it was copied from. Joining on SourceTemplateTaskId rather than the shared
            // code is deliberate: the base checklist is seeded into *both* templates, so a code join
            // would match two rows per task and violate the composite PK.
            //
            // A task the user already renamed is skipped: renaming nulls SourceCode (it's theirs now,
            // one text in every language), but leaves SourceTemplateTaskId intact — so joining on the
            // GUID alone would hand a custom task a translation and split it back into two texts.
            //
            // This freezes whatever the template says right now, so any template text edited since a
            // release was created is adopted once, here — the alternative is inventing an original the
            // schema never recorded. Standard INSERT…SELECT, so it runs on SQLite (where the tests get
            // their schema) as well as Postgres.
            migrationBuilder.Sql("""
                INSERT INTO "ReleaseTaskTranslations" ("ReleaseTaskId", "Locale", "Text")
                SELECT rt."Id", tt."Locale", tt."Text"
                FROM "ReleaseTasks" rt
                JOIN "TemplateTaskTranslations" tt ON tt."TemplateTaskId" = rt."SourceTemplateTaskId"
                WHERE rt."SourceTemplateTaskId" IS NOT NULL
                  AND rt."SourceCode" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseTaskTranslations");
        }
    }
}
