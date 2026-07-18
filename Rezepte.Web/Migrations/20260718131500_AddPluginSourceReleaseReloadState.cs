using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rezepte.Web.Data;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(RezepteDbContext))]
    [Migration("20260718131500_AddPluginSourceReleaseReloadState")]
    public partial class AddPluginSourceReleaseReloadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReloadError",
                table: "PluginSourceReleases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReloadedAt",
                table: "PluginSourceReleases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReloadStatus",
                table: "PluginSourceReleases",
                type: "TEXT",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReloadError",
                table: "PluginSourceReleases");

            migrationBuilder.DropColumn(
                name: "ReloadedAt",
                table: "PluginSourceReleases");

            migrationBuilder.DropColumn(
                name: "ReloadStatus",
                table: "PluginSourceReleases");
        }
    }
}
