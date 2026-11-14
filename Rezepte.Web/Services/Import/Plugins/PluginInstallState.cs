namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Persistent state of managed plugin installations stored in <c>plugins/.install-state.json</c>.
/// </summary>
public sealed class PluginInstallState
{
    /// <summary>
    /// Gets or sets the schema version of the state file.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the managed plugin installations, one entry per plugin base name.
    /// </summary>
    public List<PluginInstallEntry> Installs { get; set; } = [];

    /// <summary>
    /// Gets or sets the directory names marked for deletion that could not be removed yet.
    /// </summary>
    public List<string> PendingDeletes { get; set; } = [];
}

/// <summary>
/// Represents a single managed plugin installation tracked by the plugin directory registry.
/// </summary>
public sealed class PluginInstallEntry
{
    /// <summary>
    /// Gets or sets the plugin base name (the directory name without the version suffix).
    /// </summary>
    public string BaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the effective versioned directory name (<c>&lt;BaseName&gt;@&lt;ReleaseTag&gt;</c>).
    /// </summary>
    public string DirectoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release tag the installed version was derived from.
    /// </summary>
    public string ReleaseTag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plugin source identifier the installation belongs to, or <c>null</c>.
    /// </summary>
    public string? SourceKey { get; set; }
}
