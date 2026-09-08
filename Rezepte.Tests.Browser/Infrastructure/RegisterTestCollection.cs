using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Shares one <see cref="PlaywrightBrowserFixture"/> and one <see cref="RegisterTestsAppFixture"/>
/// across the registration browser tests. A separate collection is required because the
/// registration tests need an application instance without the shared test user and restart it
/// against an empty database, which must not affect the other browser tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class RegisterTestCollection : ICollectionFixture<PlaywrightBrowserFixture>, ICollectionFixture<RegisterTestsAppFixture>
{
    /// <summary>
    /// The collection name used by xUnit's <see cref="CollectionDefinitionAttribute"/>.
    /// </summary>
    public const string Name = "Rezepte Register Browser Tests";
}
