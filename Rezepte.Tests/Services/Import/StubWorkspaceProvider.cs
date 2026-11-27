using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Workspace provider test double that creates real work directories below the given base
/// directory in the <c>{base}/{purpose}/{guid}</c> structure.
/// </summary>
/// <param name="root">The base directory parameter.</param>
/// <returns>The result.</returns>
internal sealed class StubWorkspaceProvider(string root) : IPluginWorkspaceProvider
{
    /// <inheritdoc />
    public string CreateWorkDirectory(string purpose)
        => Directory.CreateDirectory(Path.Combine(root, purpose, Guid.NewGuid().ToString("N"))).FullName;
}
