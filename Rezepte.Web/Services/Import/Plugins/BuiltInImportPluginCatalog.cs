namespace Rezepte.Web.Services.Import.Plugins;

public static class BuiltInImportPluginCatalog
{
    private const int AiPluginPriority = 1000;

    public static IReadOnlyList<ImportPluginDescriptor> GetPlugins() =>
    [
        BuiltIn("ai-foto", "AI-Foto", "Importiert Rezepte aus Fotos per KI.", typeof(AIFotoImportHandler), AiPluginPriority),
        BuiltIn("ai-url", "AI-URL", "Importiert Rezepte aus Webseiten per KI.", typeof(AIUrlImportHandler), AiPluginPriority),
    ];

    private static ImportPluginDescriptor BuiltIn(string id, string displayName, string description, Type handlerType, int defaultPriority)
    {
        return new ImportPluginDescriptor(
            id,
            displayName,
            description,
            typeof(BuiltInImportPluginCatalog).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            handlerType.Assembly.GetName().Name ?? "Rezepte.Web",
            handlerType.FullName ?? handlerType.Name,
            handlerType,
            defaultPriority,
            PluginStatus.Loaded,
            null);
    }
}
