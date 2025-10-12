namespace Rezepte.Web.Entities;

public class RecipeIngredient
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string StepId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
}
