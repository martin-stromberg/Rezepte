using Rezepte.Web.Configuration;

namespace Rezepte.Web.Services;

/// <summary>
/// Assembles the normalized settings for the loading bar from <c>LoadingBarOptions</c>.
/// </summary>
public interface ILoadingBarService
{
    /// <summary>
    /// Returns the normalized, cached loading bar settings.
    /// </summary>
    /// <returns>The normalized loading bar settings.</returns>
    LoadingBarSettings GetSettings();
}
