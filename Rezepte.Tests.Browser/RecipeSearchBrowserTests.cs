using FluentAssertions;
using Microsoft.Playwright;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser regression tests for the recipe search page (/recipes/search).
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class RecipeSearchBrowserTests
{
    private const int PageContentTimeoutMilliseconds = 20000;
    private const int InteractionRetryTimeoutMilliseconds = 15000;

    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipeSearchBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public RecipeSearchBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// A search submitted through the navigation bar shows the seeded recipe in the result list.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task NavbarSearch_ShowsSeededRecipe()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var title = $"E2E-Suche-Navbar-{Guid.NewGuid():N}";
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, title);

        await pageObject.SubmitNavigationSearchAsync(title);

        await pageObject.Page.WaitForURLAsync(
            "**/recipes/search*",
            new PageWaitForURLOptions { Timeout = PageContentTimeoutMilliseconds });
        await pageObject.Page
            .Locator($".list-group-item:has-text('{title}')")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// A direct request of the search page with a query parameter shows the seeded recipe,
    /// covering the prerender/SSR path of the page.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task DirectUrl_WithQuery_ShowsResults()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var title = $"E2E-Suche-Direkt-{Guid.NewGuid():N}";
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, title);

        await pageObject.GotoAsync($"/recipes/search?q={Uri.EscapeDataString(title)}");

        // Expect(...) retries: the prerendered result list is briefly hidden again while the
        // interactive circuit repeats the same search (isLoading), so a single CountAsync
        // can hit the transient empty window.
        var item = pageObject.Page.Locator($".list-group-item:has-text('{title}')");
        await Assertions.Expect(item).ToHaveCountAsync(
            1,
            new LocatorAssertionsToHaveCountOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// A search without matches shows the no-results alert and not the failure alert.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Search_WithoutMatches_ShowsNoSearchResults()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        await pageObject.GotoAsync($"/recipes/search?q={Uri.EscapeDataString($"E2E-KeinTreffer-{Guid.NewGuid():N}")}");

        var alert = pageObject.Page.Locator(".alert-secondary");
        await Assertions.Expect(alert).ToContainTextAsync(
            "Keine Treffer.",
            new LocatorAssertionsToContainTextOptions { Timeout = PageContentTimeoutMilliseconds });
        (await pageObject.Page.Locator(".alert-danger").CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// With more results than the page size, the next-page button moves to the second page.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Pagination_NextPage_ShowsPageTwo()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var prefix = $"E2ESeiten{Guid.NewGuid():N}";
        var titles = Enumerable.Range(1, 12).Select(i => $"{prefix}-{i:00}").ToArray();
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, titles);

        await pageObject.GotoAsync($"/recipes/search?q={prefix}&page=1&pageSize=5");

        // Expect(...) retries: the prerendered result list is briefly hidden again while the
        // interactive circuit repeats the same search (isLoading), so a single CountAsync
        // can hit the transient empty window.
        var items = pageObject.Page.Locator(".list-group-item");
        await Assertions.Expect(items).ToHaveCountAsync(
            5,
            new LocatorAssertionsToHaveCountOptions { Timeout = PageContentTimeoutMilliseconds });
        await Assertions.Expect(pageObject.Page.Locator(".pagination")).ToContainTextAsync(
            "Seite 1 von 3",
            new LocatorAssertionsToContainTextOptions { Timeout = PageContentTimeoutMilliseconds });

        // A click that landed belatedly must not be followed by another one, which would
        // overshoot to page three - ClickUntilTargetStateAsync re-checks before each retry.
        var nextButton = pageObject.Page.Locator("button[aria-label='Nächste']");
        var pageTwoLabel = pageObject.Page.GetByText("Seite 2 von 3");
        await ClickUntilTargetStateAsync(pageObject.Page, nextButton, pageTwoLabel, "page=2");

        await Assertions.Expect(items).ToHaveCountAsync(
            5,
            new LocatorAssertionsToHaveCountOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// Submitting the in-page search form updates the result list.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task InPageSearch_Submit_UpdatesResultList()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var title = $"E2E-Formular-{Guid.NewGuid():N}";
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, title);

        await pageObject.GotoAsync($"/recipes/search?q={Uri.EscapeDataString($"E2E-AndererBegriff-{Guid.NewGuid():N}")}");
        await pageObject.Page
            .Locator(".alert-secondary")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });

        // The prerender/circuit double-run made this test flaky: a circuit re-render could
        // write q back into #search-q after FillAsync. The page now only emits the value
        // attribute when q actually changes, so typed text can no longer be overwritten;
        // the whole fill→submit interaction is still retried within the deadline budget as
        // a safeguard against residual races (e.g. a reload between fill and submit or a
        // swallowed submit). Before each retry the target URL is checked: a submit that
        // landed belatedly must not be followed by another one.
        var searchInput = pageObject.Page.Locator("#search-q");
        var targetUrl = $"**/recipes/search?q={Uri.EscapeDataString(title)}*";
        var deadline = DateTime.UtcNow.AddMilliseconds(InteractionRetryTimeoutMilliseconds);
        while (true)
        {
            if (pageObject.Page.Url.Contains($"q={Uri.EscapeDataString(title)}", StringComparison.Ordinal))
            {
                break;
            }

            await pageObject.Page.FillAsync("#search-q", title);
            try
            {
                await Assertions.Expect(searchInput).ToHaveValueAsync(
                    title,
                    new LocatorAssertionsToHaveValueOptions { Timeout = 5000 });
            }
            catch (PlaywrightException) when (DateTime.UtcNow < deadline)
            {
                // Retry: a re-render wrote the old q back over the filled value.
                continue;
            }

            await pageObject.Page.ClickAsync("form:has(#search-q) button[type='submit']");
            try
            {
                // First prove the GET form actually navigated before waiting on the result list —
                // otherwise a swallowed submit and a failed search are indistinguishable.
                await pageObject.Page.WaitForURLAsync(
                    targetUrl,
                    new PageWaitForURLOptions { Timeout = 5000 });
                break;
            }
            catch (TimeoutException) when (DateTime.UtcNow < deadline)
            {
                // Retry: the submit was likely swallowed before the interactive circuit was up.
            }
        }

        // The field must show the submitted term: the fresh mount re-emits the value
        // attribute because q changed, proving the q→field sync on the new-mount path.
        await Assertions.Expect(searchInput).ToHaveValueAsync(
            title,
            new LocatorAssertionsToHaveValueOptions { Timeout = PageContentTimeoutMilliseconds });

        await pageObject.Page
            .Locator($".list-group-item:has-text('{title}')")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// A value typed into the in-page search field survives the prerender→interactive
    /// circuit transition and a full interactive render cycle on the same instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task InPageSearch_TypedValue_SurvivesInteractiveCircuit()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var prefix = $"E2E-Tipp-{Guid.NewGuid():N}";
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, $"{prefix}-01", $"{prefix}-02");

        // The prerendered result list is only the entry point, not a circuit barrier.
        await pageObject.GotoAsync($"/recipes/search?q={prefix}&page=1&pageSize=1");
        await pageObject.Page
            .Locator(".list-group-item")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });

        var typedValue = $"E2E-Getippt-{Guid.NewGuid():N}";
        await pageObject.Page.FillAsync("#search-q", typedValue);

        // The pagination buttons only work once the interactive circuit is up, so a
        // successful click proves the circuit was attached and all pre-click frames had
        // landed. NavigateTo does not remount the component — the same instance gets new
        // parameters, so every render in between ran on that instance.
        var nextButton = pageObject.Page.Locator("button[aria-label='Nächste']");
        var pageTwoLabel = pageObject.Page.GetByText("Seite 2 von 2");
        await ClickUntilTargetStateAsync(pageObject.Page, nextButton, pageTwoLabel, "page=2");

        // End-state barrier instead of a DOM-transition observer: the pagination block is
        // only rendered when !isLoading with results, so "Seite 2 von 2" proves the
        // interactive post-navigation render completed — robust whether the spinner
        // toggled visibly or stayed up through a superseded initial search.
        await Assertions.Expect(pageObject.Page.Locator(".pagination")).ToContainTextAsync(
            "Seite 2 von 2",
            new LocatorAssertionsToContainTextOptions { Timeout = PageContentTimeoutMilliseconds });

        // Without the app fix at least one of the proven circuit frames would have written
        // q back over the typed value via the value attribute's property write.
        await Assertions.Expect(pageObject.Page.Locator("#search-q")).ToHaveValueAsync(
            typedValue,
            new LocatorAssertionsToHaveValueOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// Submitting the navigation bar search while the search page is already displayed
    /// synchronizes the in-page search field to the new query (new-mount path).
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task NavbarSearch_WhileOnSearchPage_UpdatesSearchField()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);
        var title = $"E2E-NavbarSync-{Guid.NewGuid():N}";
        await RecipeSeedHelper.SeedRecipesAsync(pageObject.Page, _appFixture.BaseAddress, title);

        await pageObject.GotoAsync($"/recipes/search?q={Uri.EscapeDataString($"E2E-Fremdbegriff-{Guid.NewGuid():N}")}");
        await pageObject.Page
            .Locator(".alert-secondary")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });

        // The navbar form lives outside the interactive boundary, so the submit is a
        // native GET navigation and remounts the page component.
        await pageObject.SubmitNavigationSearchAsync(title);

        await pageObject.Page.WaitForURLAsync(
            $"**/recipes/search?q={Uri.EscapeDataString(title)}*",
            new PageWaitForURLOptions { Timeout = PageContentTimeoutMilliseconds });

        await Assertions.Expect(pageObject.Page.Locator("#search-q")).ToHaveValueAsync(
            title,
            new LocatorAssertionsToHaveValueOptions { Timeout = PageContentTimeoutMilliseconds });

        await pageObject.Page
            .Locator($".list-group-item:has-text('{title}')")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });
    }

    /// <summary>
    /// Clicks a button that only works once the interactive circuit is up, retrying until
    /// the target state is reached or the <see cref="InteractionRetryTimeoutMilliseconds"/>
    /// budget runs out. Before each retry the target state is checked: a click that landed
    /// belatedly must not be followed by another one, which would overshoot the target.
    /// The last failure is not swallowed - after the deadline it propagates to the caller.
    /// </summary>
    /// <param name="page">The page the interaction runs on.</param>
    /// <param name="button">The button to click.</param>
    /// <param name="targetLabel">The locator whose visibility marks the target state.</param>
    /// <param name="targetUrlFragment">A URL fragment that also marks the target state.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task ClickUntilTargetStateAsync(IPage page, ILocator button, ILocator targetLabel, string targetUrlFragment)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(InteractionRetryTimeoutMilliseconds);
        while (true)
        {
            if (page.Url.Contains(targetUrlFragment, StringComparison.Ordinal) || await targetLabel.IsVisibleAsync())
            {
                return;
            }

            await button.ClickAsync();
            try
            {
                await targetLabel.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                return;
            }
            catch (TimeoutException) when (DateTime.UtcNow < deadline)
            {
                // Retry: the click was likely swallowed before the interactive circuit was up.
            }
        }
    }
}
