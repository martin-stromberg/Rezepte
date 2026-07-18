using Rezepte.Import.Abstractions;

namespace Rezepte.Import.Plugins.AIUrl;

public sealed class AIUrlImportPlugin : IImportPlugin
{
    public string Id => "ai-url";
    public string DisplayName => "AI-URL";
    public string? Description => "Importiert Rezepte aus Webseiten per KI.";
    public string Version => "1.0.0";
    public Type HandlerType => typeof(AIUrlImportHandler);
    public int DefaultPriority => 1000;
}
