using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Services.Import.Plugins;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Base class for <see cref="PluginDirectoryRegistry"/> tests providing a temporary workspace
/// with a managed <c>plugins</c> root below the content root.
/// </summary>
public abstract class PluginDirectoryRegistryTestsBase
{
    /// <summary>
    /// Creates a registry instance rooted at the given workspace.
    /// </summary>
    /// <param name="workspace">The workspace parameter.</param>
    /// <returns>The registry under test.</returns>
    protected static PluginDirectoryRegistry CreateRegistry(TempWorkspace workspace)
        => new(new TestHostEnvironment(workspace.Root), NullLogger<PluginDirectoryRegistry>.Instance);

    /// <summary>
    /// Host environment pointing its content root at the workspace root.
    /// </summary>
    /// <param name="contentRoot">The content root parameter.</param>
    /// <returns>The result.</returns>
    protected sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        /// <inheritdoc />
        public string EnvironmentName { get; set; } = Environments.Development;

        /// <inheritdoc />
        public string ApplicationName { get; set; } = "Rezepte.Tests";

        /// <inheritdoc />
        public string ContentRootPath { get; set; } = contentRoot;

        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRoot);
    }

    /// <summary>
    /// Temporary directory workspace with a <c>plugins</c> subdirectory that is removed on dispose.
    /// </summary>
    protected sealed class TempWorkspace : IDisposable
    {
        /// <summary>
        /// Gets the workspace root directory.
        /// </summary>
        /// <returns>The result.</returns>
        public string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "rezepte-registry-tests", Guid.NewGuid().ToString("N"))).FullName;

        /// <summary>
        /// Gets the managed plugins directory below the workspace root.
        /// </summary>
        /// <returns>The result.</returns>
        public string PluginRoot => Directory.CreateDirectory(Path.Combine(Root, "plugins")).FullName;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                try
                {
                    Directory.Delete(Root, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
