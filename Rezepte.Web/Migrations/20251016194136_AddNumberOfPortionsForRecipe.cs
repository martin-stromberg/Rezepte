using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rezepte.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberOfPortionsForRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Portions",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Portions",
                table: "Recipes");
        }
    }
}
