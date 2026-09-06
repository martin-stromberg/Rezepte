using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Represents the airecipe class.
/// </summary>
public class AIRecipe
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int Portions { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    /// <returns>The result.</returns>
    public List<string> Ingredients { get; set; } = new();
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public byte[]? ImageData { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string ImageUri { get; set; } = string.Empty;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int PreparationTimeInMinutes { get; set; }
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int CookingTimeInMinutes { get; set; }
}

/// <summary>
/// Defines the igemini client interface.
/// </summary>
public interface IGeminiClient
{
    /// <summary>
    /// extracts the recipe async.
    /// </summary>
    /// <param name="ocrText">The ocr text parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<AIRecipe[]> ExtractRecipeAsync(string ocrText, CancellationToken ct = default);
    /// <summary>
    /// extracts the recipe from url async.
    /// </summary>
    /// <param name="responseContent">The response content parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<AIRecipe[]> ExtractRecipeFromUrlAsync(string responseContent, CancellationToken ct = default);
    /// <summary>
    /// Determines whether service account.
    /// </summary>
    /// <returns>The result.</returns>
    bool HasServiceAccount();
    /// <summary>
    /// Determines whether api key.
    /// </summary>
    /// <returns>The result.</returns>
    bool HasApiKey();
}
