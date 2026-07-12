using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zmg.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentifiersAndTaskTimeframes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxDaysBefore",
                table: "TemplateTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinDaysBefore",
                table: "TemplateTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDaysBefore",
                table: "ReleaseTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinDaysBefore",
                table: "ReleaseTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Isrc",
                table: "Releases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Upc",
                table: "Releases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110100"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110140"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110180"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110201"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110241"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110281"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110302"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore", "Title" },
                values: new object[] { 14, 7, "Distribute to DSPs" });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110342"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110382"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110403"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore", "Title" },
                values: new object[] { null, null, "Make video for YouTube, thumbnail and additional YouTube resources" });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110443"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110483"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110504"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore", "Title" },
                values: new object[] { null, null, "Pitch to Amazon" });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110544"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110584"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110645"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110685"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110746"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110786"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110847"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110948"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110a49"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110b4a"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110c4b"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110d4c"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110e4d"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110f4e"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-11111111104f"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111150"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111251"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220100"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220140"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220180"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220201"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220241"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220281"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220302"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220342"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220382"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220403"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220443"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220483"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220504"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220544"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220584"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220605"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220645"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220685"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220706"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220746"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220786"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220807"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220847"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220887"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220908"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220948"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220988"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220a09"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220a49"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220b0a"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220b4a"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220c0b"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220c4b"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220d4c"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220e4d"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222220f4e"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-22222222104f"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221150"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221251"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222221352"),
                columns: new[] { "MaxDaysBefore", "MinDaysBefore" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "TemplateTasks",
                columns: new[] { "Id", "ChecklistTemplateId", "MaxDaysBefore", "MinDaysBefore", "Phase", "SortOrder", "Title" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111110605"), new Guid("11111111-1111-1111-1111-111111111111"), 14, 7, 0, 5, "Pitch to Spotify" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110605"));

            migrationBuilder.DropColumn(
                name: "MaxDaysBefore",
                table: "TemplateTasks");

            migrationBuilder.DropColumn(
                name: "MinDaysBefore",
                table: "TemplateTasks");

            migrationBuilder.DropColumn(
                name: "MaxDaysBefore",
                table: "ReleaseTasks");

            migrationBuilder.DropColumn(
                name: "MinDaysBefore",
                table: "ReleaseTasks");

            migrationBuilder.DropColumn(
                name: "Isrc",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "Upc",
                table: "Releases");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110302"),
                column: "Title",
                value: "Make video for YouTube, thumbnail and additional YouTube resources");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110403"),
                column: "Title",
                value: "Pitch to Amazon");

            migrationBuilder.UpdateData(
                table: "TemplateTasks",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111110504"),
                column: "Title",
                value: "Pitch to Spotify");
        }
    }
}
