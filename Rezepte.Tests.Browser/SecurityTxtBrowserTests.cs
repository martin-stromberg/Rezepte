using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class SecurityTxtBrowserTests(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)
{
    [SkippableFact]
    public async Task GetSecurityTxt_ReturnsOk_WithoutAuthentication_WhenEnabled()
    {
        Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(appFixture.ApplicationAvailable, appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(browserFixture.Browser!, appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();
        await pageObject.ClickSecurityTxtMenuItemAsync();

        var expires = DateTimeOffset.UtcNow.AddYears(1);
        await pageObject.EnableAndSaveSecurityTxtAsync("mailto:security@example.com", expires);

        var (statusCode, _) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(appFixture.BaseAddress);

        statusCode.Should().Be(200);
    }

    [SkippableFact]
    public async Task GetSecurityTxt_ReturnsNotFound_WhenDisabled()
    {
        Skip.IfNot(appFixture.ApplicationAvailable, appFixture.ApplicationUnavailableSkipReason);

        var (statusCode, _) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(appFixture.BaseAddress);

        statusCode.Should().Be(404);
    }

    [SkippableFact]
    public async Task Admin_CanConfigureSecurityTxtViaUi_AndContentAppearsInPublicEndpoint()
    {
        Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(appFixture.ApplicationAvailable, appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(browserFixture.Browser!, appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsSecurityTxtMenuItemVisibleAsync()).Should().BeTrue();

        await pageObject.ClickSecurityTxtMenuItemAsync();
        var expires = DateTimeOffset.UtcNow.AddYears(1);
        await pageObject.EnableAndSaveSecurityTxtAsync("mailto:admin@example.com", expires);

        var (statusCode, body) = await SecurityTxtPageObject.GetPublicSecurityTxtAsync(appFixture.BaseAddress);

        statusCode.Should().Be(200);
        body.Should().Contain("Contact:");
        body.Should().Contain("mailto:admin@example.com");
    }

    [SkippableFact]
    public async Task RegularUser_DoesNotSeeSecurityTxtMenuItemInSettings()
    {
        Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(appFixture.ApplicationAvailable, appFixture.ApplicationUnavailableSkipReason);

        const string regularUsername = "securitytxt-nonadmin";
        const string regularPassword = "NonAdmin!456";
        await SecurityTxtPageObject.RegisterUserAsync(appFixture.BaseAddress, regularUsername, regularPassword);

        await using var pageObject = await SecurityTxtPageObject.CreateAsync(browserFixture.Browser!, appFixture.BaseAddress);
        await pageObject.LoginAsync(regularUsername, regularPassword);
        await pageObject.NavigateToSettingsAsync();

        (await pageObject.IsSecurityTxtMenuItemVisibleAsync()).Should().BeFalse();
    }
}
