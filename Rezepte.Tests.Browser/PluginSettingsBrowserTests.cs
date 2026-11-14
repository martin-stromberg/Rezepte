using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests for the per-source manual update check in the plugin settings panel.
/// </summary>
[Collection(PluginSettingsTestCollection.Name)]
public class PluginSettingsBrowserTests
{
    private const string Owner = "e2eowner";

    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly PluginSettingsAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSettingsBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public PluginSettingsBrowserTests(PlaywrightBrowserFixture browserFixture, PluginSettingsAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;

        // The stub is shared across the collection; every test starts from a defined
        // not-found response so no test inherits a release configured by another test.
        _appFixture.GitHubStub.RespondNotFound();
    }

    /// <summary>
    /// Verifies that the plugin sources section shows the localized explanation text and a check
    /// button with a localized tooltip for a newly added source.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task PluginSettings_ShowsInfoText_AndCheckButtonWithTooltip()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToPluginSettingsAsync();

        const string Repository = "plugin-e2e-info";
        await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: false);

        (await pageObject.IsSourcesInfoTextVisibleAsync()).Should().BeTrue();
        (await pageObject.GetCheckButtonTitleAsync($"{Owner}/{Repository}")).Should().Be("Auf Updates prüfen");
        (await pageObject.IsCheckButtonDisabledAsync($"{Owner}/{Repository}")).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that clicking the check button on a private source without a stored PAT shows the
    /// localized failure alert and updates the status cell with the error and check timestamps.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task CheckButton_WithoutPat_ShowsLocalizedError_AndUpdatesStatus()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToPluginSettingsAsync();

        const string Repository = "plugin-e2e-nopat";
        await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: true);

        await pageObject.ClickCheckButtonAsync($"{Owner}/{Repository}");

        var alertText = await pageObject.GetAlertTextAsync();
        alertText.Should().Contain("Für die private Quelle ist kein PAT hinterlegt.");

        var statusText = await pageObject.GetSourceStatusTextAsync($"{Owner}/{Repository}");
        statusText.Should().Contain("Letzter Fehler");
        statusText.Should().Contain("Zuletzt geprüft");
    }

    /// <summary>
    /// Verifies that deactivating a source disables its check button and that the tooltip wrapper
    /// explains the reason.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task CheckButton_IsDisabled_WithReason_WhenSourceDeactivated()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToPluginSettingsAsync();

        const string Repository = "plugin-e2e-disabled";
        await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: false);

        await pageObject.SetSourceEnabledAsync($"{Owner}/{Repository}", enabled: false);

        (await pageObject.IsCheckButtonDisabledAsync($"{Owner}/{Repository}")).Should().BeTrue();
        (await pageObject.GetCheckButtonTooltipAsync($"{Owner}/{Repository}")).Should().Be("Quelle ist deaktiviert.");
    }

    /// <summary>
    /// Verifies that a check whose latest release is already installed reports
    /// "Kein Update gefunden." through a success alert.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task CheckButton_WhenReleaseAlreadyInstalled_ShowsNoUpdate()
    {
        Skip.IfNot(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Skip.IfNot(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        _appFixture.GitHubStub.RespondWithRelease(releaseId: 777, tagName: "v9.9.9", assetId: 4242, assetName: "plugin.zip");

        await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToPluginSettingsAsync();

        const string Repository = "plugin-e2e-installed";
        await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: false);
        await _appFixture.SeedInstalledReleaseAsync(Owner, Repository, "v9.9.9", gitHubReleaseId: 777, assetId: 4242, assetName: "plugin.zip");

        await pageObject.ClickCheckButtonAsync($"{Owner}/{Repository}");

        var alertText = await pageObject.GetAlertTextAsync();
        alertText.Should().Contain("Kein Update gefunden.");
        _appFixture.GitHubStub.RequestCount.Should().BeGreaterThan(0,
            "the update check must have queried the local GitHub stub instead of the real API");
    }
}
