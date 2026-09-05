using System.Collections.ObjectModel;

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
    /// <returns>The result.</returns>
    /// <remarks>
    /// Defaults to an empty array rather than <see cref="DefaultColors"/> because the .NET configuration
    /// binder appends configured entries to a pre-populated array instead of replacing them. Starting
    /// from an empty array is what allows <c>LoadingBar:Colors</c> in appsettings.json to fully replace
    /// the documented default palette. Use <see cref="DefaultColors"/> to obtain the documented defaults.
    /// </remarks>
    public string[] Colors { get; set; } = Array.Empty<string>();

    private static readonly string[] DefaultColorsArray = { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD" };

    /// <summary>
    /// The documented default color palette, used when <see cref="Colors"/> is empty or invalid.
    /// </summary>
    /// <param name="DefaultColorsArray">The default colors array parameter.</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result.</returns>
    public static readonly IReadOnlyList<string> DefaultColors = new ReadOnlyCollection<string>(DefaultColorsArray);
}
