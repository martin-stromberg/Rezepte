using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PluginSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Repository = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrustConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecretName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastSuccessfulReleaseTag = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PluginSourceReleases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PluginSourceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ReleaseTag = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GitHubReleaseId = table.Column<long>(type: "INTEGER", nullable: false),
                    AssetId = table.Column<long>(type: "INTEGER", nullable: false),
                    AssetName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstalledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginSourceReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginSourceReleases_PluginSources_PluginSourceId",
                        column: x => x.PluginSourceId,
                        principalTable: "PluginSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PluginSourceReleases_PluginSourceId_ReleaseTag_AssetId",
                table: "PluginSourceReleases",
                columns: new[] { "PluginSourceId", "ReleaseTag", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginSourceReleases_PluginSourceId_Status",
                table: "PluginSourceReleases",
                columns: new[] { "PluginSourceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PluginSources_Enabled",
                table: "PluginSources",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_PluginSources_Owner_Repository",
                table: "PluginSources",
                columns: new[] { "Owner", "Repository" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginSourceReleases");

            migrationBuilder.DropTable(
                name: "PluginSources");
        }
    }
}
