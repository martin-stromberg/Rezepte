using FluentAssertions;
using Microsoft.Playwright;
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

        await pageObject.Page.DelayNavigationAsync("**/cookbooks", 1500);
        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();
    }

    [SkippableFact]
    public async Task LinkClick_WithDelayedResponse_MakesLoadingBarVisible()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.Page.DelayNavigationAsync("**/cookbooks", 1500);
        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.GetLoadingBarOpacityAsync()).Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task AfterNavigationCompleted_HidesLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.Page.DelayNavigationAsync("**/cookbooks", 800);
        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });

        await pageObject.WaitUntilLoadingBarActiveAsync();
        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();

        await pageObject.WaitUntilLoadingBarHiddenAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
