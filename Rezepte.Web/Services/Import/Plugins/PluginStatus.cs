namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Represents the plugin status class.
/// </summary>
public static class PluginStatus
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Loaded = "Loaded";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Missing = "Missing";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Incompatible = "Incompatible";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string LoadFailed = "LoadFailed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string RuntimeFailed = "RuntimeFailed";
}
