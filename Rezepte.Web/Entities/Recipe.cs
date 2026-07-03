namespace Rezepte.Web.Entities;

public class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    public ICollection<RecipeImage> Images { get; set; } = new List<RecipeImage>();
    public ICollection<RecipeCookbook> RecipeCookbooks { get; set; } = new List<RecipeCookbook>();
    public ICollection<RecipeSideDish> SideDishes { get; set; } = new List<RecipeSideDish>();
    public ICollection<RecipeSideDish> UsedAsSideDishFor { get; set; } = new List<RecipeSideDish>();
    public string? Uri { get; internal set; }
    public int Portions { get; set; } = 0;
}
