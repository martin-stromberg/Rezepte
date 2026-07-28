using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarVisibilityBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
{
    [SkippableFact]
    public async Task LinkClick_WithDelayedResponse_ActivatesLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();
    }

    [SkippableFact]
    public async Task LinkClick_WithDelayedResponse_MakesLoadingBarVisible()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarVisibleAsync();

        (await pageObject.GetLoadingBarOpacityAsync()).Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task AfterNavigationCompleted_HidesLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 800);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();
        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();

        await pageObject.WaitUntilLoadingBarHiddenAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
