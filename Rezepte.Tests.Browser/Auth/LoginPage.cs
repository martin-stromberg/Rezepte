using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Auth;

/// <summary>
/// Page object for the login page.
/// </summary>
public sealed class LoginPage
{
    private const string UsernameSelector = "#username";
    private const string PasswordSelector = "#password";
    private const string SubmitSelector = "button.btn-accent[type='submit']";

    private readonly string _baseAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page used by this page object.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    public LoginPage(IPage page, string baseAddress)
    {
        Page = page;
        _baseAddress = baseAddress;
    }

    /// <summary>
    /// Gets the Playwright page used by this page object.
    /// </summary>
    public IPage Page { get; }

    /// <summary>
    /// Navigates to the login page, fills the credentials and submits the form.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task that represents the asynchronous login operation.</returns>
    public async Task LoginAsync(string username, string password)
    {
        await Page.GotoAsync($"{_baseAddress}/login");
        await Page.FillAsync(UsernameSelector, username);
        await Page.FillAsync(PasswordSelector, password);
        await Page.ClickAsync(SubmitSelector);
        await Page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });
    }
}
