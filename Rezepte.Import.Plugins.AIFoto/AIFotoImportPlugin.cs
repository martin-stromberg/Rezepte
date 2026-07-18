using Rezepte.Import.Abstractions;
using Rezepte.Web.Services.Import;

namespace Rezepte.Import.Plugins.AIFoto;

public sealed class AIFotoImportPlugin : IImportPlugin
{
    public string Id => "ai-foto";
    public string DisplayName => "AI-Foto";
    public string? Description => "Importiert Rezepte aus Fotos per KI.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(AIFotoImportHandler);
    public int DefaultPriority => 1000;
}
