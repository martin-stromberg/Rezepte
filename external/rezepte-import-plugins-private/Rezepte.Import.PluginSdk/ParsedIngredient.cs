namespace Rezepte.Import.PluginSdk;

public sealed record ParsedIngredient(decimal Amount, string? Unit, string Name);
