using FluentAssertions;
using Microsoft.Playwright;
using Rezepte.Tests.Browser.Infrastructure;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarColorBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
{
    private static readonly IReadOnlyList<string> ConfiguredPalette = new LoadingBarOptions().Colors;

    [SkippableFact]
    public async Task LinkClick_UsesColorFromConfiguredPalette()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.Page.DelayNavigationAsync("**/cookbooks", 1500);
        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });
        await pageObject.WaitUntilLoadingBarActiveAsync();

        var color = await pageObject.GetLoadingBarColorAsync();

        color.Should().NotBeNullOrEmpty();
        ConfiguredPalette.Should().Contain(color);
    }

    [SkippableFact]
    public async Task SecondClickDuringRunningAnimation_ChangesColor()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.Page.DelayNavigationAsync("**/cookbooks", 3000);

        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });
        await pageObject.WaitUntilLoadingBarActiveAsync();
        var firstColor = await pageObject.GetLoadingBarColorAsync();

        await pageObject.Page.ClickAsync("a[href='/cookbooks']", new PageClickOptions { NoWaitAfter = true });
        await pageObject.WaitUntilLoadingBarActiveAsync();
        var secondColor = await pageObject.GetLoadingBarColorAsync();

        secondColor.Should().NotBe(firstColor);
    }
}
