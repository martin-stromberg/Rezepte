namespace Rezepte.Web.Services.Import.Plugins;

public interface IPluginPackageValidator
{
    Task<PluginPackageValidationResult> ValidateAsync(string zipPath, CancellationToken ct = default);
}
