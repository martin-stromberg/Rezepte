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

        // The pagination buttons only work once the interactive circuit is up, so the click is
        // retried until the page-two label appears (or the overall budget runs out). Before each
        // retry the target state is checked: a click that landed belatedly must not be followed
        // by another one, which would overshoot to page three.
        var nextButton = pageObject.Page.Locator("button[aria-label='Nächste']");
        var pageTwoLabel = pageObject.Page.GetByText("Seite 2 von 3");
        var deadline = DateTime.UtcNow.AddMilliseconds(InteractionRetryTimeoutMilliseconds);
        while (true)
        {
            if (pageObject.Page.Url.Contains("page=2", StringComparison.Ordinal) || await pageTwoLabel.IsVisibleAsync())
            {
                break;
            }

            await nextButton.ClickAsync();
            try
            {
                await pageTwoLabel.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                break;
            }
            catch (TimeoutException) when (DateTime.UtcNow < deadline)
            {
                // Retry: the click was likely swallowed before the interactive circuit was up.
            }
        }

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

        await pageObject.Page.FillAsync("#search-q", title);
        await Assertions.Expect(pageObject.Page.Locator("#search-q")).ToHaveValueAsync(
            title,
            new LocatorAssertionsToHaveValueOptions { Timeout = PageContentTimeoutMilliseconds });
        await pageObject.Page.ClickAsync("form:has(#search-q) button[type='submit']");

        // First prove the GET form actually navigated before waiting on the result list —
        // otherwise a swallowed submit and a failed search are indistinguishable.
        await pageObject.Page.WaitForURLAsync(
            $"**/recipes/search?q={Uri.EscapeDataString(title)}*",
            new PageWaitForURLOptions { Timeout = PageContentTimeoutMilliseconds });

        await pageObject.Page
            .Locator($".list-group-item:has-text('{title}')")
            .WaitForAsync(new LocatorWaitForOptions { Timeout = PageContentTimeoutMilliseconds });
    }
}
