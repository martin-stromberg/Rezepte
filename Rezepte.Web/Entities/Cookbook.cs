namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the cookbook class.
/// </summary>
public class Cookbook
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
    public string? Description { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Reihenfolge-Index für Drag&Drop / Sortierung
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// lists the value.
    /// </summary>
    /// <typeparam name="RecipeCookbook">The recipe cookbook type parameter.</typeparam>
    /// <returns>The result.</returns>
    public ICollection<RecipeCookbook> RecipeCookbooks { get; set; } = new List<RecipeCookbook>();
}
