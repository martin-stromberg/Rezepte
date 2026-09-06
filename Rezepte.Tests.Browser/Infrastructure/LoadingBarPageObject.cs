using System.Globalization;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login and access to the loading bar host element's active state, color, and opacity
/// for the loading bar browser tests.
/// </summary>
public sealed class LoadingBarPageObject : IAsyncDisposable
{
    /// <summary>
    /// The relative URL of the cookbooks page.
    /// </summary>
    public const string CookbooksHref = "/cookbooks";

    /// <summary>
    /// The Playwright route glob that matches the cookbooks page.
    /// </summary>
    public const string CookbooksRouteGlob = "**/cookbooks";

    /// <summary>
    /// The relative URL of the shopping list page.
    /// </summary>
    public const string ShoppingListHref = "/shopping-list";

    /// <summary>
    /// The Playwright route glob that matches the shopping list page.
    /// </summary>
    public const string ShoppingListRouteGlob = "**/shopping-list";

    /// <summary>
    /// The Playwright route glob that matches the recipe search page.
    /// </summary>
    public const string SearchRouteGlob = "**/recipes/search*";

    private const string HostSelector = "#loading-bar";
    private const string ActiveClass = "loading-bar-active";
    private const string SearchInputSelector = "#nav-search";
    private const string SearchSubmitSelector = "button[aria-label='Suche starten']";
    private const string ShoppingListEditSelector = "button[aria-label='Bearbeiten']";
    private const string ShoppingListAddGroupSelector = "button[title='Gruppe hinzufügen'], button[title='Gruppe hinzufügen']";
    private const string ShoppingListAddRowSelector = "form.shopping-add-row";
    private const string ShoppingListAddRowInputSelector = "form.shopping-add-row input[aria-label='Zutat hinzufügen'], form.shopping-add-row input[aria-label='Zutat hinzufügen']";
    private const string ShoppingListAddRowSubmitSelector = "form.shopping-add-row button[type='submit']";

    private readonly IBrowserContext _context;
    private readonly string _baseAddress;

    private LoadingBarPageObject(IBrowserContext context, IPage page, string baseAddress)
    {
        _context = context;
        Page = page;
        _baseAddress = baseAddress;
    }

    /// <summary>
    /// Gets the Playwright page used by this page object.
    /// </summary>
    public IPage Page { get; }

    /// <summary>
    /// Creates a new browser context and page, starts tracing, and returns a configured page object.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <returns>A task that resolves to a new <see cref="LoadingBarPageObject"/>.</returns>
    public static async Task<LoadingBarPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        return new LoadingBarPageObject(context, page, baseAddress);
    }

    /// <summary>
    /// Logs in with the supplied credentials and waits for the URL to leave the login page.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task that represents the asynchronous login operation.</returns>
    public async Task LoginAsync(string username, string password)
    {
        await GotoAsync("/login");
        await Page.FillAsync("#username", username);
        await Page.FillAsync("#password", password);
        await Page.ClickAsync("button.btn-accent[type=submit]");
        await Page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Navigates to the supplied relative path.
    /// </summary>
    /// <param name="path">The relative path to open.</param>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public async Task GotoAsync(string path)
    {
        await Page.GotoAsync($"{_baseAddress}{path}");
    }

    /// <summary>
    /// Clicks the navigation link with the supplied relative <paramref name="href"/>
    /// without waiting for the post-click navigation to finish.
    /// </summary>
    /// <param name="href">The <c>href</c> value of the link to click.</param>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    public async Task ClickNavigationLinkAsync(string href)
    {
        // PageClickOptions.NoWaitAfter is marked obsolete by Playwright itself only because a
        // future Playwright version will make this behavior the default - the option is not
        // being removed and its current behavior (needed here so a client-side navigation link
        // click doesn't block on Playwright's own post-click navigation wait) is unchanged.
        // Genuinely bypassing it would require re-verifying loading-bar timing against a live
        // browser run, which is out of scope for this CI migration.
#pragma warning disable CS0612
        await Page.ClickAsync($"a[href='{href}']", new PageClickOptions { NoWaitAfter = true });
#pragma warning restore CS0612
    }

    /// <summary>
    /// Delays every request matching <paramref name="urlGlobPattern"/> by <paramref name="delayMilliseconds"/>.
    /// </summary>
    /// <param name="urlGlobPattern">The URL glob pattern to match.</param>
    /// <param name="delayMilliseconds">The delay to apply before continuing the route.</param>
    /// <returns>A task that represents the asynchronous route registration operation.</returns>
    public async Task DelayRouteAsync(string urlGlobPattern, int delayMilliseconds)
    {
        await Page.RouteAsync(urlGlobPattern, async route =>
        {
            await Task.Delay(delayMilliseconds);
            await route.ContinueAsync();
        });
    }

    /// <summary>
    /// Aborts every request matching <paramref name="urlGlobPattern"/> by completing it without a response.
    /// </summary>
    /// <param name="urlGlobPattern">The URL glob pattern to match.</param>
    /// <returns>A task that represents the asynchronous route registration operation.</returns>
    public async Task BlockRouteAsync(string urlGlobPattern)
    {
        await Page.RouteAsync(urlGlobPattern, _ => Task.CompletedTask);
    }

    /// <summary>
    /// Fills the navigation search input and submits the search form.
    /// </summary>
    /// <param name="term">The search term to submit.</param>
    /// <returns>A task that represents the asynchronous submit operation.</returns>
    public async Task SubmitNavigationSearchAsync(string term)
    {
        await Page.FillAsync(SearchInputSelector, term);
        // See ClickNavigationLinkAsync above for why NoWaitAfter is still used here.
#pragma warning disable CS0612
        await Page.ClickAsync(SearchSubmitSelector, new PageClickOptions { NoWaitAfter = true });
#pragma warning restore CS0612
    }

    /// <summary>
    /// Adds a new item to the interactive shopping list and submits the add-row form.
    /// </summary>
    /// <param name="itemName">The name of the item to add.</param>
    /// <returns>A task that represents the asynchronous form submission operation.</returns>
    public async Task SubmitInteractiveShoppingListItemAsync(string itemName)
    {
        await GotoAsync(ShoppingListHref);
        var editButton = Page.Locator(ShoppingListEditSelector);
        var addGroupButton = Page.Locator(ShoppingListAddGroupSelector).First;

        await editButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 10000,
        });

        for (var attempt = 0; attempt < 5 && !await addGroupButton.IsVisibleAsync(); attempt++)
        {
            await editButton.ClickAsync();
            try
            {
                await addGroupButton.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 2000,
                });
            }
            catch (TimeoutException) when (attempt < 4)
            {
            }
        }

        await addGroupButton.ClickAsync();
        await Page.WaitForSelectorAsync(ShoppingListAddRowSelector);

        await Page.FillAsync(ShoppingListAddRowInputSelector, itemName);
        await Page.ClickAsync(ShoppingListAddRowSubmitSelector);
    }

    /// <summary>
    /// Determines whether the loading bar host element currently has the active CSS class.
    /// </summary>
    /// <returns>
    /// A task that resolves to <c>true</c> when the loading bar is active; otherwise <c>false</c>.
    /// </returns>
    public async Task<bool> IsLoadingBarActiveAsync()
    {
        var classAttribute = await Page.GetAttributeAsync(HostSelector, "class");
        return classAttribute is not null && classAttribute.Contains(ActiveClass, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads the current value of the <c>--loading-bar-color</c> CSS custom property.
    /// </summary>
    /// <returns>A task that resolves to the color string, or <c>null</c> when not set.</returns>
    public async Task<string?> GetLoadingBarColorAsync()
    {
        var color = await Page.EvalOnSelectorAsync<string>(
            HostSelector,
            "el => getComputedStyle(el).getPropertyValue('--loading-bar-color').trim()");
        return string.IsNullOrEmpty(color) ? null : color;
    }

    /// <summary>
    /// Reads the current opacity of the loading bar host element.
    /// </summary>
    /// <returns>A task that resolves to the opacity as a number between 0 and 1.</returns>
    public async Task<double> GetLoadingBarOpacityAsync()
    {
        var opacity = await Page.EvalOnSelectorAsync<string>(HostSelector, "el => getComputedStyle(el).opacity");
        return double.Parse(opacity, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Waits until the loading bar host element has the active CSS class.
    /// </summary>
    /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitUntilLoadingBarActiveAsync(int timeoutMilliseconds = 1000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !!el && el.classList.contains('{ActiveClass}'); }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    /// <summary>
    /// Waits until the loading bar host element becomes visible (opacity greater than 0).
    /// </summary>
    /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitUntilLoadingBarVisibleAsync(int timeoutMilliseconds = 2000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !!el && parseFloat(getComputedStyle(el).opacity) > 0; }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    /// <summary>
    /// Waits until the loading bar color changes away from <paramref name="previousColor"/>.
    /// </summary>
    /// <param name="previousColor">The color value that must change.</param>
    /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitUntilLoadingBarColorChangedAsync(string previousColor, int timeoutMilliseconds = 2000)
    {
        await Page.WaitForFunctionAsync(
            $"expected => {{ const el = document.querySelector('{HostSelector}'); return !!el && getComputedStyle(el).getPropertyValue('--loading-bar-color').trim() !== expected; }}",
            arg: previousColor,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    /// <summary>
    /// Waits until the loading bar is hidden or no longer active.
    /// </summary>
    /// <param name="timeoutMilliseconds">The maximum time to wait in milliseconds.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitUntilLoadingBarHiddenAsync(int timeoutMilliseconds = 5000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !el || !el.classList.contains('{ActiveClass}'); }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    /// <summary>
    /// Stops Playwright tracing, saves the trace file, and closes the browser context.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        Directory.CreateDirectory("playwright-traces");
        await _context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = Path.Combine("playwright-traces", $"LoadingBar-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip")
        });
        await _context.CloseAsync();
    }
}
