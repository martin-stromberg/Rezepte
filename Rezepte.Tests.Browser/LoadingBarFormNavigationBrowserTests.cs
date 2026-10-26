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

        // Delay the search navigation so the request stays in flight long enough for the
        // assertion below to observe the active loading bar.
        await pageObject.DelayRouteAsync(LoadingBarPageObject.SearchRouteGlob, 1500);

        // The search submit reaches /recipes/search in one of two ways: once the interactive
        // circuit is up, NavigationManager.NavigateTo() runs as a Blazor enhanced navigation
        // ('enhancednavigationstart' activates the bar); if the circuit is not connected yet,
        // the form's native action="/recipes/search" fallback performs a real navigation
        // ('beforeunload' activates the bar). Either way the request matches the delayed route
        // above and the bar must activate while it is in flight. Starting the wait BEFORE the
        // submit is kept on purpose: it guarantees the assertion observes the bar's activation
        // regardless of which of the two mechanisms fires.
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
