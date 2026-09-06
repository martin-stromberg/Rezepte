namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the shopping list item class.
/// </summary>
public class ShoppingListItem
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string GroupId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string? Unit { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IsChecked { get; set; }
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
    public ShoppingListGroup? Group { get; set; }
}
