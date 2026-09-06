using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Rezepte.Web.Configuration;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the loading bar color is chosen from the configured palette.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class LoadingBarColorBrowserTests
{
    private static readonly IReadOnlyList<string> ConfiguredPalette = LoadingBarOptions.DefaultColors;

    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingBarColorBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public LoadingBarColorBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that a navigation link click shows a loading bar color from the configured palette.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task LinkClick_UsesColorFromConfiguredPalette()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        await pageObject.DelayRouteAsync(LoadingBarPageObject.CookbooksRouteGlob, 1500);
        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.CookbooksHref);
        await pageObject.WaitUntilLoadingBarActiveAsync();

        var color = await pageObject.GetLoadingBarColorAsync();

        color.Should().NotBeNullOrEmpty();
        ConfiguredPalette.Should().Contain(color);
    }

    /// <summary>
    /// Verifies that a second click during an already running animation changes the loading bar color.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task SecondClickDuringRunningAnimation_ChangesColor()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

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
