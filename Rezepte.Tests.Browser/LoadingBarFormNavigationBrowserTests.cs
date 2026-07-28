using FluentAssertions;
using Microsoft.Playwright;
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
        await pageObject.Page.FillAsync("#nav-search", "Suppe");
        await pageObject.Page.ClickAsync("button[aria-label='Suche starten']", new PageClickOptions { NoWaitAfter = true });

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
        await pageObject.GotoAsync(LoadingBarPageObject.ShoppingListHref);
        await pageObject.Page.ClickAsync("button[aria-label='Bearbeiten']");
        await pageObject.Page.ClickAsync("button[title='Gruppe hinzufuegen']");
        await pageObject.Page.WaitForSelectorAsync("form.shopping-add-row");

        await pageObject.Page.FillAsync("form.shopping-add-row input[aria-label='Zutat hinzufuegen']", "Testzutat");
        await pageObject.Page.ClickAsync("form.shopping-add-row button[type='submit']");

        // There is no positive signal to poll for here: the bar must never activate, so a
        // deterministic wait is used before checking that it never did.
        await pageObject.Page.WaitForTimeoutAsync(500);

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeFalse();
    }
}
