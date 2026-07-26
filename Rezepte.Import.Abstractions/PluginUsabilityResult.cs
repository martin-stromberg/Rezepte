namespace Rezepte.Import.Abstractions;

public sealed record PluginUsabilityResult(bool IsUsable, IReadOnlyList<PluginUsabilityIssue> Issues)
{
    public static readonly PluginUsabilityResult Usable = new(true, []);
}
