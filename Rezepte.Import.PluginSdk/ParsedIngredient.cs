namespace Rezepte.Import.PluginSdk;

/// <summary>
/// Ingredient quantity parsed from a free-text ingredient line.
/// </summary>
/// <param name="Amount">Numeric amount extracted from the line.</param>
/// <param name="Unit">Optional unit, for example "g" or "ml".</param>
/// <param name="Name">Name of the ingredient.</param>
/// <returns>A new instance of the <see cref="ParsedIngredient"/> record.</returns>
public sealed record ParsedIngredient(decimal Amount, string? Unit, string Name);
