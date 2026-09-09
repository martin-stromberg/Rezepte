namespace Rezepte.Web.Services;

/// <summary>
/// Describes a single demo recipe used during seeding.
/// </summary>
/// <param name="Title">The title of the demo recipe.</param>
/// <param name="Description">The optional description of the demo recipe.</param>
/// <param name="Portions">The number of portions the recipe yields.</param>
/// <param name="Steps">The preparation steps and their ingredients.</param>
/// <returns>The demo recipe definition.</returns>
public sealed record DemoRecipe(string Title, string? Description, int Portions, IReadOnlyList<RecipeCreateStep> Steps);
