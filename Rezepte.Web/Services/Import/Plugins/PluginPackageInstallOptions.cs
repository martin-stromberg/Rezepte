namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Options for a plugin package installation carried out by <see cref="IPluginPackageInstaller"/>.
/// </summary>
/// <param name="ReleaseTag">The release tag the versioned target directory names are derived from.</param>
/// <param name="SourceKey">The plugin source identifier (<see cref="Rezepte.Web.Entities.PluginSource.Id"/>) the installation belongs to, or <c>null</c> when the installation is not tied to a plugin source.</param>
/// <returns>The result.</returns>
public sealed record PluginPackageInstallOptions(string ReleaseTag, string? SourceKey);
