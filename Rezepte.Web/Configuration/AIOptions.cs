namespace Rezepte.Web.Configuration;

/// <summary>
/// Represents the aioptions class.
/// </summary>
public sealed class AIOptions
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool Simulate { get; set; } = false;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public bool EnableCache { get; set; } = false;
    /// <summary>
    /// Represents the public class.
    /// </summary>
    public int CacheDurationHours { get; set; } = 24;
}
