using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAILogType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiRequestLogs_Timestamp",
                table: "AiRequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_AiRequestLogs_UserId",
                table: "AiRequestLogs");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "AiRequestLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_Type_Timestamp",
                table: "AiRequestLogs",
                columns: new[] { "Type", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_UserId_Type_Timestamp",
                table: "AiRequestLogs",
                columns: new[] { "UserId", "Type", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiRequestLogs_Type_Timestamp",
                table: "AiRequestLogs");

            migrationBuilder.DropIndex(
                name: "IX_AiRequestLogs_UserId_Type_Timestamp",
                table: "AiRequestLogs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AiRequestLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_Timestamp",
                table: "AiRequestLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AiRequestLogs_UserId",
                table: "AiRequestLogs",
                column: "UserId");
        }
    }
}
