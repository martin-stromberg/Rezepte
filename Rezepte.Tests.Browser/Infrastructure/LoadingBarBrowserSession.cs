using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Skips the calling test when the browser or the application under test is unavailable, then creates a
/// logged-in <see cref="LoadingBarPageObject"/> for the standard loading bar browser test classes.
/// </summary>
internal static class LoadingBarBrowserSession
{
    public static async Task<LoadingBarPageObject> StartLoggedInSessionAsync(
        PlaywrightBrowserFixture browserFixture,
        RezepteAppFixture appFixture)
    {
        Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(appFixture.ApplicationAvailable, RezepteAppFixture.ApplicationUnavailableSkipReason);

        var pageObject = await LoadingBarPageObject.CreateAsync(browserFixture.Browser!, appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        return pageObject;
    }
}
