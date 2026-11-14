using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login, navigation to the Plugins settings panel, plugin source management, and
/// assertions on the per-source update check UI for the plugin settings browser tests.
/// </summary>
public sealed class PluginSettingsPageObject : IAsyncDisposable
{
    private const string SettingsHref = "/settings";
    private const string PluginsMenuItemText = "Plugins";
    private const string SourceUrlSelector = "#plugin-source-url";
    private const string SourcePrivateSelector = "#plugin-source-private";
    private const string SourceTrustSelector = "#plugin-source-trust";
    private const string SourceSubmitSelector = "form:has(#plugin-source-url) button[type=submit]";
    private const string CheckButtonIconSelector = "i.bi-arrow-repeat";
    private const string RemoveButtonIconSelector = "i.bi-trash";
    private const string SuccessAlertSelector = ".alert-success";
    private const string DangerAlertSelector = ".alert-danger";
    private const string PluginTableSelector = "h3:has-text('Lokale Import-Plugins') + .table-responsive";

    private readonly IBrowserContext _context;
    private readonly string _baseAddress;

    private PluginSettingsPageObject(IBrowserContext context, IPage page, string baseAddress)
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
    /// <returns>A task that resolves to a new <see cref="PluginSettingsPageObject"/>.</returns>
    public static async Task<PluginSettingsPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        return new PluginSettingsPageObject(context, page, baseAddress);
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
    /// Navigates to the settings page, opens the Plugins panel, and waits for the source form.
    /// The menu click is retried because a click that lands before the Blazor circuit has
    /// connected is silently dropped.
    /// </summary>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public async Task NavigateToPluginSettingsAsync()
    {
        await Page.GotoAsync($"{_baseAddress}{SettingsHref}");
        var menuItem = Page.Locator($".list-group-item:has-text('{PluginsMenuItemText}')");
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await menuItem.ClickAsync();
            try
            {
                await Page.WaitForSelectorAsync(SourceUrlSelector, new PageWaitForSelectorOptions { Timeout = 5000 });
                return;
            }
            catch (TimeoutException) when (DateTime.UtcNow < deadline)
            {
                // The click happened before the interactive circuit was ready; retry.
            }
        }
    }

    /// <summary>
    /// Adds a plugin source through the source form and waits until its table row appears.
    /// </summary>
    /// <param name="repositoryUrl">The GitHub repository URL of the source.</param>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <param name="isPrivate">Whether the source is private (no PAT is stored).</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task AddSourceAsync(string repositoryUrl, string displayName, bool isPrivate)
    {
        await Page.FillAsync(SourceUrlSelector, repositoryUrl);
        if (isPrivate)
        {
            await Page.CheckAsync(SourcePrivateSelector);
        }

        await Page.CheckAsync(SourceTrustSelector);
        await Page.ClickAsync(SourceSubmitSelector);
        await SourceRow(displayName).WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Clicks the check button of the source row and waits for a result alert to appear.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    public async Task ClickCheckButtonAsync(string displayName)
    {
        await CheckButton(displayName).ClickAsync();
        await Page.WaitForSelectorAsync(
            $"{SuccessAlertSelector}, {DangerAlertSelector}",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Toggles the enabled switch of the source row and waits for the list to reload.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <param name="enabled">The desired enabled state.</param>
    /// <returns>A task that represents the asynchronous toggle operation.</returns>
    public async Task SetSourceEnabledAsync(string displayName, bool enabled)
    {
        var toggle = SourceRow(displayName).Locator("input.form-check-input[type=checkbox]");
        if (await toggle.IsCheckedAsync() != enabled)
        {
            await toggle.ClickAsync();
            // Wait for the reload after the toggle: the check button's title switches to the
            // disabled reason when the source is off and back to the check label when on.
            // The wait is scoped to this row so a button of another source cannot satisfy it.
            var expectedTitle = enabled ? "Auf Updates prüfen" : "Quelle ist deaktiviert.";
            await SourceRow(displayName)
                .Locator($"button[title='{expectedTitle}']")
                .WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
        }
    }

    /// <summary>
    /// Determines whether the check button of the source row is disabled.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <returns>A task that resolves to <c>true</c> when the check button is disabled.</returns>
    public async Task<bool> IsCheckButtonDisabledAsync(string displayName)
    {
        return await CheckButton(displayName).IsDisabledAsync();
    }

    /// <summary>
    /// Gets the title attribute of the check button's tooltip wrapper for the source row.
    /// The wrapper carries the tooltip because <c>title</c> on a disabled button is never shown.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <returns>A task that resolves to the tooltip text, or <c>null</c> when absent.</returns>
    public async Task<string?> GetCheckButtonTooltipAsync(string displayName)
    {
        var wrapper = CheckButton(displayName).Locator("xpath=..");
        return await wrapper.GetAttributeAsync("title");
    }

    /// <summary>
    /// Gets the title attribute of the check button itself for the source row.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <returns>A task that resolves to the title text, or <c>null</c> when absent.</returns>
    public async Task<string?> GetCheckButtonTitleAsync(string displayName)
    {
        return await CheckButton(displayName).GetAttributeAsync("title");
    }

    /// <summary>
    /// Gets the text content of the first result alert, or <c>null</c> when none is rendered.
    /// </summary>
    /// <returns>A task that resolves to the alert text.</returns>
    public async Task<string?> GetAlertTextAsync()
    {
        var alert = Page.Locator($"{SuccessAlertSelector}, {DangerAlertSelector}").First;
        return await alert.CountAsync() > 0 ? await alert.TextContentAsync() : null;
    }

    /// <summary>
    /// Gets the text content of the status cell of the source row.
    /// </summary>
    /// <param name="displayName">The <c>owner/repository</c> text rendered in the source row.</param>
    /// <returns>A task that resolves to the status cell text.</returns>
    public async Task<string> GetSourceStatusTextAsync(string displayName)
    {
        var statusCell = SourceRow(displayName).Locator("td").Nth(2);
        return await statusCell.TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Waits until the plugin row with the given display name is rendered in the plugin table.
    /// </summary>
    /// <param name="displayName">The display name rendered in the plugin row.</param>
    /// <returns>A task that represents the asynchronous wait operation.</returns>
    public async Task WaitForPluginRowAsync(string displayName)
    {
        await PluginRow(displayName).WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Clicks the remove button of the plugin row and waits for a result alert to appear.
    /// </summary>
    /// <param name="displayName">The display name rendered in the plugin row.</param>
    /// <returns>A task that represents the asynchronous click operation.</returns>
    public async Task RemovePluginRowAsync(string displayName)
    {
        await PluginRow(displayName).Locator($"button:has({RemoveButtonIconSelector})").ClickAsync();
        await Page.WaitForSelectorAsync(
            $"{SuccessAlertSelector}, {DangerAlertSelector}",
            new PageWaitForSelectorOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Determines whether a plugin row with the given display name is rendered in the plugin
    /// table.
    /// </summary>
    /// <param name="displayName">The display name rendered in the plugin row.</param>
    /// <returns>A task that resolves to <c>true</c> when the plugin row is visible.</returns>
    public async Task<bool> IsPluginRowVisibleAsync(string displayName)
    {
        return await PluginRow(displayName).CountAsync() > 0;
    }

    /// <summary>
    /// Gets the title attribute of the remove button of the plugin row.
    /// </summary>
    /// <param name="displayName">The display name rendered in the plugin row.</param>
    /// <returns>A task that resolves to the title text, or <c>null</c> when absent.</returns>
    public async Task<string?> GetPluginRemoveButtonTitleAsync(string displayName)
    {
        return await PluginRow(displayName)
            .Locator($"button:has({RemoveButtonIconSelector})")
            .GetAttributeAsync("title");
    }

    /// <summary>
    /// Determines whether the localized explanation text is rendered in the sources section.
    /// </summary>
    /// <returns>A task that resolves to <c>true</c> when the info text is visible.</returns>
    public async Task<bool> IsSourcesInfoTextVisibleAsync()
    {
        return await Page.Locator("text=Jede Quelle kann zusätzlich manuell geprüft werden").CountAsync() > 0;
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
            Path = Path.Combine("playwright-traces", $"PluginSettings-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip")
        });
        await _context.CloseAsync();
    }

    private ILocator SourceRow(string displayName)
    {
        return Page.Locator($"tr:has-text('{displayName}')").First;
    }

    private ILocator PluginRow(string displayName)
    {
        return Page.Locator(PluginTableSelector).Locator($"tr:has-text('{displayName}')").First;
    }

    private ILocator CheckButton(string displayName)
    {
        return SourceRow(displayName).Locator($"button:has({CheckButtonIconSelector})").First;
    }
}
