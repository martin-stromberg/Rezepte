using Rezepte.Import.Abstractions;
using Rezepte.Web.Entities;

namespace Rezepte.Web.Services.Import;

public class BaseImportHandler : ImportParserBase
{
    public string UserId { protected get; set; } = string.Empty;

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

    protected RecipeCreateIngredient ParseRecipeCreateIngredientLine(string line)
    {
        var ingredient = ParseIngredientLine(line);
        return new RecipeCreateIngredient(ingredient.Amount, ingredient.Unit, ingredient.Name);
    }
}
