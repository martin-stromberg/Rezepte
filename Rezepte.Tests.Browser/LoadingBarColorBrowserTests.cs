using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarColorBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
{
    private static readonly IReadOnlyList<string> ConfiguredPalette = LoadingBarOptions.DefaultColors;

    [SkippableFact]
    public async Task LinkClick_UsesColorFromConfiguredPalette()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);
        await pageObject.WaitUntilLoadingBarActiveAsync();

        var color = await pageObject.GetLoadingBarColorAsync();

        color.Should().NotBeNullOrEmpty();
        ConfiguredPalette.Should().Contain(color);
    }

    [SkippableFact]
    public async Task SecondClickDuringRunningAnimation_ChangesColor()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 3000);

        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);
        await pageObject.WaitUntilLoadingBarActiveAsync();
        var firstColor = await pageObject.GetLoadingBarColorAsync();
        firstColor.Should().NotBeNullOrEmpty();

        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);
        await pageObject.WaitUntilLoadingBarColorChangedAsync(firstColor!);
        var secondColor = await pageObject.GetLoadingBarColorAsync();

        secondColor.Should().NotBe(firstColor);
    }
}
