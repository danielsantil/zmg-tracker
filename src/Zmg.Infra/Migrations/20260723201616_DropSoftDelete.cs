using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zmg.Infra.Migrations
{
    /// <inheritdoc />
    public partial class DropSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Releases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Songs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Releases",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
