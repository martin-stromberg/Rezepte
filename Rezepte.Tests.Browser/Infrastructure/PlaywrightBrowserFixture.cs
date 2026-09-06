using Microsoft.Playwright;
using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Initializes Playwright and launches headless Chromium. Detects missing browser binaries so
/// dependent tests can be skipped instead of failing on a machine without <c>playwright install</c>.
/// </summary>
public sealed class PlaywrightBrowserFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;

    /// <summary>
    /// Gets a value indicating whether a Chromium browser could be launched.
    /// </summary>
    public bool BrowsersAvailable { get; private set; }

    /// <summary>
    /// Gets the error message when <see cref="BrowsersAvailable"/> is <c>false</c>.
    /// </summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>
    /// Gets the launched Chromium browser instance, or <c>null</c> if not available.
    /// </summary>
    public IBrowser? Browser { get; private set; }

    /// <summary>
    /// Creates the Playwright instance and launches the browser.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            BrowsersAvailable = true;
        }
        catch (PlaywrightException ex)
        {
            BrowsersAvailable = false;
            UnavailableReason = ex.Message;
        }
    }

    /// <summary>
    /// Closes the browser and disposes the Playwright instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
