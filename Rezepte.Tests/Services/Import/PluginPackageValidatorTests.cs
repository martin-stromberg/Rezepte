using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Tests.TestHelpers;
using Rezepte.Web.Services.Import.Plugins;
using System.IO.Compression;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the plugin package validator tests.
/// </summary>
public sealed class PluginPackageValidatorTests
{
    /// <summary>
    /// Validate async should reject zip path traversal.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ShouldRejectZipPathTraversal()
    {
        using var workspace = new TempWorkspace();
        var zipPath = Path.Combine(workspace.Root, "package.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("../evil.dll");
        }

        var sut = new PluginPackageValidator(new FakePluginManager([]), new StubWorkspaceProvider(workspace.Root), NullLogger<PluginPackageValidator>.Instance);

        var result = await sut.ValidateAsync(zipPath);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("unsafe path");
    }

    /// <summary>
    /// Validate async should accept multiple plugin subdirectories when discovery succeeds.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ShouldAcceptMultiplePluginSubdirectoriesWhenDiscoverySucceeds()
    {
        using var workspace = new TempWorkspace();
        var zipPath = Path.Combine(workspace.Root, "package.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("PluginA/PluginA.dll");
            archive.CreateEntry("PluginB/PluginB.dll");
        }

        var descriptors = new[]
        {
            new ImportPluginDescriptor("plugin-a", "Plugin A", null, "1.0.0", "PluginA", "PluginA.Handler", typeof(object), 0, PluginStatus.Loaded, null, null),
            new ImportPluginDescriptor("plugin-b", "Plugin B", null, "1.0.0", "PluginB", "PluginB.Handler", typeof(object), 0, PluginStatus.Loaded, null, null)
        };
        var sut = new PluginPackageValidator(new FakePluginManager(descriptors), new StubWorkspaceProvider(workspace.Root), NullLogger<PluginPackageValidator>.Instance);

        var result = await sut.ValidateAsync(zipPath);

        result.Success.Should().BeTrue();
        result.PluginDirectories.Select(Path.GetFileName).Should().BeEquivalentTo(["PluginA", "PluginB"]);
    }

    /// <summary>
    /// Validate async should propagate the workspace exception when no work directory can be
    /// provided instead of translating it into a generic extraction failure.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ShouldPropagateWorkspaceException_WhenWorkDirectoryUnavailable()
    {
        using var workspace = new TempWorkspace();
        var zipPath = Path.Combine(workspace.Root, "package.zip");
        var sut = new PluginPackageValidator(new FakePluginManager([]), new ThrowingWorkspaceProvider(new PluginWorkspaceException()), NullLogger<PluginPackageValidator>.Instance);

        var act = () => sut.ValidateAsync(zipPath);

        var error = await act.Should().ThrowAsync<PluginWorkspaceException>();
        error.Which.Message.Should().Be(PluginUpdateCheckErrors.WorkDirectoryUnavailable);
    }

    /// <summary>
    /// Validate async should delete the extraction directory when cancellation propagates without
    /// a validation result, so no orphaned work directory remains below the work root.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_ShouldDeleteExtractedRoot_WhenCancelled()
    {
        using var workspace = new TempWorkspace();
        var zipPath = Path.Combine(workspace.Root, "package.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("PluginA/PluginA.dll");
        }

        var sut = new PluginPackageValidator(new FakePluginManager([]), new StubWorkspaceProvider(workspace.Root), NullLogger<PluginPackageValidator>.Instance);
        var cancelled = new CancellationToken(canceled: true);

        var act = () => sut.ValidateAsync(zipPath, cancelled);

        await act.Should().ThrowAsync<OperationCanceledException>();
        var purposeDirectory = Path.Combine(workspace.Root, "rezepte-plugin-package");
        var leftovers = Directory.Exists(purposeDirectory) ? Directory.GetDirectories(purposeDirectory) : [];
        leftovers.Should().BeEmpty("a cancelled validation must not leave an orphaned extraction directory");
    }

    private sealed class FakePluginManager(IReadOnlyList<ImportPluginDescriptor> descriptors) : IPluginManager
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public IReadOnlyList<ImportPluginDescriptor> DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery = false) => descriptors;
        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "rezepte-validator-tests", Guid.NewGuid().ToString("N"))).FullName;

        public void Dispose()
        {
            TempDirectoryCleanup.DeleteBestEffort(Root);
        }
    }
}
