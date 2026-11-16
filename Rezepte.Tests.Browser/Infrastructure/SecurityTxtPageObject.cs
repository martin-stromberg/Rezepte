using System.Globalization;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login, navigation to the security.txt settings panel, and unauthenticated
/// access to the public <c>/security.txt</c> endpoint for the SecurityTxt browser tests.
/// </summary>
public sealed class SecurityTxtPageObject : BrowserPageObjectBase
{
    private const string SecurityTxtMenuItemText = "security.txt";
    private const string EnabledCheckboxSelector = "#securitytxt-enabled";
    private const string ContactSelector = "#securitytxt-contact";
    private const string ExpiresSelector = "#securitytxt-expires";
    private const string SaveButtonSelector = "button.btn-primary.btn-sm";
    private const string SuccessAlertSelector = ".alert-success";

    private SecurityTxtPageObject(IBrowser browser, IBrowserContext context, IPage page, string baseAddress)
        : base(browser, context, page, baseAddress, "SecurityTxt")
    {
    }

    /// <summary>
    /// Creates a new browser context and page, starts tracing, and returns a configured page object.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <returns>A task that resolves to a new <see cref="SecurityTxtPageObject"/>.</returns>
    public static async Task<SecurityTxtPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var (context, page) = await CreateTracedPageAsync(browser);
        return new SecurityTxtPageObject(browser, context, page, baseAddress);
    }

    /// <summary>
    /// Determines whether the settings menu contains the security.txt entry.
    /// </summary>
    /// <returns>
    /// A task that resolves to <c>true</c> when the menu item is visible; otherwise <c>false</c>.
    /// </returns>
    public Task<bool> IsSecurityTxtMenuItemVisibleAsync()
        => IsSettingsMenuItemVisibleAsync(SecurityTxtMenuItemText);

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
}
