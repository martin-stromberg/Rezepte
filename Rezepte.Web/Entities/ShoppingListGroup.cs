namespace Rezepte.Web.Entities;

public class ShoppingListGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RecipeId { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public Recipe? Recipe { get; set; }
    public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
}
