using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarFormNavigationBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
{
    [SkippableFact]
    public async Task SearchSubmit_ShowsLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        // Delay the search navigation so the old document stays alive long enough for the assertion
        // below to observe the loading bar while the request is still in flight.
        await pageObject.DelayRouteAsync(LoadingBarPageObject.SearchRouteGlob, 1500);
        await pageObject.SubmitNavigationSearchAsync("Suppe");

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();
    }

    [SkippableFact]
    public async Task InteractiveFormSubmitWithoutNavigation_DoesNotActivateLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        // The shopping list's "add item" form uses @onsubmit:preventDefault on an
        // @rendermode InteractiveServer component: the submit is handled entirely over the
        // circuit and never triggers a navigation. loadingBar.js's capture-phase submit
        // listener must not activate the bar for this kind of submit.
        await pageObject.SubmitInteractiveShoppingListItemAsync("Testzutat");

        // There is no positive signal to poll for here: the bar must never activate, so a
        // deterministic wait is used before checking that it never did.
        await pageObject.Page.WaitForTimeoutAsync(500);

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
