using FluentAssertions;
using Microsoft.Playwright;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the public share link feature end to end: the owner creates a link
/// through the recipe page dialog, anonymous visitors see a read-only page without navigation,
/// and the admin can configure the link validity in the settings.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class ShareLinkBrowserTests
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public ShareLinkBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies the happy path: the owner creates a share link through the dialog and an anonymous
    /// visitor sees the recipe without the application navigation.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Owner_CanShareRecipe_AndAnonymousVisitorSeesReadOnlyRecipe()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);

        var recipeId = await pageObject.CreateRecipeAsync("Share Browser Cake");
        await pageObject.NavigateToRecipeAsync(recipeId);

        var shareUrl = await pageObject.CreateShareLinkViaUiAsync(includeSideDishes: false);
        shareUrl.Should().Contain("/share/");

        // The dialog must be styled as a fixed-position modal overlay (ShareRecipeDialog.razor.css).
        (await pageObject.IsShareDialogRenderedAsModalAsync()).Should().BeTrue();

        await using var anonymousPage = await pageObject.OpenAnonymousPageAsync(shareUrl);
        try
        {
            (await anonymousPage.Locator("h1").TextContentAsync()).Should().Contain("Share Browser Cake");
            (await ShareLinkPageObject.HasApplicationNavbarAsync(anonymousPage)).Should().BeFalse();
            (await anonymousPage.Locator("button:has(.bi-share)").CountAsync()).Should().Be(0);
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that a link shared with the side dish flag exposes the direct side dish page.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task ShareWithSideDishes_AnonymousVisitorCanOpenSideDishPage()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);

        var sideDishId = await pageObject.CreateRecipeAsync("Share Browser Salad");
        var recipeId = await pageObject.CreateRecipeAsync("Share Browser Steak", [sideDishId]);
        await pageObject.NavigateToRecipeAsync(recipeId);

        var shareUrl = await pageObject.CreateShareLinkViaUiAsync(includeSideDishes: true);

        await using var anonymousPage = await pageObject.OpenAnonymousPageAsync(shareUrl);
        try
        {
            var sideDishLink = anonymousPage.Locator("a[href*='/sidedish/']", new PageLocatorOptions { HasTextString = "Share Browser Salad" });
            await sideDishLink.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await sideDishLink.ClickAsync();
            await anonymousPage.WaitForURLAsync("**/sidedish/**", new PageWaitForURLOptions { Timeout = 10000 });
            await anonymousPage.WaitForSelectorAsync("h1:has-text('Share Browser Salad')", new PageWaitForSelectorOptions { Timeout = 10000 });

            (await anonymousPage.Locator("h1").TextContentAsync()).Should().Contain("Share Browser Salad");
            (await ShareLinkPageObject.HasApplicationNavbarAsync(anonymousPage)).Should().BeFalse();
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that a link shared without the side dish flag neither lists nor resolves the
    /// side dish page.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task ShareWithoutSideDishes_SideDishPageIsDenied()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);

        var sideDishId = await pageObject.CreateRecipeAsync("Hidden Salad");
        var recipeId = await pageObject.CreateRecipeAsync("Share Browser Main", [sideDishId]);
        var token = await pageObject.CreateShareLinkViaApiAsync(recipeId, includeSideDishes: false);

        await using var anonymousPage = await pageObject.OpenAnonymousPageAsync($"/share/{token}");
        try
        {
            (await anonymousPage.Locator("h1").TextContentAsync()).Should().Contain("Share Browser Main");
            (await anonymousPage.Locator("a[href*='/sidedish/']").CountAsync()).Should().Be(0);
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }

        // Direct navigation to the side dish route must be refused as well.
        await using var sideDishPage = await pageObject.OpenAnonymousPageAsync($"/share/{token}/sidedish/{sideDishId}");
        try
        {
            (await ShareLinkPageObject.ShowsInvalidLinkErrorAsync(sideDishPage)).Should().BeTrue();
        }
        finally
        {
            await sideDishPage.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that an unknown token shows the generic error state to anonymous visitors.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task UnknownToken_ShowsGenericError()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);

        await using var anonymousPage = await pageObject.OpenAnonymousPageAsync("/share/does-not-exist");
        try
        {
            (await ShareLinkPageObject.ShowsInvalidLinkErrorAsync(anonymousPage)).Should().BeTrue();
            (await ShareLinkPageObject.HasApplicationNavbarAsync(anonymousPage)).Should().BeFalse();
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that the shared image endpoint serves the recipe image to anonymous visitors.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task SharedImage_IsServedToAnonymousVisitors()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);

        var recipeId = await pageObject.CreateRecipeAsync("Imaged Cake");
        var imageId = await pageObject.UploadImageAsync(recipeId);
        var token = await pageObject.CreateShareLinkViaApiAsync(recipeId, includeSideDishes: false);

        var status = await pageObject.GetAnonymousStatusAsync($"/api/share/{token}/image/{imageId}");
        status.Should().Be(200);

        await using var anonymousPage = await pageObject.OpenAnonymousPageAsync($"/share/{token}");
        try
        {
            var image = anonymousPage.Locator("img[src*='api/share/']").First;
            await image.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            (await image.EvaluateAsync<int>("img => img.naturalWidth")).Should().BeGreaterThan(0);
        }
        finally
        {
            await anonymousPage.Context.CloseAsync();
        }
    }

    /// <summary>
    /// Verifies that the admin sees the public links settings entry and can persist the validity.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Admin_CanConfigureShareLinkValidityViaUi()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsPublicLinksMenuItemVisibleAsync()).Should().BeTrue();

        await pageObject.SetShareLinkValidityAsync(45);
    }

    /// <summary>
    /// Verifies that a regular user does not see the public links settings entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task RegularUser_DoesNotSeePublicLinksSettings()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        const string RegularUsername = "sharelink-nonadmin";
        const string RegularPassword = "NonAdmin!456";
        await ShareLinkPageObject.RegisterUserAsync(_appFixture.BaseAddress, RegularUsername, RegularPassword);

        await using var pageObject = await ShareLinkPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RegularUsername, RegularPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsPublicLinksMenuItemVisibleAsync()).Should().BeFalse();
    }
}
