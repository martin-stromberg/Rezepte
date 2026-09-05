namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the shopping list group class.
/// </summary>
public class ShoppingListGroup
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? RecipeId { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int OrderIndex { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe? Recipe { get; set; }
    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="ShoppingListItem">The shopping list item type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
}
