namespace Rezepte.Import.Abstractions;

public sealed record ParsedIngredient(decimal Amount, string? Unit, string Name);
