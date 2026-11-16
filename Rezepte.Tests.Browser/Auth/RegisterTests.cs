using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using Rezepte.Tests.Browser.Auth;
using Rezepte.Tests.Browser.Infrastructure;
using Rezepte.Web.Services.BackgroundJobs;
using Rezepte.Web.Services.BackgroundJobs.Handlers;
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
    private const int StableCountReads = 8;
    private const int StableCountIntervalMilliseconds = 400;
    private const int MonthNavClickSettleMilliseconds = 5000;
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
        await DemoDataWaitHelper.WaitForDemoDataAsync(registerPage.Page, expected: true, DemoDataTimeoutMilliseconds, _appFixture.DatabasePath);

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
        await DemoDataWaitHelper.WaitForDemoDataAsync(registerPage.Page, expected: false, 5000, _appFixture.DatabasePath);

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

    /// <summary>
    /// Counts the elements matching the given selector several times and returns the maximum
    /// observed count. Transient renders only ever show fewer items than the settled state,
    /// never more, because Blazor applies render batches atomically - so the maximum is the
    /// reliable count when a read races the reconnecting interactive circuit. The loop exits
    /// early once two consecutive reads return the same non-zero count: counts can only
    /// transiently be lower, never partially higher, so a stable non-zero pair means the
    /// loading state has passed. Pages that stay empty run the full read window.
    /// </summary>
    /// <param name="page">The page to read on.</param>
    /// <param name="selector">The selector whose elements are counted.</param>
    /// <returns>The maximum count observed across the reads.</returns>
    private static async Task<int> CountStablyAsync(IPage page, string selector)
    {
        var max = 0;
        var previous = -1;
        for (var read = 0; read < StableCountReads; read++)
        {
            var current = await page.Locator(selector).CountAsync();
            max = Math.Max(max, current);
            if (current > 0 && current == previous)
            {
                break;
            }

            previous = current;
            if (read < StableCountReads - 1)
            {
                await Task.Delay(StableCountIntervalMilliseconds);
            }
        }

        return max;
    }

    private static async Task<int> CountCookbooksAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/cookbooks");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled states: the grid with cards or the "no cookbooks" info alert.
        await WaitForSettledContentAsync(page, ".cookbook-grid, .alert-info");
        return await CountStablyAsync(page, ".cookbook-card");
    }

    private static async Task<int> CountRecipesAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/recipes/search?q=&page=1&pageSize=100");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled states: result items or the "no results"/error alert.
        await WaitForSettledContentAsync(page, ".list-group-item, .alert");
        return await CountStablyAsync(page, ".list-group-item");
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
        var count = await CountStablyAsync(page, ".recipe-item, .event-item");

        // The demo seeding plans one event per day for the next five days, which can span a
        // month boundary while the page renders a single month at a time - so the next month
        // view is counted as well and both are summed.
        try
        {
            var monthLabel = page.Locator("xpath=//button[normalize-space()='‹']/following-sibling::div[1]");
            var previousLabel = await monthLabel.InnerTextAsync();

            // Clicks dispatched before the interactive circuit is connected are dropped
            // silently, so the navigation is retried until the label flips or the budget runs
            // out. The settle window avoids premature re-clicks on a congested circuit that
            // could advance two months and skip the event-bearing one.
            var labelDeadline = DateTime.UtcNow.AddMilliseconds(PageContentTimeoutMilliseconds);
            while (DateTime.UtcNow < labelDeadline && await monthLabel.InnerTextAsync() == previousLabel)
            {
                await page.Locator("button:has-text('›')").First.ClickAsync();

                // The label flips to the next month in the same render pass that drops the grid
                // for the reload, so once it changed, a later-settled grid is guaranteed to show
                // the new month and not a stale snapshot of the old one.
                var flipDeadline = DateTime.UtcNow.AddMilliseconds(MonthNavClickSettleMilliseconds);
                while (DateTime.UtcNow < flipDeadline && await monthLabel.InnerTextAsync() == previousLabel)
                {
                    await Task.Delay(PollIntervalMilliseconds);
                }
            }

            // Only a genuinely flipped label leads to a second count: counting the same month
            // again would double-count its events.
            var navigated = await monthLabel.InnerTextAsync() != previousLabel;
            if (navigated)
            {
                await WaitForSettledContentAsync(page, ".calendar-root, .alert-danger");
                return count + await CountStablyAsync(page, ".recipe-item, .event-item");
            }
        }
        catch (TimeoutException)
        {
            // The month navigation is best-effort: when the calendar header never renders
            // (e.g. redirect or error page), the current view is counted alone.
        }

        return count;
    }

    private static async Task<int> CountShoppingListItemsAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/shopping-list");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        // Settled state: the shopping list grid renders once loading is done, even when empty.
        await WaitForSettledContentAsync(page, ".shopping-list-grid");
        return await CountStablyAsync(page, ".shopping-item");
    }

    private static class DemoDataWaitHelper
    {
        /// <summary>
        /// Polls the UI counts until they match the expected demo-data state or the timeout is
        /// reached. When <paramref name="databasePath"/> is given, the newest seed job row is
        /// watched alongside: a job that ended in <see cref="BackgroundJobStatus.Failed"/> or
        /// <see cref="BackgroundJobStatus.Cancelled"/> fails the wait immediately with the
        /// stored error instead of letting the poll run blind until the timeout.
        /// </summary>
        /// <param name="page">The page to read the counts on.</param>
        /// <param name="expected">Whether the seeded counts are expected to appear.</param>
        /// <param name="timeoutMilliseconds">The overall wait budget.</param>
        /// <param name="databasePath">The SQLite database file of the app under test.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        public static async Task WaitForDemoDataAsync(IPage page, bool expected, int timeoutMilliseconds, string? databasePath = null)
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

                var job = databasePath is null ? null : await ReadLatestSeedJobAsync(databasePath);
                if (job?.Status is BackgroundJobStatus.Failed or BackgroundJobStatus.Cancelled)
                {
                    throw new InvalidOperationException(
                        $"The demo data seeding job ended with status {job.Status}: {job.Error ?? "(no error message stored)"}");
                }

                await Task.Delay(PollIntervalMilliseconds);
            }

            var jobInfo = "seed job state unavailable";
            if (databasePath is not null)
            {
                var job = await ReadLatestSeedJobAsync(databasePath);
                jobInfo = job is null ? "seed job state unavailable"
                    : !job.Found ? "no seed-demo-data job row exists (was it ever enqueued?)"
                    : $"seed job status={job.Status}, error={job.Error ?? "(none)"}";
            }

            throw new TimeoutException($"Demo data state did not match expected value {expected} within {timeoutMilliseconds}ms. Last observed: {last}; {jobInfo} (page: {page.Url})");
        }

        /// <summary>
        /// Reads the newest <c>seed-demo-data</c> row from the BackgroundJobs table. Returns
        /// <see langword="null"/> when the table cannot be read (database locked, schema not
        /// migrated yet) - the wait must never fail because of its own diagnostics.
        /// </summary>
        /// <param name="databasePath">The SQLite database file of the app under test.</param>
        /// <returns>The observed job row, or <see langword="null"/> when unreadable.</returns>
        private static async Task<SeedJobObservation?> ReadLatestSeedJobAsync(string databasePath)
        {
            try
            {
                await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT Status, Error
                    FROM BackgroundJobs
                    WHERE JobType = $type
                    ORDER BY CreatedAt DESC
                    LIMIT 1
                    """;
                command.Parameters.AddWithValue("$type", DemoDataSeedingJobHandler.JobTypeName);
                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return new SeedJobObservation(Found: false, Status: null, Error: null);
                }

                return new SeedJobObservation(
                    Found: true,
                    Status: (BackgroundJobStatus)reader.GetInt32(0),
                    Error: reader.IsDBNull(1) ? null : reader.GetString(1));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The observed state of the newest demo data seeding job row.
        /// </summary>
        /// <param name="Found">Whether a job row exists at all.</param>
        /// <param name="Status">The job status.</param>
        /// <param name="Error">The stored job error.</param>
        /// <returns>The result.</returns>
        private sealed record SeedJobObservation(bool Found, BackgroundJobStatus? Status, string? Error);

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
