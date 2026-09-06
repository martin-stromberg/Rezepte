using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

/// <summary>
/// Class representing the plugin package installer tests.
/// </summary>
public sealed class PluginPackageInstallerTests
{
    /// <summary>
    /// Install async should restore backup and reinitialize after reload failure.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldRestoreBackupAndReinitializeAfterReloadFailure()
    {
        using var workspace = new TempWorkspace();
        var existingPlugin = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(existingPlugin, "PluginA.dll"), "original");
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginA")).FullName;
        await File.WriteAllTextAsync(Path.Combine(incomingPlugin, "PluginA.dll"), "replacement");
        var pluginManager = new FailingPluginManager();
        var sut = new PluginPackageInstaller(new TestHostEnvironment(workspace.Root), pluginManager, NullLogger<PluginPackageInstaller>.Instance);

        var act = () => sut.InstallAsync([incomingPlugin]);

        var error = await act.Should().ThrowAsync<PluginPackageInstallException>();
        error.Which.Status.Should().Be(PluginSourceReleaseStatus.ReloadFailed);
        error.Which.Message.Should().Be("reload failed");
        pluginManager.InitializeCalls.Should().Be(2);
        File.ReadAllText(Path.Combine(existingPlugin, "PluginA.dll")).Should().Be("original");
    }

    /// <summary>
    /// Install async should remove partially copied new target after copy failure.
    /// </summary>
    [Fact]
    public async Task InstallAsync_ShouldRemovePartiallyCopiedNewTargetAfterCopyFailure()
    {
        using var workspace = new TempWorkspace();
        var incomingPlugin = Directory.CreateDirectory(Path.Combine(workspace.Root, "incoming", "PluginB")).FullName;
        var lockedFile = Path.Combine(incomingPlugin, "PluginB.dll");
        await File.WriteAllTextAsync(lockedFile, "partial");
        await using var lockedStream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var sut = new PluginPackageInstaller(new TestHostEnvironment(workspace.Root), new SuccessfulPluginManager(), NullLogger<PluginPackageInstaller>.Instance);

        var act = () => sut.InstallAsync([incomingPlugin]);

        await act.Should().ThrowAsync<PluginPackageInstallException>();
        Directory.Exists(Path.Combine(workspace.PluginRoot, "PluginB")).Should().BeFalse();
    }

    private sealed class FailingPluginManager : IPluginManager
    {
        public int InitializeCalls { get; private set; }

        public Task InitializeAsync(CancellationToken ct = default)
        {
            InitializeCalls++;
            if (InitializeCalls == 1)
            {
                throw new InvalidOperationException("reload failed");
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);
    }

    private sealed class SuccessfulPluginManager : IPluginManager
    {
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginImportHandler>>([]);
    }

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Rezepte.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRoot);
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "rezepte-installer-tests", Guid.NewGuid().ToString("N"))).FullName;
        public string PluginRoot => Directory.CreateDirectory(Path.Combine(Root, "plugins")).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
