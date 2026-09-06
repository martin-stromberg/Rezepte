namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the import handler.
/// </summary>
/// <param name="Plugin">The plugin parameter.</param>
/// <param name="Handler">The handler parameter.</param>
/// <returns>The result.</returns>
public sealed record PluginImportHandler(ImportPluginDescriptor Plugin, IImportHandler Handler);
