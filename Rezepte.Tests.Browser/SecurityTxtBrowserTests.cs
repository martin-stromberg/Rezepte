using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the security.txt endpoint and its settings panel behavior.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class SecurityTxtBrowserTests
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly RezepteAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityTxtBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public SecurityTxtBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that the public security.txt endpoint returns HTTP 200 when the feature is enabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();
        await pageObject.ClickSecurityTxtMenuItemAsync();

        var expires = DateTimeOffset.UtcNow.AddYears(1);
        await pageObject.EnableAndSaveSecurityTxtAsync("mailto:security@example.com", expires);

        var (statusCode, _) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(_appFixture.BaseAddress);

        statusCode.Should().Be(200);
    }

    /// <summary>
    /// Verifies that the public security.txt endpoint returns HTTP 404 when the feature is disabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task GetSecurityTxt_ReturnsNotFound_WhenDisabled()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();
        await pageObject.ClickSecurityTxtMenuItemAsync();
        await pageObject.DisableAndSaveSecurityTxtAsync();

        var (statusCode, _) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(_appFixture.BaseAddress);

        statusCode.Should().Be(404);
    }

    /// <summary>
    /// Verifies that an admin can configure security.txt through the UI and that the configured
    /// contact appears in the public endpoint.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsSecurityTxtMenuItemVisibleAsync()).Should().BeTrue();

        await pageObject.ClickSecurityTxtMenuItemAsync();
        var expires = DateTimeOffset.UtcNow.AddYears(1);
        await pageObject.EnableAndSaveSecurityTxtAsync("mailto:admin@example.com", expires);

        var (statusCode, body) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(_appFixture.BaseAddress);

        statusCode.Should().Be(200);
        body.Should().Contain("Contact:");
        body.Should().Contain("mailto:admin@example.com");
    }

    /// <summary>
    /// Verifies that a regular user does not see the security.txt menu item in the settings panel.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        const string RegularUsername = "securitytxt-nonadmin";
        const string RegularPassword = "NonAdmin!456";
        await SecurityTxtPageObject.RegisterUserAsync(_appFixture.BaseAddress, RegularUsername, RegularPassword);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RegularUsername, RegularPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsSecurityTxtMenuItemVisibleAsync()).Should().BeFalse();
    }
}
