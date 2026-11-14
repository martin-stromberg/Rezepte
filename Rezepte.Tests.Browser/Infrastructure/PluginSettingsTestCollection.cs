using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Shares one <see cref="PlaywrightBrowserFixture"/> and one <see cref="PluginSettingsAppFixture"/>
/// across the plugin settings browser tests. A separate collection is required because the
/// fixture overrides <c>PluginUpdates:GitHubApiBaseUrl</c> to point at a local stub, which must
/// not leak into the other browser tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PluginSettingsTestCollection : ICollectionFixture<PlaywrightBrowserFixture>, ICollectionFixture<PluginSettingsAppFixture>
{
    /// <summary>
    /// The collection name used by xUnit's <see cref="CollectionDefinitionAttribute"/>.
    /// </summary>
    public const string Name = "Rezepte Plugin Settings Browser Tests";
}
