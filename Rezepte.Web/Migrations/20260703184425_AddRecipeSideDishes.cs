using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeSideDishes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeSideDishes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RecipeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SideDishRecipeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeSideDishes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeSideDishes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecipeSideDishes_Recipes_SideDishRecipeId",
                        column: x => x.SideDishRecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSideDishes_RecipeId_SideDishRecipeId",
                table: "RecipeSideDishes",
                columns: new[] { "RecipeId", "SideDishRecipeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSideDishes_SideDishRecipeId",
                table: "RecipeSideDishes",
                column: "SideDishRecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeSideDishes");
        }
    }
}
