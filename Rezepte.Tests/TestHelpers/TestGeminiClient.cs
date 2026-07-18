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

    public bool HasApiKey()
    {
        return true;
    }

    public bool HasServiceAccount()
    {
        return true;
    }
}