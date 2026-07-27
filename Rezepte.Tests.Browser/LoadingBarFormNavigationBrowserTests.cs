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
        await pageObject.Page.DelayNavigationAsync("**/recipes/search*", 1500);
        await pageObject.Page.FillAsync("#nav-search", "Suppe");
        await pageObject.Page.ClickAsync("button[aria-label='Suche starten']", new PageClickOptions { NoWaitAfter = true });

        await pageObject.WaitUntilLoadingBarActiveAsync();

        (await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();
    }
}
