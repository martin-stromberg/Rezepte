using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Auth;

/// <summary>
/// Page object for the registration page.
/// </summary>
public sealed class RegisterPage : IAsyncDisposable
{
    private const string UsernameSelector = "#register-username";
    private const string EmailSelector = "#register-email";
    private const string PasswordSelector = "#register-password";
    private const string CreateDemoDataSelector = "#register-create-demo-data";
    // Scoped to .page-auth: an unscoped "button[type='submit']" would match the navigation
    // search form's submit button first and submit the wrong form.
    private const string SubmitSelector = ".page-auth button[type='submit']";
    private const string ErrorSelector = ".alert-danger";

    private readonly IBrowserContext _context;
    private readonly string _baseAddress;

    private RegisterPage(IBrowserContext context, IPage page, string baseAddress)
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
    /// Creates a new browser context, starts tracing, and returns a configured register page.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <returns>A task that resolves to a new <see cref="RegisterPage"/>.</returns>
    public static async Task<RegisterPage> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        return new RegisterPage(context, page, baseAddress);
    }

    /// <summary>
    /// Navigates to the registration page and waits until the registration form is rendered.
    /// </summary>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public async Task GotoAsync()
    {
        // WaitUntilState.Load is used instead of NetworkIdle because the Blazor server circuit
        // can keep a long-lived /_blazor connection open, which would make NetworkIdle unreliable.
        await Page.GotoAsync($"{_baseAddress}/register", new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
        await Page.WaitForSelectorAsync(UsernameSelector, new PageWaitForSelectorOptions { Timeout = 30000 });
    }

    /// <summary>
    /// Fills the username input.
    /// </summary>
    /// <param name="username">The username to enter.</param>
    /// <returns>A task that represents the asynchronous fill operation.</returns>
    public async Task FillUsernameAsync(string username)
    {
        await Page.FillAsync(UsernameSelector, username);
    }

    /// <summary>
    /// Fills the email input.
    /// </summary>
    /// <param name="email">The email to enter.</param>
    /// <returns>A task that represents the asynchronous fill operation.</returns>
    public async Task FillEmailAsync(string email)
    {
        await Page.FillAsync(EmailSelector, email);
    }

    /// <summary>
    /// Fills the password input.
    /// </summary>
    /// <param name="password">The password to enter.</param>
    /// <returns>A task that represents the asynchronous fill operation.</returns>
    public async Task FillPasswordAsync(string password)
    {
        await Page.FillAsync(PasswordSelector, password);
    }

    /// <summary>
    /// Checks or unchecks the demo data checkbox.
    /// </summary>
    /// <param name="createDemoData">True to check the checkbox.</param>
    /// <returns>A task that represents the asynchronous check operation.</returns>
    public async Task CheckCreateDemoDataAsync(bool createDemoData)
    {
        var isChecked = await Page.IsCheckedAsync(CreateDemoDataSelector);
        if (isChecked != createDemoData)
        {
            await Page.ClickAsync(CreateDemoDataSelector);
        }
    }

    /// <summary>
    /// Submits the registration form.
    /// </summary>
    /// <returns>A task that represents the asynchronous submit operation.</returns>
    public async Task SubmitAsync()
    {
        await Page.ClickAsync(SubmitSelector);
    }

    /// <summary>
    /// Gets the first error message from the page, if any.
    /// </summary>
    /// <returns>A task that resolves to the error message or null.</returns>
    public async Task<string?> GetErrorAsync()
    {
        var locator = Page.Locator(ErrorSelector);
        if (await locator.CountAsync() == 0)
        {
            return null;
        }

        return await locator.TextContentAsync();
    }

    /// <summary>
    /// Logs in with the supplied credentials.
    /// </summary>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task that represents the asynchronous login operation.</returns>
    public async Task LoginAsync(string username, string password)
    {
        await Page.GotoAsync($"{_baseAddress}/login");
        await Page.FillAsync("#username", username);
        await Page.FillAsync("#password", password);
        await Page.ClickAsync("button.btn-accent[type='submit']");
        await Page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 10000 });
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
            Path = Path.Combine("playwright-traces", $"Register-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip")
        });
        await _context.CloseAsync();
    }
}
