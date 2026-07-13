using Rezepte.Web.Services.Import.Url;

namespace Rezepte.Web.Services.Import.Plugins;

public static class BuiltInImportPluginCatalog
{
    public static IReadOnlyList<ImportPluginDescriptor> GetPlugins() =>
    [
        BuiltIn("backup", "Backup", "Importiert Backup-ZIP-Dateien.", typeof(BackupImportHandler)),
        BuiltIn("chefkoch", "Chefkoch", "Importiert Rezepte von Chefkoch.", typeof(ChefkochReceiptImportHandler)),
        BuiltIn("second-source", "SecondSource", "Importiert Rezepte der zweiten URL-Quelle.", typeof(SecondSourceUrlReceiptImportHandler)),
        BuiltIn("third-source", "ThirdSource", "Importiert Rezepte der dritten URL-Quelle.", typeof(ThirdSourceUrlReceiptImportHandler)),
        BuiltIn("fourth-source", "FourthSource", "Importiert Rezepte der vierten URL-Quelle.", typeof(FourthSourceUrlReceiptImportHandler)),
        BuiltIn("fifth-source", "FifthSource", "Importiert Rezepte der fünften URL-Quelle.", typeof(FifthSourceUrlRecipeImportHandler)),
        BuiltIn("sixth-source", "SixthSource", "Importiert Rezepte der sechsten URL-Quelle.", typeof(SixthSourceUrlRecipeImportHandler)),
        BuiltIn("ai-foto", "AI-Foto", "Importiert Rezepte aus Fotos per KI.", typeof(AIFotoImportHandler)),
        BuiltIn("ai-url", "AI-URL", "Importiert Rezepte aus Webseiten per KI.", typeof(AIUrlImportHandler)),
    ];

    private static ImportPluginDescriptor BuiltIn(string id, string displayName, string description, Type handlerType)
    {
        return new ImportPluginDescriptor(
            id,
            displayName,
            description,
            typeof(BuiltInImportPluginCatalog).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            handlerType.Assembly.GetName().Name ?? "Rezepte.Web",
            handlerType.FullName ?? handlerType.Name,
            handlerType,
            PluginStatus.Loaded,
            null);
    }
}
