using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Shares one <see cref="PlaywrightBrowserFixture"/> and one default-configuration
/// <see cref="RezepteAppFixture"/> across the standard loading bar browser test classes.
/// </summary>
[CollectionDefinition(Name)]
public sealed class BrowserTestCollection : ICollectionFixture<PlaywrightBrowserFixture>, ICollectionFixture<RezepteAppFixture>
{
    public const string Name = "Rezepte Browser Tests";
}
