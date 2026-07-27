using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Artificially delays every request matching the given pattern so browser tests can deterministically
/// observe behavior while a navigation is still in flight.
/// </summary>
public static class NetworkDelayHelper
{
    public static async Task DelayNavigationAsync(this IPage page, string urlGlobPattern, int delayMilliseconds)
    {
        await page.RouteAsync(urlGlobPattern, async route =>
        {
            await Task.Delay(delayMilliseconds);
            await route.ContinueAsync();
        });
    }
}
