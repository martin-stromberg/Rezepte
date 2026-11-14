namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Manages versioned plugin installation directories below <c>plugins/</c> and the persistent
/// state file <c>.install-state.json</c> (effective installs and directories marked for deletion).
/// </summary>
public interface IPluginDirectoryRegistry
{
    /// <summary>
    /// Determines whether the given plugin root is the managed install root (<c>{ContentRootPath}/plugins</c>).
    /// </summary>
    /// <param name="pluginRoot">The plugin root parameter.</param>
    /// <returns><c>true</c> when the root is the managed install root; otherwise <c>false</c>.</returns>
    bool IsManagedRoot(string pluginRoot);

    /// <summary>
    /// Loads the persistent install state. A missing or corrupt state file yields an empty state (fail-open).
    /// </summary>
    /// <returns>The loaded install state.</returns>
    PluginInstallState LoadState();

    /// <summary>
    /// Saves the install state atomically (temporary file followed by a move).
    /// </summary>
    /// <param name="state">The state parameter.</param>
    void SaveState(PluginInstallState state);

    /// <summary>
    /// Creates the versioned directory name <c>&lt;baseName&gt;@&lt;releaseTag&gt;</c>; invalid file name
    /// characters and <c>@</c> inside the release tag are replaced by <c>-</c>.
    /// </summary>
    /// <param name="baseName">The base name parameter.</param>
    /// <param name="releaseTag">The release tag parameter.</param>
    /// <returns>The versioned directory name.</returns>
    string CreateVersionedDirectoryName(string baseName, string releaseTag);

    /// <summary>
    /// Splits a directory name at the first <c>@</c> into base name and release tag.
    /// </summary>
    /// <param name="directoryName">The directory name parameter.</param>
    /// <param name="baseName">The base name result.</param>
    /// <param name="releaseTag">The release tag result.</param>
    /// <returns><c>true</c> when the name follows the <c>&lt;base&gt;@&lt;tag&gt;</c> pattern; otherwise <c>false</c>.</returns>
    bool TryParseVersionedDirectoryName(string directoryName, out string baseName, out string releaseTag);

    /// <summary>
    /// Computes the directory names below a managed root that must be excluded from plugin discovery:
    /// directories marked for deletion, non-effective <c>name@tag</c> directories and flat directories
    /// shadowed by a same-named versioned directory.
    /// </summary>
    /// <param name="pluginRoot">The plugin root parameter.</param>
    /// <param name="state">The state parameter.</param>
    /// <returns>The set of excluded directory names (comparison is ordinal-ignore-case).</returns>
    IReadOnlySet<string> GetExcludedDirectoryNames(string pluginRoot, PluginInstallState state);

    /// <summary>
    /// Selects the effective directory among the given <c>name@tag</c> candidates of one base name:
    /// the registered install wins when it exists on disk, otherwise the highest version is used.
    /// </summary>
    /// <param name="baseName">The base name parameter.</param>
    /// <param name="directoryNames">The versioned directory names present on disk.</param>
    /// <param name="state">The state parameter.</param>
    /// <returns>The effective directory name, or <c>null</c> when no candidates exist.</returns>
    string? SelectEffectiveDirectory(string baseName, IReadOnlyList<string> directoryNames, PluginInstallState state);

    /// <summary>
    /// Marks a directory name as pending deletion in the install state.
    /// </summary>
    /// <param name="state">The state parameter.</param>
    /// <param name="directoryName">The directory name parameter.</param>
    void MarkPendingDeletion(PluginInstallState state, string directoryName);

    /// <summary>
    /// Deletes all directories marked for deletion on a best-effort basis, removes stale entries
    /// and persists the resulting state.
    /// </summary>
    /// <param name="state">The state parameter.</param>
    void CleanupMarkedDirectories(PluginInstallState state);
}
