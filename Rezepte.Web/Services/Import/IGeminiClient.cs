using System.Threading;
using System.Threading.Tasks;

namespace Rezepte.Web.Services.Import;

public class AIRecipe
{
    public string Title { get; set; } = string.Empty;
    public int Portions { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
    public byte[]? ImageData { get; set; }
    public string ImageUri { get; set; } = string.Empty;
    public int PreparationTimeInMinutes { get; set; }
    public int CookingTimeInMinutes { get; set; }
}

public interface IGeminiClient
{
    Task<AIRecipe[]> ExtractRecipeAsync(string ocrText, CancellationToken ct = default);
    Task<AIRecipe[]> ExtractRecipeFromUrlAsync(string responseContent, CancellationToken ct = default);
    bool HasServiceAccount();
    bool HasApiKey();
}