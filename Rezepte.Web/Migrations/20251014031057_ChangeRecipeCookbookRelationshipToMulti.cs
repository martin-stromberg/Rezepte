using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRecipeCookbookRelationshipToMulti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId",
                table: "Recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId1",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CookbookId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CookbookId1",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CookbookId",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "CookbookId1",
                table: "Recipes");

            migrationBuilder.CreateTable(
                name: "RecipeCookbooks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CookbookId = table.Column<string>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeCookbooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeCookbooks_Cookbooks_CookbookId",
                        column: x => x.CookbookId,
                        principalTable: "Cookbooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeCookbooks_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCookbooks_CookbookId_RecipeId",
                table: "RecipeCookbooks",
                columns: new[] { "CookbookId", "RecipeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeCookbooks_RecipeId",
                table: "RecipeCookbooks",
                column: "RecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeCookbooks");

            migrationBuilder.AddColumn<string>(
                name: "CookbookId",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CookbookId1",
                table: "Recipes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CookbookId",
                table: "Recipes",
                column: "CookbookId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CookbookId1",
                table: "Recipes",
                column: "CookbookId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId",
                table: "Recipes",
                column: "CookbookId",
                principalTable: "Cookbooks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Cookbooks_CookbookId1",
                table: "Recipes",
                column: "CookbookId1",
                principalTable: "Cookbooks",
                principalColumn: "Id");
        }
    }
}
