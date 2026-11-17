using System.Net.Http.Json;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Provides the shared Playwright infrastructure for page objects: traced browser contexts,
/// login, settings navigation and user registration.
/// </summary>
public abstract class BrowserPageObjectBase : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly string _traceFilePrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserPageObjectBase"/> class.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="context">The browser context hosting <paramref name="page"/>.</param>
    /// <param name="page">The page used by this page object.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <param name="traceFilePrefix">The file name prefix for the saved trace file.</param>
    protected BrowserPageObjectBase(IBrowser browser, IBrowserContext context, IPage page, string baseAddress, string traceFilePrefix)
    {
        Browser = browser;
        _context = context;
        Page = page;
        BaseAddress = baseAddress;
        _traceFilePrefix = traceFilePrefix;
    }

    /// <summary>
    /// Gets the Playwright page used by this page object.
    /// </summary>
    public IPage Page { get; }

    /// <summary>
    /// Gets the Playwright browser instance.
    /// </summary>
    protected IBrowser Browser { get; }

    /// <summary>
    /// Gets the base URL of the application under test.
    /// </summary>
    protected string BaseAddress { get; }

    /// <summary>
    /// Creates a new browser context and page and starts tracing.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <returns>A task that resolves to the created context and page.</returns>
    protected static async Task<(IBrowserContext Context, IPage Page)> CreateTracedPageAsync(IBrowser browser)
    {
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        return (context, page);
    }

    /// <summary>
    /// Logs in with the supplied credentials and waits for the URL to leave the login page.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task that represents the asynchronous login operation.</returns>
    public async Task LoginAsync(string username, string password)
    {
        await Page.GotoAsync($"{BaseAddress}/login");
        await Page.FillAsync("#username", username);
        await Page.FillAsync("#password", password);
        await Page.ClickAsync("button.btn-accent[type=submit]");
        await Page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Navigates to the settings page and waits for the menu list to appear.
    /// </summary>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public async Task NavigateToSettingsAsync()
    {
        await Page.GotoAsync($"{BaseAddress}/settings");
        await Page.WaitForSelectorAsync(".list-group", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Determines whether the settings menu contains an entry with the given text.
    /// </summary>
    /// <param name="menuItemText">The text of the settings menu item.</param>
    /// <returns>
    /// A task that resolves to <c>true</c> when the menu item is visible; otherwise <c>false</c>.
    /// </returns>
    protected async Task<bool> IsSettingsMenuItemVisibleAsync(string menuItemText)
    {
        var buttons = await Page.QuerySelectorAllAsync(".list-group-item");
        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            if (text != null && text.Contains(menuItemText, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Registers a new user through the public authentication API.
    /// </summary>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <param name="username">The user name for the new account.</param>
    /// <param name="password">The password for the new account.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    public static async Task RegisterUserAsync(string baseAddress, string username, string password)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
        using var response = await httpClient.PostAsJsonAsync("api/auth/register", new
        {
            Username = username,
            Password = password,
            Email = (string?)null
        });
        response.EnsureSuccessStatusCode();
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
            Path = Path.Combine("playwright-traces", $"{_traceFilePrefix}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip")
        });
        await _context.CloseAsync();
    }
}
