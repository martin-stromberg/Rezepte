using FluentAssertions;
using Rezepte.Tests.Browser.Infrastructure;
using Xunit;

namespace Rezepte.Tests.Browser;

[Collection(BrowserTestCollection.Name)]
public class LoadingBarDisabledBrowserTests(
    PlaywrightBrowserFixture browserFixture,
    LoadingBarDisabledBrowserTests.DisabledFixture appFixture)
    : IClassFixture<LoadingBarDisabledBrowserTests.DisabledFixture>
{
    [SkippableFact]
    public async Task WhenFeatureDisabled_PageContainsNoLoadingBarElement()
    {
        await using var pageObject = await LoadingBarBrowserSession.StartLoggedInSessionAsync(browserFixture, appFixture);

        var count = await pageObject.Page.Locator("#loading-bar").CountAsync();

        count.Should().Be(0);
    }

    public sealed class DisabledFixture() : ConfiguredRezepteAppFixture(new Dictionary<string, string?>
    {
        ["LoadingBar__Enabled"] = "false"
    });
}
