namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Represents the built in import plugin catalog class.
/// </summary>
public static class BuiltInImportPluginCatalog
{
    /// <summary>
    /// Gets the plugins.
    /// </summary>
    /// <returns>The result.</returns>
    public static IReadOnlyList<ImportPluginDescriptor> GetPlugins() => [];
}
