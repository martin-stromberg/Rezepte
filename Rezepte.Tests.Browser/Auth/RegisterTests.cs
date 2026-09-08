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
    private const int DemoDataTimeoutMilliseconds = 15000;
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

        (await CountCookbooksAsync(registerPage.Page)).Should().Be(ExpectedCookbookCount);
        (await CountRecipesAsync(registerPage.Page)).Should().Be(ExpectedRecipeCount);
        (await CountCalendarEventsAsync(registerPage.Page)).Should().Be(ExpectedCalendarEventCount);
        (await CountShoppingListItemsAsync(registerPage.Page)).Should().BeGreaterThan(EmptyCount);
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

    private static async Task<int> CountCookbooksAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/cookbooks");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        return await page.Locator(".cookbook-card").CountAsync();
    }

    private static async Task<int> CountRecipesAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/recipes/search?q=&page=1&pageSize=100");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        return await page.Locator(".list-group-item").CountAsync();
    }

    private static async Task<int> CountCalendarEventsAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/calendar");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
        return await page.Locator(".recipe-item").CountAsync();
    }

    private static async Task<int> CountShoppingListItemsAsync(IPage page)
    {
        await page.GotoAsync($"{GetBaseAddress(page)}/shopping-list");
        // LoadState.Load suffices: the pages are prerendered, while NetworkIdle would hang on
        // the long-lived /_blazor connection opened by interactive server pages.
        await page.WaitForLoadStateAsync(LoadState.Load);
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
