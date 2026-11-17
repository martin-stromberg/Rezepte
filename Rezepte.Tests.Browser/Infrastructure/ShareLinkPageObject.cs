using System.Text.Json;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Encapsulates login, the recipe share dialog on the recipe page, the public
/// <c>/share/{token}</c> pages and the public links admin settings for the share link browser tests.
/// </summary>
public sealed class ShareLinkPageObject : BrowserPageObjectBase
{
    // A minimal valid PNG (1x1 transparent pixel) used for image uploads.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private const string ShareButtonSelector = "button:has(.bi-share)";
    private const string OverlayBackdropSelector = ".overlay-backdrop";
    private const string IncludeSideDishesSelector = "#share-include-sidedishes";
    private const string ShareLinkUrlSelector = "#share-link-url";
    private const string PublicLinksMenuItemText = "Öffentliche Links";
    private const string ValidityDaysSelector = "#sharelink-validity-days";
    private const string SuccessAlertSelector = ".alert-success";
    private const string WarningAlertSelector = ".alert-warning";

    private ShareLinkPageObject(IBrowser browser, IBrowserContext context, IPage page, string baseAddress)
        : base(browser, context, page, baseAddress, "ShareLink")
    {
    }

    /// <summary>
    /// Creates a new browser context and page, starts tracing, and returns a configured page object.
    /// </summary>
    /// <param name="browser">The Playwright browser instance.</param>
    /// <param name="baseAddress">The base URL of the application under test.</param>
    /// <returns>A task that resolves to a new <see cref="ShareLinkPageObject"/>.</returns>
    public static async Task<ShareLinkPageObject> CreateAsync(IBrowser browser, string baseAddress)
    {
        var (context, page) = await CreateTracedPageAsync(browser);
        return new ShareLinkPageObject(browser, context, page, baseAddress);
    }

    /// <summary>
    /// Fetches a JWT session token for the user that is logged in on <see cref="BrowserPageObjectBase.Page"/>.
    /// </summary>
    /// <returns>A task that resolves to the bearer token.</returns>
    public async Task<string> GetSessionTokenAsync()
    {
        var tokenResponse = await Page.Context.APIRequest.GetAsync($"{BaseAddress}/api/session/token");
        if (!tokenResponse.Ok)
        {
            throw new InvalidOperationException(
                $"Fetching the session token failed with status {tokenResponse.Status}: {await tokenResponse.TextAsync()}");
        }

        using var tokenDocument = JsonDocument.Parse(await tokenResponse.TextAsync());
        return tokenDocument.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The session token response did not contain a token.");
    }

    /// <summary>
    /// Creates a recipe for the logged-in user through the API and returns its id.
    /// </summary>
    /// <param name="title">The recipe title.</param>
    /// <param name="sideDishRecipeIds">Optional side dish recipe ids to link.</param>
    /// <param name="sourceUri">Optional external source uri.</param>
    /// <returns>A task that resolves to the created recipe id.</returns>
    public async Task<string> CreateRecipeAsync(string title, List<string>? sideDishRecipeIds = null, string? sourceUri = null)
    {
        var token = await GetSessionTokenAsync();
        var response = await Page.Context.APIRequest.PostAsync($"{BaseAddress}/api/recipes", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = new
            {
                cookbookId = (string?)null,
                title,
                description = $"Browser test recipe '{title}'.",
                uri = sourceUri,
                portions = (int?)4,
                steps = Array.Empty<object>(),
                sideDishRecipeIds
            }
        });

        if (!response.Ok)
        {
            throw new InvalidOperationException(
                $"Creating the recipe '{title}' failed with status {response.Status}: {await response.TextAsync()}");
        }

        using var document = JsonDocument.Parse(await response.TextAsync());
        return document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("The recipe creation response did not contain an id.");
    }

    /// <summary>
    /// Uploads a small PNG image to the recipe through the API and returns the image id.
    /// </summary>
    /// <param name="recipeId">The recipe id.</param>
    /// <returns>A task that resolves to the created image id.</returns>
    public async Task<string> UploadImageAsync(string recipeId)
    {
        var token = await GetSessionTokenAsync();
        var multipart = Page.Context.APIRequest.CreateFormData();
        multipart.Set("file", new FilePayload { Name = "test.png", MimeType = "image/png", Buffer = TinyPng });

        var response = await Page.Context.APIRequest.PostAsync($"{BaseAddress}/api/recipes/{recipeId}/image", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            Multipart = multipart
        });

        if (!response.Ok)
        {
            throw new InvalidOperationException(
                $"Uploading the image failed with status {response.Status}: {await response.TextAsync()}");
        }

        using var document = JsonDocument.Parse(await response.TextAsync());
        return document.RootElement.GetProperty("imageId").GetString()
            ?? throw new InvalidOperationException("The image upload response did not contain an imageId.");
    }

    /// <summary>
    /// Creates a share link for the recipe directly through the API and returns its token.
    /// </summary>
    /// <param name="recipeId">The recipe id.</param>
    /// <param name="includeSideDishes">Whether side dishes should be accessible through the link.</param>
    /// <returns>A task that resolves to the share token.</returns>
    public async Task<string> CreateShareLinkViaApiAsync(string recipeId, bool includeSideDishes)
    {
        var token = await GetSessionTokenAsync();
        var response = await Page.Context.APIRequest.PostAsync($"{BaseAddress}/api/recipes/{recipeId}/share", new APIRequestContextOptions
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = $"Bearer {token}" },
            DataObject = new { includeSideDishes }
        });

        if (!response.Ok)
        {
            throw new InvalidOperationException(
                $"Creating the share link failed with status {response.Status}: {await response.TextAsync()}");
        }

        using var document = JsonDocument.Parse(await response.TextAsync());
        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The share link response did not contain a token.");
    }

    /// <summary>
    /// Opens the recipe page for the given id and waits for the share button of the
    /// interactive circuit to appear.
    /// </summary>
    /// <param name="recipeId">The recipe id.</param>
    /// <returns>A task that represents the asynchronous navigation operation.</returns>
    public async Task NavigateToRecipeAsync(string recipeId)
    {
        await Page.GotoAsync($"{BaseAddress}/recipes/{recipeId}");
        await Page.WaitForSelectorAsync(ShareButtonSelector, new PageWaitForSelectorOptions { Timeout = 20000 });
    }

    /// <summary>
    /// Opens the share dialog, optionally enables the side dish checkbox and creates the link.
    /// </summary>
    /// <param name="includeSideDishes">Whether to enable the side dish checkbox before creating.</param>
    /// <returns>A task that resolves to the absolute share URL shown in the dialog.</returns>
    public async Task<string> CreateShareLinkViaUiAsync(bool includeSideDishes)
    {
        await Page.ClickAsync(ShareButtonSelector);
        await Page.WaitForSelectorAsync(OverlayBackdropSelector, new PageWaitForSelectorOptions { Timeout = 10000 });

        if (includeSideDishes)
        {
            await Page.WaitForSelectorAsync(IncludeSideDishesSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
            await Page.CheckAsync(IncludeSideDishesSelector);
        }

        await Page.ClickAsync(".overlay-content button.btn-primary");
        await Page.WaitForSelectorAsync(ShareLinkUrlSelector, new PageWaitForSelectorOptions { Timeout = 10000 });

        return await Page.Locator(ShareLinkUrlSelector).InputValueAsync();
    }

    /// <summary>
    /// Determines whether the open share dialog is rendered as a fixed-position modal overlay
    /// covering the viewport.
    /// </summary>
    /// <returns>
    /// A task that resolves to <c>true</c> when the backdrop exists and is styled
    /// <c>position: fixed</c>; otherwise <c>false</c>.
    /// </returns>
    public async Task<bool> IsShareDialogRenderedAsModalAsync()
    {
        var backdrop = Page.Locator(OverlayBackdropSelector);
        if (await backdrop.CountAsync() == 0 || !await backdrop.IsVisibleAsync())
        {
            return false;
        }

        var position = await backdrop.EvaluateAsync<string>("el => getComputedStyle(el).position");
        return position == "fixed";
    }

    /// <summary>
    /// Opens a fresh anonymous browser context (no cookies) and navigates to the given path or
    /// absolute URL.
    /// </summary>
    /// <param name="pathOrUrl">The path relative to the application or an absolute share URL.</param>
    /// <returns>A task that resolves to the anonymous page.</returns>
    public async Task<IPage> OpenAnonymousPageAsync(string pathOrUrl)
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var url = pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? pathOrUrl
            : $"{BaseAddress}{pathOrUrl}";
        await page.GotoAsync(url);
        return page;
    }

    /// <summary>
    /// Fetches a path through an anonymous request context and returns the status code.
    /// </summary>
    /// <param name="pathOrUrl">The path relative to the application or an absolute URL.</param>
    /// <returns>A task that resolves to the HTTP status code.</returns>
    public async Task<int> GetAnonymousStatusAsync(string pathOrUrl)
    {
        var context = await Browser.NewContextAsync();
        try
        {
            var url = pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? pathOrUrl
                : $"{BaseAddress}{pathOrUrl}";
            var response = await context.APIRequest.GetAsync(url);
            return response.Status;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Determines whether the settings menu contains the public links entry.
    /// </summary>
    /// <returns>
    /// A task that resolves to <c>true</c> when the menu item is visible; otherwise <c>false</c>.
    /// </returns>
    public Task<bool> IsPublicLinksMenuItemVisibleAsync()
        => IsSettingsMenuItemVisibleAsync(PublicLinksMenuItemText);

    /// <summary>
    /// Opens the public links settings panel, sets the validity days and saves.
    /// </summary>
    /// <param name="days">The validity in days.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SetShareLinkValidityAsync(int days)
    {
        await Page.ClickAsync($".list-group-item:has-text('{PublicLinksMenuItemText}')");
        await Page.WaitForSelectorAsync(ValidityDaysSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.FillAsync(ValidityDaysSelector, days.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Page.ClickAsync("button.btn-primary");
        await Page.WaitForSelectorAsync(SuccessAlertSelector, new PageWaitForSelectorOptions { Timeout = 10000 });
    }

    /// <summary>
    /// Determines whether the anonymous page shows the invalid-or-expired alert and no navigation bar.
    /// </summary>
    /// <param name="page">The anonymous page.</param>
    /// <returns>A task that resolves to <c>true</c> when the error alert is visible.</returns>
    public static async Task<bool> ShowsInvalidLinkErrorAsync(IPage page)
    {
        var alert = page.Locator(WarningAlertSelector);
        await alert.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        return await alert.IsVisibleAsync();
    }

    /// <summary>
    /// Determines whether the page renders the application navigation bar.
    /// </summary>
    /// <param name="page">The page to inspect.</param>
    /// <returns>A task that resolves to <c>true</c> when the navbar exists.</returns>
    public static async Task<bool> HasApplicationNavbarAsync(IPage page)
    {
        return await page.Locator("nav.app-navbar").CountAsync() > 0;
    }
}
