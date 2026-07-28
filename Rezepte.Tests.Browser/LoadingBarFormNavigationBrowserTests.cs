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

        // Unlike an enhanced-navigation link click, this search submit performs a genuine
        // full-page navigation (NavigationManager.NavigateTo() from the server-side handler).
        // Once such a navigation is in flight, Playwright's single-shot queries against the
        // page (e.g. GetAttributeAsync) block until the navigation settles - they would only
        // ever observe the *new* page's fresh (inactive) bar, never the old page's active one.
        // WaitForFunctionAsync's polling does not have that limitation, so a successful wait
        // here (it throws on timeout) is itself the proof that the bar became active in time.
        await pageObject.WaitUntilLoadingBarActiveAsync();
    }

    [SkippableFact]
    public async Task InteractiveFormSubmitWithoutNavigation_DoesNotActivateLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        // The shopping list's "add item" form uses @onsubmit:preventDefault on an
        // @rendermode InteractiveServer component: the submit is handled entirely over the
        // circuit and never triggers a navigation, so neither Blazor's 'enhancednavigationstart'
        // nor the browser's own 'beforeunload' fire for it - the bar must stay inactive.
        await pageObject.SubmitInteractiveShoppingListItemAsync("Testzutat");

        // There is no positive signal to poll for here: the bar must never activate, so a
        // deterministic wait is used before checking that it never did.
        await pageObject.Page.WaitForTimeoutAsync(500);

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
