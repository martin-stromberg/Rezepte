namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the recipe image class.
/// </summary>
public class RecipeImage
{
    /// <summary>
    /// guids the value.
    /// </summary>
    /// <returns>The result.</returns>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string RecipeId { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public Recipe Recipe { get; set; } = null!;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    /// <summary>
    /// arrays the value.
    /// </summary>
    /// <returns>The result.</returns>
    public byte[] Data { get; set; } = Array.Empty<byte>();
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Url => $"/api/recipes/{RecipeId}/image/{Id}";
}
