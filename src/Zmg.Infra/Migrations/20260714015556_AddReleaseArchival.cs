using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseArchival : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Releases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Releases",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Releases");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Releases");
        }
    }
}
