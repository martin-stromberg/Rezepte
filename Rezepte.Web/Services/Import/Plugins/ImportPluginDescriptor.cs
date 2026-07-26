namespace Rezepte.Web.Services.Import.Plugins;

public sealed record ImportPluginDescriptor(
    string Id,
    string DisplayName,
    string? Description,
    string Version,
    string AssemblyName,
    string TypeName,
    Type? HandlerType,
    int DefaultPriority,
    string Status,
    string? Error,
    Type? PluginType);
