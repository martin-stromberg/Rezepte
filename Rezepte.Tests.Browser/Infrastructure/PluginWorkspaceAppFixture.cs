using System.IO.Compression;
using Rezepte.Tests.TestHelpers;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Application fixture for the plugin workspace browser tests. The application process runs with
/// <c>TMP</c>/<c>TEMP</c>/<c>TMPDIR</c> and <c>PluginUpdates:WorkDirectory</c> pointing at paths
/// below a regular file so no work directory can be created there; the update check must fall
/// back to <c>{ContentRoot}/plugins/.work</c>. The GitHub API is stubbed by
/// <see cref="GitHubStubServer"/>, including a binary asset download endpoint for the real
/// download-validate-install path.
/// </summary>
public sealed class PluginWorkspaceAppFixture : RezepteAppFixture
{
    private const string PluginDirectoryName = "Rezepte.E2E.TestPlugin";

    private readonly GitHubStubServer _gitHubStub = GitHubStubServer.Start();
    private readonly string _blockerRoot = Directory.CreateTempSubdirectory("rezepte-plugin-workspace-tests-").FullName;
    private readonly string _blockedBasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginWorkspaceAppFixture"/> class and
    /// prepares the regular file that makes every candidate base path uncreatable.
    /// </summary>
    public PluginWorkspaceAppFixture()
    {
        var blockerFile = Path.Combine(_blockerRoot, "blocker");
        File.WriteAllText(blockerFile, "blocked");
        _blockedBasePath = Path.Combine(blockerFile, "child");
    }

    /// <summary>
    /// Gets the local GitHub API stub the application under test is pointed at.
    /// </summary>
    public GitHubStubServer GitHubStub => _gitHubStub;

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string?> GetEnvironmentOverrides()
    {
        return new Dictionary<string, string?>
        {
            ["PluginUpdates__GitHubApiBaseUrl"] = _gitHubStub.BaseAddress,
            ["PluginUpdates__WorkDirectory"] = Path.Combine(_blockedBasePath, "configured"),
            ["TMP"] = Path.Combine(_blockedBasePath, "tmp"),
            ["TEMP"] = Path.Combine(_blockedBasePath, "tmp"),
            ["TMPDIR"] = Path.Combine(_blockedBasePath, "tmp")
        };
    }

    /// <summary>
    /// Builds the release package served by the stub's asset endpoint: a plugin directory with
    /// the real backup plugin assembly and its contract assembly. A clean plugin is required
    /// because the package validator fails on any incompatible descriptor.
    /// </summary>
    /// <returns>The ZIP archive bytes.</returns>
    public static byte[] CreateUpdatePackageZip()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntryFromFile(
                Path.Combine(AppContext.BaseDirectory, "Rezepte.Import.Plugins.Backup.dll"),
                $"{PluginDirectoryName}/Rezepte.Import.Plugins.Backup.dll");
            archive.CreateEntryFromFile(
                Path.Combine(AppContext.BaseDirectory, "Rezepte.Import.Abstractions.dll"),
                $"{PluginDirectoryName}/Rezepte.Import.Abstractions.dll");
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Replaces the <c>.work</c> directory below the published plugins root with a regular file,
    /// so the content-root fallback candidate fails as well.
    /// </summary>
    public void BlockPluginsWorkDirectory()
    {
        var workPath = GetPluginsWorkPath();
        if (Directory.Exists(workPath))
        {
            TempDirectoryCleanup.DeleteBestEffort(workPath);
            if (Directory.Exists(workPath))
            {
                // Assemblies loaded from .work through a collectible load context stay locked
                // until the context is collected, which makes the deletion fail. Renaming the
                // directory still works, because the lock is held on the files, not the path.
                Directory.Move(workPath, $"{workPath}.blocked-{Guid.NewGuid():N}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(workPath)!);
        File.WriteAllText(workPath, "blocked");
    }

    /// <summary>
    /// Removes a <c>.work</c> blocker file again; a regular work directory is left untouched.
    /// </summary>
    public void UnblockPluginsWorkDirectory()
    {
        var workPath = GetPluginsWorkPath();
        if (File.Exists(workPath))
        {
            File.Delete(workPath);
        }
    }

    /// <summary>
    /// Stops the application process first, then removes the plugin artifacts the update checks
    /// created in the shared publish directory, and finally stops the GitHub stub server.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        CleanupPluginArtifacts();
        _gitHubStub.Dispose();
        TempDirectoryCleanup.DeleteBestEffort(_blockerRoot);
    }

    private string GetPluginsWorkPath()
    {
        var applicationDirectory = ApplicationDirectory
            ?? throw new InvalidOperationException("The application fixture has not been initialized.");
        return Path.Combine(applicationDirectory, "plugins", ".work");
    }

    private void CleanupPluginArtifacts()
    {
        var applicationDirectory = ApplicationDirectory;
        if (applicationDirectory is null)
        {
            return;
        }

        var pluginsRoot = Path.Combine(applicationDirectory, "plugins");
        if (!Directory.Exists(pluginsRoot))
        {
            return;
        }

        var workPath = Path.Combine(pluginsRoot, ".work");
        if (File.Exists(workPath))
        {
            TempDirectoryCleanup.DeleteFileBestEffort(workPath);
        }
        else if (Directory.Exists(workPath))
        {
            TempDirectoryCleanup.DeleteBestEffort(workPath);
        }

        foreach (var renamedWorkDirectory in Directory.EnumerateDirectories(pluginsRoot, ".work.blocked-*"))
        {
            TempDirectoryCleanup.DeleteBestEffort(renamedWorkDirectory);
        }

        TempDirectoryCleanup.DeleteFileBestEffort(Path.Combine(pluginsRoot, ".install-state.json"));
        foreach (var versionedDirectory in Directory.EnumerateDirectories(pluginsRoot, $"{PluginDirectoryName}@*"))
        {
            TempDirectoryCleanup.DeleteBestEffort(versionedDirectory);
        }
    }
}
