namespace Rezepte.Web.Entities;

public class RecipeStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string RecipeId { get; set; } = string.Empty;
    public int StepIndex { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool RequiresOvernightRest { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
