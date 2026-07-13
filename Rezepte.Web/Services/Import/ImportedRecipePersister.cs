using Rezepte.Import.Abstractions;
using System.Globalization;

namespace Rezepte.Web.Services.Import;

public sealed class ImportedRecipePersister(IRecipeService recipes, ILogger<ImportedRecipePersister> logger) : IImportedRecipePersister
{
    public async Task<ImportResult> PersistAsync(ImportResult result, string targetCookbookId, string userId, CancellationToken ct = default)
    {
        if (!result.Success || result.ImportedRecipes is null || result.ImportedRecipes.Count == 0)
        {
            return result;
        }

        var created = result.CreatedRecipeIds.ToList();
        foreach (var imported in result.ImportedRecipes)
        {
            ct.ThrowIfCancellationRequested();

            var steps = ToSteps(imported);
            var title = string.IsNullOrWhiteSpace(imported.Title) ? "Importiertes Rezept" : imported.Title;
            var existing = string.IsNullOrWhiteSpace(imported.SourceUri)
                ? null
                : await recipes.FindByUri(userId, imported.SourceUri, ct).ConfigureAwait(false);

            string recipeId;
            if (existing is not null)
            {
                var (ok, error) = await recipes.UpdateAsync(
                    userId,
                    existing.Id,
                    title,
                    imported.Description,
                    imported.SourceUri,
                    imported.Portions,
                    steps,
                    ct).ConfigureAwait(false);

                if (!ok)
                {
                    return result with { Success = false, Error = error ?? "Failed to update imported recipe.", CreatedRecipeIds = created };
                }

                recipeId = existing.Id;
            }
            else
            {
                var (ok, error, recipe) = await recipes.CreateAsync(
                    userId,
                    targetCookbookId,
                    title,
                    imported.Description,
                    imported.SourceUri,
                    imported.Portions,
                    steps,
                    ct).ConfigureAwait(false);

                if (!ok || recipe is null)
                {
                    return result with { Success = false, Error = error ?? "Failed to create imported recipe.", CreatedRecipeIds = created };
                }

                recipeId = recipe.Id;
                created.Add(recipe.Id);
            }

            await AddImagesAsync(userId, recipeId, title, imported.Images, ct).ConfigureAwait(false);
        }

        return result with { CreatedRecipeIds = created, ImportedRecipes = [] };
    }

    private static List<RecipeCreateStep> ToSteps(ImportedRecipe imported)
    {
        var ingredients = imported.Ingredients.Select(ToIngredient).ToList();
        var steps = imported.Steps.Count == 0
            ? [new ImportedRecipeStep { Text = imported.Description ?? string.Empty }]
            : imported.Steps;

        return steps.Select((step, index) => new RecipeCreateStep(
            Title: null,
            Description: step.Text ?? string.Empty,
            DurationMinutes: index == 0 ? imported.WorkTimeMinutes : 0,
            RequiresOvernightRest: false,
            Ingredients: index == 0 ? ingredients : [])).ToList();
    }

    private static RecipeCreateIngredient ToIngredient(ImportedIngredient ingredient)
    {
        var quantity = ingredient.Quantity?.Trim() ?? string.Empty;
        var name = ingredient.Name?.Trim() ?? string.Empty;
        var amount = 0m;
        string? unit = null;

        var number = new string(quantity.TakeWhile(c => char.IsDigit(c) || c is '.' or ',').ToArray());
        if (!string.IsNullOrWhiteSpace(number)
            && decimal.TryParse(number.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            amount = parsed;
            var rest = quantity[number.Length..].Trim();
            unit = string.IsNullOrWhiteSpace(rest) ? null : rest;
        }
        else if (!string.IsNullOrWhiteSpace(quantity))
        {
            name = $"{quantity} {name}".Trim();
        }

        return new RecipeCreateIngredient(amount, unit, name);
    }

    private async Task AddImagesAsync(string userId, string recipeId, string title, IReadOnlyList<ImportedImage> images, CancellationToken ct)
    {
        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            if (image.Data.Length == 0)
            {
                continue;
            }

            var fileName = string.IsNullOrWhiteSpace(image.FileName) ? $"{SanitizeFileName(title)}-{index + 1}.bin" : image.FileName;
            var contentType = string.IsNullOrWhiteSpace(image.ContentType) ? "application/octet-stream" : image.ContentType;
            var (ok, error, _) = await recipes.AddImageAsync(userId, recipeId, new MemoryStream(image.Data), fileName, contentType, ct).ConfigureAwait(false);
            if (!ok)
            {
                logger.LogWarning("Failed to attach imported image {FileName} to recipe {RecipeId}: {Error}", fileName, recipeId, error);
            }
        }
    }

    private static string SanitizeFileName(string input)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(input) ? "image" : input;
    }
}
