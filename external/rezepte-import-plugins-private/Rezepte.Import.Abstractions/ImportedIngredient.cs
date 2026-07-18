namespace Rezepte.Import.Abstractions;

public sealed record ImportedIngredient
{
    public string? Quantity { get; init; }
    public string? Name { get; init; }
}
