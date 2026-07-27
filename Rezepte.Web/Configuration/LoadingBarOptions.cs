namespace Rezepte.Web.Configuration;

/// <summary>
/// Configuration for the loading bar shown below the navigation bar during navigation.
/// </summary>
public sealed class LoadingBarOptions
{
    /// <summary>
    /// Enables or disables the loading bar feature globally.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Height of the loading bar as a CSS length (e.g. "3px", "0.25rem").
    /// </summary>
    public string Height { get; set; } = "3px";

    /// <summary>
    /// Duration of one full right-to-left sweep as a CSS time (e.g. "2s", "500ms").
    /// </summary>
    public string AnimationDuration { get; set; } = "2s";

    /// <summary>
    /// Delay after navigation completes before the loading bar is hidden, as a CSS time.
    /// </summary>
    public string HideDelay { get; set; } = "300ms";

    /// <summary>
    /// Safety limit after which the loading bar is hidden even without a completion signal, as a CSS time.
    /// </summary>
    public string MaxVisibleDuration { get; set; } = "15s";

    /// <summary>
    /// Colors from which one is chosen at random for each navigation interaction.
    /// </summary>
    public string[] Colors { get; set; } = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD" };
}
