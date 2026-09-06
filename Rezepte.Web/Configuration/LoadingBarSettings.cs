namespace Rezepte.Web.Configuration;

/// <summary>
/// Normalized, render-ready settings for the loading bar, assembled from <see cref="LoadingBarOptions"/>.
/// </summary>
/// <returns>The result.</returns>
/// <param name="Enabled">Whether the loading bar feature is enabled.</param>
/// <param name="Height">Height of the loading bar as a CSS length.</param>
/// <param name="AnimationDuration">Duration of one full right-to-left sweep as a CSS time.</param>
/// <param name="Colors">Colors from which one is chosen at random for each navigation interaction.</param>
/// <param name="HideDelayMilliseconds">Delay in milliseconds after navigation completes before the loading bar is hidden.</param>
/// <param name="MaxVisibleDurationMilliseconds">Safety limit in milliseconds after which the loading bar is hidden even without a completion signal.</param>
public sealed record LoadingBarSettings(
    bool Enabled,
    string Height,
    string AnimationDuration,
    IReadOnlyList<string> Colors,
    int HideDelayMilliseconds,
    int MaxVisibleDurationMilliseconds);
