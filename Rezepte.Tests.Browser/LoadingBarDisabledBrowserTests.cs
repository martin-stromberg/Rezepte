using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

/// <summary>
/// Browser tests that verify the loading bar is not rendered when disabled.
/// </summary>
[Collection(BrowserTestCollection.Name)]
public class LoadingBarDisabledBrowserTests : IClassFixture<LoadingBarDisabledBrowserTests.DisabledFixture>
{
    private readonly PlaywrightBrowserFixture _browserFixture;
    private readonly DisabledFixture _appFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingBarDisabledBrowserTests"/> class.
    /// </summary>
    /// <param name="browserFixture">The shared Playwright browser fixture.</param>
    /// <param name="appFixture">The fixture that disables the loading bar.</param>
    public LoadingBarDisabledBrowserTests(PlaywrightBrowserFixture browserFixture, DisabledFixture appFixture)
    {
        _browserFixture = browserFixture;
        _appFixture = appFixture;
    }

    /// <summary>
    /// Verifies that the loading bar host element is absent when the feature is disabled.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [SkippableFact]
    public async Task WhenFeatureDisabled_PageContainsNoLoadingBarElement()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(_browserFixture, _appFixture);

        var count = await pageObject.Page.Locator("#loading-bar").CountAsync();

        count.Should().Be(0);
    }

    /// <summary>
    /// Fixture that starts the application with the loading bar disabled.
    /// </summary>
    public sealed class DisabledFixture : ConfiguredRezepteAppFixture
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisabledFixture"/> class.
        /// </summary>
        public DisabledFixture() : base(new Dictionary<string, string?>
        {
            ["LoadingBar__Enabled"] = "false"
        })
        {
        }
    }
}
