using System.Globalization;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login and access to the loading bar host element's active state, color, and opacity
/// for the loading bar browser tests.
/// </summary>
public sealed class LoadingBarPageObject : IAsyncDisposable
{
    public const string CookbooksHref = "/cookbooks";
    public const string CookbooksRouteGlob = "**/cookbooks";
    public const string ShoppingListHref = "/shopping-list";
    public const string ShoppingListRouteGlob = "**/shopping-list";
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

    public IPage Page { get; }

    public static async Task<LoadingBarPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        return new LoadingBarPageObject(context, page, baseAddress);
    }

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

    public async Task GotoAsync(string path)
    {
        await Page.GotoAsync($"{_baseAddress}{path}");
    }

    public async Task ClickNavigationLinkAsync(string href)
    {
        await Page.ClickAsync($"a[href='{href}']", new PageClickOptions { NoWaitAfter = true });
    }

    public async Task DelayRouteAsync(string urlGlobPattern, int delayMilliseconds)
    {
        await Page.RouteAsync(urlGlobPattern, async route =>
        {
            await Task.Delay(delayMilliseconds);
            await route.ContinueAsync();
        });
    }

    public async Task BlockRouteAsync(string urlGlobPattern)
    {
        await Page.RouteAsync(urlGlobPattern, _ => Task.CompletedTask);
    }

    public async Task SubmitNavigationSearchAsync(string term)
    {
        await Page.FillAsync(SearchInputSelector, term);
        await Page.ClickAsync(SearchSubmitSelector, new PageClickOptions { NoWaitAfter = true });
    }

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

    public async Task<bool> IsLoadingBarActiveAsync()
    {
        var classAttribute = await Page.GetAttributeAsync(HostSelector, "class");
        return classAttribute is not null && classAttribute.Contains(ActiveClass, StringComparison.Ordinal);
    }

    public async Task<string?> GetLoadingBarColorAsync()
    {
        var color = await Page.EvalOnSelectorAsync<string>(
            HostSelector,
            "el => getComputedStyle(el).getPropertyValue('--loading-bar-color').trim()");
        return string.IsNullOrEmpty(color) ? null : color;
    }

    public async Task<double> GetLoadingBarOpacityAsync()
    {
        var opacity = await Page.EvalOnSelectorAsync<string>(HostSelector, "el => getComputedStyle(el).opacity");
        return double.Parse(opacity, CultureInfo.InvariantCulture);
    }

    public async Task WaitUntilLoadingBarActiveAsync(int timeoutMilliseconds = 1000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !!el && el.classList.contains('{ActiveClass}'); }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    public async Task WaitUntilLoadingBarVisibleAsync(int timeoutMilliseconds = 2000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !!el && parseFloat(getComputedStyle(el).opacity) > 0; }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    public async Task WaitUntilLoadingBarColorChangedAsync(string previousColor, int timeoutMilliseconds = 2000)
    {
        await Page.WaitForFunctionAsync(
            $"expected => {{ const el = document.querySelector('{HostSelector}'); return !!el && getComputedStyle(el).getPropertyValue('--loading-bar-color').trim() !== expected; }}",
            arg: previousColor,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    public async Task WaitUntilLoadingBarHiddenAsync(int timeoutMilliseconds = 5000)
    {
        await Page.WaitForFunctionAsync(
            $"() => {{ const el = document.querySelector('{HostSelector}'); return !el || !el.classList.contains('{ActiveClass}'); }}",
            arg: null,
            new PageWaitForFunctionOptions { Timeout = timeoutMilliseconds });
    }

    public async ValueTask DisposeAsync()
    {
        await _context.CloseAsync();
    }
}
