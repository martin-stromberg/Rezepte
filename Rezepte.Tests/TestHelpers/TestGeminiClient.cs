using System.Threading;
using System.Threading.Tasks;
using Rezepte.Web.Services.Import;

namespace Rezepte.Tests.TestHelpers;

/// <summary>
/// Simple test/dummy implementation of <see cref="IGeminiClient"/> that simulates responses synchronously.
/// Use in unit tests to avoid real network calls.
/// </summary>
public class TestGeminiClient : IGeminiClient
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="ocrText">The ocr text parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<AIRecipe[]> ExtractRecipeAsync(string ocrText, CancellationToken ct = default)
    {
        var r = new AIRecipe
        {
            Title = "Simuliertes Rezept",
            Instructions = $"Simulierte Antwort für OCR: {(ocrText?.Substring(0, Math.Min(20, ocrText?.Length ?? 0)) ?? string.Empty)}",
            Ingredients = new System.Collections.Generic.List<string> { "1 Zutat" },
            Portions = 1,
            PreparationTimeInMinutes = 10
        };
        return Task.FromResult(new[] { r });
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="responseContent">The response content parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    public Task<AIRecipe[]> ExtractRecipeFromUrlAsync(string responseContent, CancellationToken ct = default)
    {
        var r = new AIRecipe
        {
            Title = "Simuliertes URL-Rezept",
            Instructions = $"Simulierte Antwort für URL-Inhalt (len={responseContent?.Length ?? 0})",
            Ingredients = new System.Collections.Generic.List<string> { "1 Zutat" },
            Portions = 2,
            PreparationTimeInMinutes = 5
        };
        return Task.FromResult(new[] { r });
    }

    /// <summary>
    /// Has api key.
    /// </summary>
    /// <returns>The result.</returns>
    public bool HasApiKey()
    {
        return true;
    }

    /// <summary>
    /// Has service account.
    /// </summary>
    /// <returns>The result.</returns>
    public bool HasServiceAccount()
    {
        return true;
    }
}
