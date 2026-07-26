namespace Rezepte.Import.Abstractions;

public sealed record PluginUsabilityResult(bool IsUsable, IReadOnlyList<PluginUsabilityIssue> Issues)
{
    public static readonly PluginUsabilityResult Usable = new(true, []);

    public static PluginUsabilityResult FromIssues(IReadOnlyList<PluginUsabilityIssue> issues) =>
        issues.Count == 0 ? Usable : new PluginUsabilityResult(false, issues);
}
