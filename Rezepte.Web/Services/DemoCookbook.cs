namespace Rezepte.Web.Services;

/// <summary>
/// Groups demo recipes under a demo cookbook name.
/// </summary>
/// <param name="Name">The display name of the demo cookbook.</param>
/// <param name="Recipes">The recipes that belong to the demo cookbook.</param>
/// <returns>The demo cookbook definition.</returns>
public sealed record DemoCookbook(string Name, IReadOnlyList<DemoRecipe> Recipes);
