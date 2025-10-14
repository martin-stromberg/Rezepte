namespace Rezepte.Web.Entities;

public class RecipeCookbook
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string CookbookId { get; set; } = string.Empty;
    public Cookbook Cookbook { get; set; } = null!;
    public string RecipeId { get; set; } = string.Empty;
    public Recipe Recipe { get; set; } = null!;
}
