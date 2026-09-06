using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the loading bar safety timeout hides the bar for hanging navigations.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class LoadingBarSafetyTimeoutBrowserTests : IClassFixture<LoadingBarSafetyTimeoutBrowserTests.ShortMaxVisibleDurationFixture>
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly ShortMaxVisibleDurationFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingBarSafetyTimeoutBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The fixture that uses a short maximum visible duration.</param>
    public LoadingBarSafetyTimeoutBrowserTests(PlaywrightBrowserFixture browserFixture, ShortMaxVisibleDurationFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that the loading bar hides after the configured maximum visible duration
    /// when the target navigation never completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        // Route the target navigation but never fulfill, continue, or abort it, so the request hangs forever.
        await pageObject.BlockRouteAsync(LoadingBarPageObject.ShoppingListRouteGlob);

        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.ShoppingListHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();
        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();

        await pageObject.WaitUntilLoadingBarHiddenAsync(timeoutMilliseconds: 2000);

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }

    /// <summary>
    /// Fixture that starts the application with a short loading bar maximum visible duration.
    /// </summary>
    public sealed class ShortMaxVisibleDurationFixture : ConfiguredRezepteAppFixture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShortMaxVisibleDurationFixture"/> class.
        /// </summary>
        public ShortMaxVisibleDurationFixture() : base(new Dictionary<string, string?>
        {
            ["LoadingBar__MaxVisibleDuration"] = "800ms"
        })
        {
        }
    }
}
