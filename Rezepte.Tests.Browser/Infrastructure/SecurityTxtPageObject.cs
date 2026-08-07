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

    public IPage Page { get; }

    public static async Task<SecurityTxtPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        return new SecurityTxtPageObject(context, page, baseAddress);
    }

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

    public async Task NavigateToSettingsAsync()
    {
        await Page.GotoAsync($"{_baseAddress}{SettingsHref}");
        await Page.WaitForSelectorAsync(".list-group", new PageWaitForSelectorOptions { Timeout = 10000 });
    }

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

    public async Task ClickSecurityTxtMenuItemAsync()
    {
        await Page.ClickAsync($".list-group-item:has-text('{SecurityTxtMenuItemText}')");
        await Page.WaitForSelectorAsync(EnabledCheckboxSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
    }

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

    public static async Task<(int StatusCode, string Body)> GetPublicSecurityTxtAsync(string baseAddress)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
        using var response = await httpClient.GetAsync("/security.txt");
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, body);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.CloseAsync();
    }
}
