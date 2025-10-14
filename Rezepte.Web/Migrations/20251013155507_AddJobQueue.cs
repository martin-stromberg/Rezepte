using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddJobQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CookbookId1",
                table: "Recipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CookbookId1",
                table: "Recipes",
                column: "CookbookId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId1",
                table: "Recipes",
                column: "CookbookId1",
                principalTable: "Cookbooks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId1",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CookbookId1",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CookbookId1",
                table: "Recipes");
        }
    }
}
