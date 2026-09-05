namespace Rezepte.Web.Entities;

/// <summary>
/// Represents the app setting class.
/// </summary>
public class AppSetting
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Key { get; set; } = string.Empty; // e.g. "AiEnabled"
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
