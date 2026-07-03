namespace Rezepte.Web.Entities;

public class RecipeSideDish
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string RecipeId { get; set; } = string.Empty;
    public string SideDishRecipeId { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public Recipe? Recipe { get; set; }
    public Recipe? SideDishRecipe { get; set; }
}
