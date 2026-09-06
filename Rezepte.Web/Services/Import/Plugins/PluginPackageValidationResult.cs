namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the package validation result.
/// </summary>
/// <param name="Success">The success parameter.</param>
/// <param name="Error">The error parameter.</param>
/// <param name="ExtractedRoot">The extracted root parameter.</param>
/// <param name="PluginDirectories">The plugin directories parameter.</param>
/// <param name="DiscoveredPlugins">The discovered plugins parameter.</param>
/// <returns>The result.</returns>
public sealed record PluginPackageValidationResult(
    bool Success,
    string? Error,
    string ExtractedRoot,
    IReadOnlyList<string> PluginDirectories,
    IReadOnlyList<ImportPluginDescriptor> DiscoveredPlugins)
{
    /// <summary>
    /// faileds the value.
    /// </summary>
    /// <param name="error">The error parameter.</param>
    /// <param name="extractedRoot">The extracted root parameter.</param>
    /// <returns>The result.</returns>
    public static PluginPackageValidationResult Failed(string error, string extractedRoot)
        => new(false, error, extractedRoot, [], []);
}
