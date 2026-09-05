namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// imports the plugin descriptor.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="DisplayName">The display name parameter.</param>
/// <param name="Description">The description parameter.</param>
/// <param name="Version">The version parameter.</param>
/// <param name="AssemblyName">The assembly name parameter.</param>
/// <param name="TypeName">The type name parameter.</param>
/// <param name="HandlerType">The handler type parameter.</param>
/// <param name="DefaultPriority">The default priority parameter.</param>
/// <param name="Status">The status parameter.</param>
/// <param name="Error">The error parameter.</param>
/// <param name="PluginType">The plugin type parameter.</param>
/// <returns>The result.</returns>
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
