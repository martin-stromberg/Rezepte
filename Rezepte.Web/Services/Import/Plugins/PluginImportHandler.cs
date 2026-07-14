namespace Rezepte.Web.Services.Import.Plugins;

public sealed record PluginImportHandler(ImportPluginDescriptor Plugin, IImportHandler Handler);
