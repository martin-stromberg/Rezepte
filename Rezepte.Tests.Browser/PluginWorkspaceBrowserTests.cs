using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Rezepte.Tests.TestHelpers;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests for the plugin update work directory fallback. The application under test runs
/// with a broken system temp directory (<c>TMP</c>/<c>TEMP</c>/<c>TMPDIR</c> point below a regular
/// file) and a blocked <c>PluginUpdates:WorkDirectory</c>, so the update check can only succeed
/// through the <c>{ContentRoot}/plugins/.work</c> fallback.
/// </summary>
[Collection(PluginWorkspaceTestCollection.Name)]
[Trait(TestCategories.Category, TestCategories.OsInterface)]
public class PluginWorkspaceBrowserTests
{
    private const string Owner = "e2eworkspace";

    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly PluginWorkspaceAppFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginWorkspaceBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The shared application fixture.</param>
    public PluginWorkspaceBrowserTests(PlaywrightBrowserFixture browserFixture, PluginWorkspaceAppFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;

        // The stub and the .work blocker are shared across the collection; every test starts
        // from a defined state so no test inherits a release or a blocker from another test.
        _appFixture.GitHubStub.RespondNotFound();
        if (_appFixture.ApplicationAvailable)
        {
            _appFixture.UnblockPluginsWorkDirectory();
        }
    }

    /// <summary>
    /// Verifies that a manual update check downloads, validates and installs the release package
    /// through the <c>plugins/.work</c> fallback when both the configured work directory and the
    /// system temp directory are unusable.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckButton_WhenSystemTempBroken_InstallsReleaseViaPluginsWorkFallback()
    {
        Assert.SkipUnless(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Assert.SkipUnless(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        _appFixture.GitHubStub.RespondWithRelease(releaseId: 555, tagName: "v9.9.9", assetId: 9090, assetName: "plugin.zip");
        _appFixture.GitHubStub.RespondWithAssetDownload(PluginWorkspaceAppFixture.CreateUpdatePackageZip());

        await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
        await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
        await pageObject.NavigateToPluginSettingsAsync();

        const string Repository = "plugin-e2e-workspace-fallback";
        await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: false);

        await pageObject.ClickCheckButtonAsync($"{Owner}/{Repository}");

        var alertText = await pageObject.GetAlertTextAsync();
        alertText.Should().Contain("Version v9.9.9 installiert.",
            "the update check must have used the plugins/.work fallback to download, validate and install the package");
    }

    /// <summary>
    /// Verifies that a manual update check shows the localized work-directory error when every
    /// candidate — the configured directory, the system temp directory and the
    /// <c>plugins/.work</c> fallback blocked by a regular file — fails.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckButton_WhenAllWorkDirectoryCandidatesFail_ShowsLocalizedError()
    {
        Assert.SkipUnless(_browserFixture.BrowsersAvailable, _browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.");
        Assert.SkipUnless(_appFixture.ApplicationAvailable, _appFixture.ApplicationUnavailableSkipReason);

        _appFixture.GitHubStub.RespondWithRelease(releaseId: 556, tagName: "v9.9.9", assetId: 9091, assetName: "plugin.zip");
        _appFixture.BlockPluginsWorkDirectory();
        try
        {
            await using var pageObject = await PluginSettingsPageObject.CreateAsync(_browserFixture.Browser!, _appFixture.BaseAddress);
            await pageObject.LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword);
            await pageObject.NavigateToPluginSettingsAsync();

            const string Repository = "plugin-e2e-workspace-blocked";
            await pageObject.AddSourceAsync($"https://github.com/{Owner}/{Repository}", $"{Owner}/{Repository}", isPrivate: false);

            await pageObject.ClickCheckButtonAsync($"{Owner}/{Repository}");

            var alertText = await pageObject.GetAlertTextAsync();
            alertText.Should().Contain("Das Arbeitsverzeichnis für die Plugin-Aktualisierung konnte nicht angelegt werden.");
        }
        finally
        {
            _appFixture.UnblockPluginsWorkDirectory();
        }
    }
}
