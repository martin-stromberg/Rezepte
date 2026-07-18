namespace Rezepte.Web.Services.Import.Plugins;

public sealed record PluginPackageValidationResult(
    bool Success,
    string? Error,
    string ExtractedRoot,
    IReadOnlyList<string> PluginDirectories,
    IReadOnlyList<ImportPluginDescriptor> DiscoveredPlugins)
{
    public static PluginPackageValidationResult Failed(string error, string extractedRoot)
        => new(false, error, extractedRoot, [], []);
}
