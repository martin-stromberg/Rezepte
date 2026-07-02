namespace Rezepte.Web.Entities;

public class ShoppingListItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string GroupId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public ShoppingListGroup? Group { get; set; }
}
