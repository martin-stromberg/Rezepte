using Rezepte.Import.Abstractions;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import;

/// <summary>
/// Represents the base import handler class.
/// </summary>
public class BaseImportHandler : ImportParserBase
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string UserId { protected get; set; } = string.Empty;

    /// <summary>
    /// Parses the ingredient.
    /// </summary>
    /// <param name="line">The line parameter.</param>
    /// <returns>The result.</returns>
    protected RecipeIngredient? ParseIngredient(string line)
    {
        var ingredient = ParseIngredientLine(line);
        if (string.IsNullOrWhiteSpace(ingredient.Name))
            return null;

        return new RecipeIngredient
        {
            Name = ingredient.Name,
            Amount = ingredient.Amount,
            Unit = ingredient.Unit
        };
    }

    /// <summary>
    /// Parses the recipe create ingredient line.
    /// </summary>
    /// <param name="line">The line parameter.</param>
    /// <returns>The result.</returns>
    protected RecipeCreateIngredient ParseRecipeCreateIngredientLine(string line)
    {
        var ingredient = ParseIngredientLine(line);
        return new RecipeCreateIngredient(ingredient.Amount, ingredient.Unit, ingredient.Name);
    }
}
