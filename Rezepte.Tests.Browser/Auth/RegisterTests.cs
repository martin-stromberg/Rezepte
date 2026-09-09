using FluentAssertions;
using Microsoft.Playwright;
using Rezepte.Tests.Browser.Auth;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests for the registration flow with and without demo data.
/// </summary>
[Collection(RegisterTestCollection.Name)]
public class RegisterTests
{
    private const string Password = "DemoTest!123";
    private const int PollIntervalMilliseconds = 500;
    private const int PageContentTimeoutMilliseconds = 20000;
    private const int FinalAssertRetryMilliseconds = 30000;
    // The seeding job also writes the bundled demo images into the database, so
    // generous headroom is needed on slower machines.
    private const int DemoDataTimeoutMilliseconds = 120000;
    private const int ExpectedCookbookCount = 5;
    private const int ExpectedRecipeCount = 43;
    private const int ExpectedCalendarEventCount = 5;
    private const int MinExpectedShoppingListItems = 1;
    private const int EmptyCount = 0;

    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RegisterTestsAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public RegisterTests(PlaywrightBrowserFixture browserFixture, RegisterTestsAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that registering with demo data creates cookbooks, recipes, calendar events and shopping list items.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Register_WithDemoData_CreatesDemoData()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        // The application only allows access to /register while no user exists, so the app
        // is restarted against an empty database before exercising the registration flow.
        await _appFixture.RestartWithEmptyDatabaseAsync();

        var username = GenerateUniqueUsername();
        await using var registerPage = await RegisterPage.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);

        await registerPage.GotoAsync();
        await registerPage.FillUsernameAsync(username);
        await registerPage.FillEmailAsync($"{username}@example.invalid");
        await registerPage.FillPasswordAsync(Password);
        await registerPage.CheckCreateDemoDataAsync(true);
        await registerPage.SubmitAsync();

        await registerPage.Page.WaitForURLAsync(
            url => url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });

        await new LoginPage(registerPage.Page, _appFixture.BaseAddress).LoginAsync(username, Password);
        await DemoDataWaitHelper.WaitForDemoDataAsync(registerPage.Page, expected: true, DemoDataTimeoutMilliseconds);

        // The seeded state is already proven by WaitForDemoDataAsync, but the individual page
        // reads are still retried: the interactive pages can transiently render empty right
        // after a navigation while the app settles after the seeding job.
        (await EventuallyReadCountAsync(registerPage.Page, CountCookbooksAsync, count => count == ExpectedCookbookCount))
            .Should().Be(ExpectedCookbookCount);
        (await EventuallyReadCountAsync(registerPage.Page, CountRecipesAsync, count => count == ExpectedRecipeCount))
            .Should().Be(ExpectedRecipeCount);
        (await EventuallyReadCountAsync(registerPage.Page, CountCalendarEventsAsync, count => count == ExpectedCalendarEventCount))
            .Should().Be(ExpectedCalendarEventCount);
        (await EventuallyReadCountAsync(registerPage.Page, CountShoppingListItemsAsync, count => count > EmptyCount))
            .Should().BeGreaterThan(EmptyCount);
    }

    /// <summary>
    /// Verifies that registering without demo data leaves the lists empty.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Register_WithoutDemoData_LeavesListsEmpty()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        // See Register_WithDemoData_CreatesDemoData for why the empty-database restart is needed.
        await _appFixture.RestartWithEmptyDatabaseAsync();

        var username = GenerateUniqueUsername();
        await using var registerPage = await RegisterPage.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);

        await registerPage.GotoAsync();
        await registerPage.FillUsernameAsync(username);
        await registerPage.FillEmailAsync($"{username}@example.invalid");
        await registerPage.FillPasswordAsync(Password);
        await registerPage.CheckCreateDemoDataAsync(false);
        await registerPage.SubmitAsync();

        await registerPage.Page.WaitForURLAsync(
            url => url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });

        await new LoginPage(registerPage.Page, _appFixture.BaseAddress).LoginAsync(username, Password);
        await DemoDataWaitHelper.WaitForDemoDataAsync(registerPage.Page, expected: false, 5000);

        (await CountCookbooksAsync(registerPage.Page)).Should().Be(EmptyCount);
        (await CountRecipesAsync(registerPage.Page)).Should().Be(EmptyCount);
        (await CountCalendarEventsAsync(registerPage.Page)).Should().Be(EmptyCount);
        (await CountShoppingListItemsAsync(registerPage.Page)).Should().Be(EmptyCount);
    }

    private static string GenerateUniqueUsername()
    {
        // The username validator only accepts 3-20 characters, so the unique suffix is
        // truncated to fit (4 + 16 = 20 characters, still collision-safe for test runs).
        return $"e2e_{Guid.NewGuid():N}"[..20];
    }

    private static string GetBaseAddress(IPage page)
    {
        return new Uri(page.Url).GetLeftPart(UriPartial.Authority);
    }

    /// <summary>
    /// Repeatedly navigates to and counts a page until the count satisfies the predicate or
    /// the retry budget is exhausted. Returns the last observed count so the caller can assert.
    /// </summary>
    /// <param name="page">The page to read on.</param>
    /// <param name="countAsync">The counting function.</param>
    /// <param name="satisfied">The predicate the count must satisfy.</param>
    /// <returns>The last observed count.</returns>
    private static async Task<int> EventuallyReadCountAsync(IPage page, Func<IPage, Task<int>> countAsync, Func<int, bool> satisfied)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(FinalAssertRetryMilliseconds);
        var last = await countAsync(page);
        while (!satisfied(last) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollIntervalMilliseconds);
            last = await countAsync(page);
        }

        return last;
    }

    /// <summary>
    /// Waits until the given page has rendered either its content or its empty/error state.
    /// The interactive server pages render a "Lade…" placeholder first and populate the
    /// lists asynchronously via API calls, so counting without waiting races the render.
    /// </summary>
    /// <param name="page">The page to wait on.</param>
    /// <param name="settledSelector">A selector that appears once the page has settled.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task WaitForSettledContentAsync(IPage page, string settledSelector)
    {
        try
        {
            await page.WaitForSelectorAsync(settledSelector, new PageWaitForSelectorOptions { Timeout = PageContentTimeoutMilliseconds });
        }
        catch (TimeoutException)
        {
            // Fall through: the caller still counts whatever is currently rendered.
        }
    }

    private static async Task<int> CountCookbooksAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/cookbooks");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled states: the grid with cards or the "no cookbooks" info alert.
        await WaitForSettledContentAsync(page, ".cookbook-grid, .alert-info");
        return await page.Locator(".cookbook-card").CountAsync();
    }

    private static async Task<int> CountRecipesAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/recipes/search?q=&page=1&pageSize=100");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled states: result items or the "no results"/error alert.
        await WaitForSettledContentAsync(page, ".list-group-item, .alert");
        return await page.Locator(".list-group-item").CountAsync();
    }

    private static async Task<int> CountCalendarEventsAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/calendar");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled states: the calendar grid (with or without entries) or the error alert.
        await WaitForSettledContentAsync(page, ".calendar-root, .alert-danger");
        // Count both card variants: ".recipe-item" needs the per-recipe preview fetch to have
        // completed, while ".event-item" is the fallback card rendered for the same scheduled
        // events when the preview is unavailable (e.g. slow or aborted request on CI).
        return await page.Locator(".recipe-item, .event-item").CountAsync();
    }

    private static async Task<int> CountShoppingListItemsAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/shopping-list");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled state: the shopping list grid renders once loading is done, even when empty.
        await WaitForSettledContentAsync(page, ".shopping-list-grid");
        return await page.Locator(".shopping-item").CountAsync();
    }

    private static class DemoDataWaitHelper
    {
        public static async Task WaitForDemoDataAsync(IPage page, bool expected, int timeoutMilliseconds)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            var last = string.Empty;
            while (DateTime.UtcNow < deadline)
            {
                var (cookbooks, recipes, calendar, shopping) = await ReadCountsAsync(page);
                last = $"cookbooks={cookbooks}, recipes={recipes}, calendar={calendar}, shopping={shopping}";

                var actual = expected
                    ? cookbooks >= ExpectedCookbookCount && recipes >= ExpectedRecipeCount && calendar >= ExpectedCalendarEventCount && shopping >= MinExpectedShoppingListItems
                    : cookbooks == EmptyCount && recipes == EmptyCount && calendar == EmptyCount && shopping == EmptyCount;
                if (actual)
                {
                    return;
                }

                await Task.Delay(PollIntervalMilliseconds);
            }

            throw new TimeoutException($"Demo data state did not match expected value {expected} within {timeoutMilliseconds}ms. Last observed: {last} (page: {page.Url})");
        }

        private static async Task<(int Cookbooks, int Recipes, int Calendar, int Shopping)> ReadCountsAsync(IPage page)
        {
            var cookbooks = await CountCookbooksAsync(page);
            var recipes = await CountRecipesAsync(page);
            var calendar = await CountCalendarEventsAsync(page);
            var shopping = await CountShoppingListItemsAsync(page);
            return (cookbooks, recipes, calendar, shopping);
        }
    }
}
