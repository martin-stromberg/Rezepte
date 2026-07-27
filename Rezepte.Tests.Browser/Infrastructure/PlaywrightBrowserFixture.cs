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

    public bool BrowsersAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public IBrowser? Browser { get; private set; }

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

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
