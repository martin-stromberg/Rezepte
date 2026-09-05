namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Defines the iplugin package validator interface.
/// </summary>
public interface IPluginPackageValidator
{
    /// <summary>
    /// Validates the async.
    /// </summary>
    /// <param name="zipPath">The zip path parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    /// <returns>The result.</returns>
    Task<PluginPackageValidationResult> ValidateAsync(string zipPath, CancellationToken ct = default);
}
