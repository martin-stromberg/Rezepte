namespace Rezepte.Web.Services.Import;

public sealed record ParsedIngredient(decimal Amount, string? Unit, string Name);
