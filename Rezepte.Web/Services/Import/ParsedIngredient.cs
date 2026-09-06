namespace Rezepte.Web.Services.Import;

/// <summary>
/// parseds the ingredient.
/// </summary>
/// <param name="Amount">The amount parameter.</param>
/// <param name="Unit">The unit parameter.</param>
/// <param name="Name">The name parameter.</param>
/// <returns>The result.</returns>
public sealed record ParsedIngredient(decimal Amount, string? Unit, string Name);
