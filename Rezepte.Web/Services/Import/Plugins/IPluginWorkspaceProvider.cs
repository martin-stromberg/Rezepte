namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Provides temporary work directories for the plugin update path (download, package
/// extraction, install backup).
/// </summary>
public interface IPluginWorkspaceProvider
{
    /// <summary>
    /// Creates a fresh work directory for the given purpose below a writable base directory.
    /// </summary>
    /// <param name="purpose">The purpose parameter.</param>
    /// <returns>The full path of the created directory.</returns>
    string CreateWorkDirectory(string purpose);
}
