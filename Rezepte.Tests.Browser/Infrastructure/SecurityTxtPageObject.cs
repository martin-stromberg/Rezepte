using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login, navigation to the security.txt settings panel, and unauthenticated
/// access to the public <c>/security.txt</c> endpoint for the SecurityTxt browser tests.
/// </summary>
public sealed class SecurityTxtPageObject : IAsyncDisposable
{
    private const string SettingsHref = "/settings";
    private const string SecurityTxtMenuItemText = "security.txt";
    private const string EnabledCheckboxSelector = "#securitytxt-enabled";
    private const string ContactSelector = "#securitytxt-contact";
    private const string ExpiresSelector = "#securitytxt-expires";
    private const string SaveButtonSelector = "button.btn-primary.btn-sm";
    private const string SuccessAlertSelector = ".alert-success";

    private readonly IBrowserContext _context;
    private readonly string _baseAddress;

    private SecurityTxtPageObject(IBrowserContext context, IPage page, string baseAddress)
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
    /// <returns>A task that resolves to a new <see cref="SecurityTxtPageObject"/>.</returns>
    public static async Task<SecurityTxtPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        return new SecurityTxtPageObject(context, page, baseAddress);
    }

    /// <summary>
    /// Logs in with the supplied credentials and waits for the URL to leave the login page.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task that represents the asynchronous login operation.</returns>
    public async Task LoginAsync(string username, string password)
    {
        await Page.GotoAsync($"{_baseAddress}/login");
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
        await Page.GotoAsync($"{_baseAddress}{SettingsHref}");
        await Page.WaitForSelectorAsync(".list-group", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Determines whether the settings menu contains the security.txt entry.
    /// </summary>
    /// <returns>
    /// A task that resolves to <c>true</c> when the menu item is visible; otherwise <c>false</c>.
    /// </returns>
    public async Task<bool> IsSecurityTxtMenuItemVisibleAsync()
    {
        var buttons = await Page.QuerySelectorAllAsync(".list-group-item");
        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            if (text != null && text.Contains(SecurityTxtMenuItemText, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Clicks the security.txt menu item and waits for the enabled checkbox.
    /// </summary>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    public async Task ClickSecurityTxtMenuItemAsync()
    {
        await Page.ClickAsync($".list-group-item:has-text('{SecurityTxtMenuItemText}')");
        await Page.WaitForSelectorAsync(EnabledCheckboxSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Enables security.txt, fills contact and expiration, and saves the settings.
    /// </summary>
    /// <param name="contact">The contact value to set.</param>
    /// <param name="expires">The expiration timestamp to set.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task EnableAndSaveSecurityTxtAsync(string contact, DateTimeOffset expires)
    {
        var enabledChecked = await Page.IsCheckedAsync(EnabledCheckboxSelector);
        if (!enabledChecked)
        {
            await Page.ClickAsync(EnabledCheckboxSelector);
        }

        await Page.FillAsync(ContactSelector, contact);

        var expiresLocal = expires.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        await Page.FillAsync(ExpiresSelector, expiresLocal);

        await Page.ClickAsync(SaveButtonSelector);
        await Page.WaitForSelectorAsync(SuccessAlertSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Disables security.txt and saves the settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task DisableAndSaveSecurityTxtAsync()
    {
        var enabledChecked = await Page.IsCheckedAsync(EnabledCheckboxSelector);
        if (enabledChecked)
        {
            await Page.ClickAsync(EnabledCheckboxSelector);
        }

        await Page.ClickAsync(SaveButtonSelector);
        await Page.WaitForSelectorAsync(SuccessAlertSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
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
    /// Requests the public <c>/security.txt</c> endpoint without authentication.
    /// </summary>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <returns>
    /// A task that resolves to the HTTP status code and the raw response body.
    /// </returns>
    public static async Task<(int StatusCode, string Body)> GetPublicSecurityTxtAsync(string baseAddress)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
        using var response = await httpClient.GetAsync("/security.txt");
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, body);
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
            Path = Path.Combine("playwright-traces", $"SecurityTxt-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip")
        });
        await _context.CloseAsync();
    }
}
