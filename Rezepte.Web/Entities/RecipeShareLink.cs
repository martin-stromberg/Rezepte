namespace Rezepte.Web.Entities;

/// <summary>
/// Represents a public, time-limited share link for a single recipe.
/// </summary>
public class RecipeShareLink
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string RecipeId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe? Recipe { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool IncludeSideDishes { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
