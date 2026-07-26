using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Services.Import.Plugins;
using System.IO.Compression;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class PluginPackageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ShouldRejectZipPathTraversal()
    {
        using var workspace = new TempWorkspace();
        var zipPath = Path.Combine(workspace.Root, "package.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            archive.CreateEntry("../evil.dll");
        }

        var sut = new PluginPackageValidator(new FakePluginManager([]), NullLogger<PluginPackageValidator>.Instance);

        var result = await sut.ValidateAsync(zipPath);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("unsafe path");
    }

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
        var sut = new PluginPackageValidator(new FakePluginManager(descriptors), NullLogger<PluginPackageValidator>.Instance);

        var result = await sut.ValidateAsync(zipPath);

        result.Success.Should().BeTrue();
        result.PluginDirectories.Select(Path.GetFileName).Should().BeEquivalentTo(["PluginA", "PluginB"]);
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
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
