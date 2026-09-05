namespace Rezepte.Import.Abstractions;

/// <summary>
/// Describes whether an import plugin can be used and which issues prevent usage.
/// </summary>
/// <param name="IsUsable"><c>true</c> if the plugin can be used; otherwise <c>false</c>.</param>
/// <param name="Issues">Collection of issues that prevent usage.</param>
/// <returns>A new instance of the <see cref="PluginUsabilityResult"/> record.</returns>
public sealed record PluginUsabilityResult(bool IsUsable, IReadOnlyList<PluginUsabilityIssue> Issues)
{
    private static readonly PluginUsabilityResult _usable = new(true, []);

    /// <summary>
    /// Singleton value representing a usable plugin without issues.
    /// </summary>
    public static PluginUsabilityResult Usable { get; } = _usable;

    /// <summary>
    /// Creates a usability result from the provided issues.
    /// </summary>
    /// <param name="issues">Issues to inspect.</param>
    /// <returns>A usable result if <paramref name="issues"/> is empty; otherwise a non-usable result.</returns>
    public static PluginUsabilityResult FromIssues(IReadOnlyList<PluginUsabilityIssue> issues) =>
        issues.Count == 0 ? Usable : new PluginUsabilityResult(false, issues);
}
