namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Represents the plugin update check outcome class.
/// </summary>
public static class PluginUpdateCheckOutcome
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string NoUpdate = "NoUpdate";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Installed = "Installed";
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public const string Failed = "Failed";
}
