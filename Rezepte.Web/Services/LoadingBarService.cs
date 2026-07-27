using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services;

/// <summary>
/// Validates and normalizes <see cref="LoadingBarOptions"/> into a cached <see cref="LoadingBarSettings"/>.
/// Invalid values are never rejected with an exception; they are replaced with documented defaults and logged as warnings.
/// </summary>
public sealed class LoadingBarService : ILoadingBarService
{
    private static readonly LoadingBarOptions Defaults = new();
    private static readonly IReadOnlyList<string> DefaultColors = new ReadOnlyCollection<string>(new LoadingBarOptions().Colors);
    private static readonly Regex CssLengthPattern = new(@"^\d+(?:\.\d+)?(?:px|rem|em)$", RegexOptions.Compiled);
    private static readonly Regex CssTimePattern = new(@"^(\d+(?:\.\d+)?)(ms|s)$", RegexOptions.Compiled);
    private static readonly Regex HexColorPattern = new(@"^#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    private readonly ILogger<LoadingBarService> _logger;
    private readonly Lazy<LoadingBarSettings> _settings;

    public LoadingBarService(IOptions<LoadingBarOptions> options, ILogger<LoadingBarService> logger)
    {
        _logger = logger;
        _settings = new Lazy<LoadingBarSettings>(() => BuildSettings(options.Value));
    }

    /// <returns>The normalized loading bar settings.</returns>
    public LoadingBarSettings GetSettings() => _settings.Value;

    private LoadingBarSettings BuildSettings(LoadingBarOptions options)
    {
        var height = ValidateAgainstPattern(options.Height, CssLengthPattern, Defaults.Height, nameof(LoadingBarOptions.Height));
        var animationDuration = ValidateAgainstPattern(options.AnimationDuration, CssTimePattern, Defaults.AnimationDuration, nameof(LoadingBarOptions.AnimationDuration));
        var hideDelayMilliseconds = ValidateCssTimeAsMilliseconds(options.HideDelay, ToDefaultMilliseconds(Defaults.HideDelay), nameof(LoadingBarOptions.HideDelay));
        var maxVisibleDurationMilliseconds = ValidateCssTimeAsMilliseconds(options.MaxVisibleDuration, ToDefaultMilliseconds(Defaults.MaxVisibleDuration), nameof(LoadingBarOptions.MaxVisibleDuration));

        if (maxVisibleDurationMilliseconds <= hideDelayMilliseconds)
        {
            _logger.LogWarning(
                "LoadingBar:{Field} ({ValueMilliseconds}ms) must be greater than LoadingBar:HideDelay ({HideDelayMilliseconds}ms). Falling back to default '{Default}'.",
                nameof(LoadingBarOptions.MaxVisibleDuration),
                maxVisibleDurationMilliseconds,
                hideDelayMilliseconds,
                Defaults.MaxVisibleDuration);
            maxVisibleDurationMilliseconds = ToDefaultMilliseconds(Defaults.MaxVisibleDuration);
        }

        var colors = ValidateColors(options.Colors);

        return new LoadingBarSettings(
            options.Enabled,
            height,
            animationDuration,
            colors,
            hideDelayMilliseconds,
            maxVisibleDurationMilliseconds);
    }

    private string ValidateAgainstPattern(string? value, Regex pattern, string fallback, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value) && pattern.IsMatch(value))
        {
            return value;
        }

        _logger.LogWarning("Invalid LoadingBar:{Field} value '{Value}'. Falling back to default '{Default}'.", fieldName, value, fallback);
        return fallback;
    }

    private int ValidateCssTimeAsMilliseconds(string? value, int fallbackMilliseconds, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value) && TryToMilliseconds(value, out var milliseconds))
        {
            return milliseconds;
        }

        _logger.LogWarning("Invalid LoadingBar:{Field} value '{Value}'. Falling back to default '{DefaultMilliseconds}ms'.", fieldName, value, fallbackMilliseconds);
        return fallbackMilliseconds;
    }

    private IReadOnlyList<string> ValidateColors(string[]? colors)
    {
        var validColors = new List<string>();
        foreach (var color in colors ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(color) && HexColorPattern.IsMatch(color))
            {
                validColors.Add(color);
            }
            else
            {
                _logger.LogWarning("Invalid LoadingBar:Colors entry '{Value}'. Removing it from the color list.", color);
            }
        }

        if (validColors.Count == 0)
        {
            _logger.LogWarning("LoadingBar:Colors contains no valid entries after filtering. Falling back to the default color list.");
            return DefaultColors;
        }

        return new ReadOnlyCollection<string>(validColors);
    }

    private static bool TryToMilliseconds(string cssTime, out int milliseconds)
    {
        var match = CssTimePattern.Match(cssTime);
        if (!match.Success)
        {
            milliseconds = 0;
            return false;
        }

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value;
        milliseconds = unit == "ms" ? (int)value : (int)(value * 1000);
        return true;
    }

    private static int ToDefaultMilliseconds(string cssTime)
    {
        if (!TryToMilliseconds(cssTime, out var milliseconds))
        {
            throw new ArgumentException($"Default CSS time '{cssTime}' is not a valid CSS time.", nameof(cssTime));
        }

        return milliseconds;
    }
}
