using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarSafetyTimeoutBrowserTests(
    PlaywrightBrowserFixture browserFixture,
    LoadingBarSafetyTimeoutBrowserTests.ShortMaxVisibleDurationFixture appFixture)
    : IClassFixture<LoadingBarSafetyTimeoutBrowserTests.ShortMaxVisibleDurationFixture>
{
    [SkippableFact]
    public async Task WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        // Route the target navigation but never fulfill, continue, or abort it, so the request hangs forever.
        await pageObject.BlockRouteAsync(LoadingBarPageObject.ShoppingListRouteGlob);

        await pageObject.ClickNavigationLinkAsync(LoadingBarPageObject.ShoppingListHref);

        await pageObject.WaitUntilLoadingBarActiveAsync();
        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();

        await pageObject.WaitUntilLoadingBarHiddenAsync(timeoutMilliseconds: 2000);

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }

    public sealed class ShortMaxVisibleDurationFixture() : ConfiguredRezepteAppFixture(new Dictionary<string, string?>
    {
        ["LoadingBar__MaxVisibleDuration"] = "800ms"
    });
}
