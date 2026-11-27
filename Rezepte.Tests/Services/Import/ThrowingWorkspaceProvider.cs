using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Workspace provider test double that always fails to provide a work directory by throwing the
/// configured exception.
/// </summary>
/// <param name="exception">The exception parameter.</param>
/// <returns>The result.</returns>
internal sealed class ThrowingWorkspaceProvider(Exception exception) : IPluginWorkspaceProvider
{
    /// <inheritdoc />
    public string CreateWorkDirectory(string purpose)
    {
        throw exception;
    }
}
