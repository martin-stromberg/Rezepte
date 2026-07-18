namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginPackageInstaller
{
    Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default);
}
