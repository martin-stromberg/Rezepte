using Xunit;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Shares one <see cref="PlaywrightBrowserFixture"/> and one <see cref="PluginWorkspaceAppFixture"/>
/// across the plugin workspace browser tests. A separate collection is required because the
/// fixture breaks <c>TMP</c>/<c>TEMP</c>/<c>TMPDIR</c> for its application process and toggles
/// the shared <c>{publish}/plugins/.work</c> path between directory and blocker file, both of
/// which must not leak into other browser tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PluginWorkspaceTestCollection : ICollectionFixture<PlaywrightBrowserFixture>, ICollectionFixture<PluginWorkspaceAppFixture>
{
    /// <summary>
    /// The collection name used by xUnit's <see cref="CollectionDefinitionAttribute"/>.
    /// </summary>
    public const string Name = "Rezepte Plugin Workspace Browser Tests";
}
