namespace Rezepte.Import.Abstractions;

public interface IImportPlugin
{
    string Id { get; }
    string DisplayName { get; }
    string? Description { get; }
    string Version { get; }
    Type HandlerType { get; }
    int DefaultPriority => 0;
}
