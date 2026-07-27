using System.Globalization;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login and access to the loading bar host element's active state, color, and opacity
/// for the loading bar browser tests.
/// </summary>
public sealed class LoadingBarPageObject : IAsyncDisposable
{
    private const string HostSelector = "#loading-bar";
    private const string ActiveClass = "loading-bar-active";

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
