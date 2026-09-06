using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the loading bar behavior for navigation and interactive forms.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class LoadingBarFormNavigationBrowserTests
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingBarFormNavigationBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public LoadingBarFormNavigationBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that submitting the navigation search shows the loading bar.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task SearchSubmit_ShowsLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        // Delay the search navigation so the old document stays alive long enough for the assertion
        // below to observe the loading bar while the request is still in flight.
        await pageObject.DelayRouteAsync(LoadingBarPageObject.SearchRouteGlob, 1500);

        // This search submit performs a genuine full-page navigation (NavigationManager.NavigateTo()
        // from the server-side handler), unlike an enhanced-navigation link click. Starting the wait
        // BEFORE triggering the submit (instead of after) is essential here, not just style: once the
        // submit is in flight, Playwright's WaitForFunctionAsync call would bind to whatever document
        // is current *at the moment it is issued*. Issued after the click, on a fast enough navigation
        // it can lose the race and bind to the *new*, already-loaded (and therefore inactive) document,
        // producing a timeout no matter how large - the assertion would then be observing the wrong
        // page entirely. Starting the wait first guarantees it binds to the old document, where the
        // 'beforeunload'-triggered activation actually happens.
        var loadingBarActiveTask = pageObject.WaitUntilLoadingBarActiveAsync(timeoutMilliseconds: 5000);
        await pageObject.SubmitNavigationSearchAsync("Suppe");
        await loadingBarActiveTask;
    }

    /// <summary>
    /// Verifies that an interactive form submit without navigation does not activate the loading bar.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task InteractiveFormSubmitWithoutNavigation_DoesNotActivateLoadingBar()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

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
