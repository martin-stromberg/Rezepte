using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the loading bar becomes active, visible, and hidden at the expected times.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class LoadingBarVisibilityBrowserTests
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingBarVisibilityBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public LoadingBarVisibilityBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that the loading bar becomes active after a delayed navigation link click.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task LinkClick_WithDelayedResponse_ActivatesLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the loading bar becomes visible after a delayed navigation link click.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task LinkClick_WithDelayedResponse_MakesLoadingBarVisible()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarVisibleAsync();

        (await pageObject.GetLoadingBarOpacityAsync()).Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Verifies that the loading bar hides again after a delayed navigation completes.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task AfterNavigationCompleted_HidesLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 800);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();
        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();

        await pageObject.WaitUntilLoadingBarHiddenAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
