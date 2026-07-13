using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using Xunit;

namespace Rezepte.Tests.Services.Import;

public sealed class PluginManagerTests
{
    [Fact]
    public async Task InitializeAsync_ShouldDiscoverExternalPluginDirectlyUnderPlugins()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyFixturePlugin(workspace.PluginRoot);
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("external-test-plugin");
        plugin.Should().NotBeNull();
        plugin!.Status.Should().Be(PluginStatus.Loaded);
        plugin.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ShouldDiscoverExternalPluginFromSubfolderWithAdjacentAbstractionsAssembly()
    {
        using var workspace = PluginWorkspace.Create();
        var pluginFolder = Directory.CreateDirectory(Path.Combine(workspace.PluginRoot, "Rezepte.Tests.PluginFixture")).FullName;
        workspace.CopyFixturePlugin(pluginFolder);
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("external-test-plugin");
        plugin.Should().NotBeNull();
        plugin!.Status.Should().Be(PluginStatus.Loaded);
        plugin.Error.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_ShouldMarkBrokenDllAsLoadFailed()
    {
        using var workspace = PluginWorkspace.Create();
        File.WriteAllText(Path.Combine(workspace.PluginRoot, "Broken.dll"), "not a managed assembly");
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("loadfailed:Broken");
        plugin.Should().NotBeNull();
        plugin!.Status.Should().Be(PluginStatus.LoadFailed);
        plugin.Enabled.Should().BeFalse();
        plugin.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InitializeAsync_ShouldIgnoreAdjacentContractAssemblyWithoutPlugin()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyOutputAssembly("Rezepte.Import.Abstractions.dll", workspace.PluginRoot);
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("incompatible:Rezepte.Import.Abstractions");
        plugin.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_ShouldMarkPluginWithInvalidHandlerTypeAsIncompatible()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyFixturePlugin(workspace.PluginRoot);
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("invalid-handler-plugin");
        plugin.Should().NotBeNull();
        plugin!.Status.Should().Be(PluginStatus.Incompatible);
        plugin.Enabled.Should().BeFalse();
        plugin.Error.Should().Be("Configured handler type does not implement IImportHandler.");
    }

    [Fact]
    public async Task InitializeAsync_ShouldKeepExistingOrderAndAppendNewPlugins()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyFixturePlugin(workspace.PluginRoot);
        var provider = CreateServices(workspace.ContentRoot, out var sut);

        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RezepteDbContext>();
            db.PluginSettings.Add(CreateSetting("existing-plugin", 10, PluginStatus.Missing));
            await db.SaveChangesAsync();
        }

        await sut.InitializeAsync();

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var existing = await verifyDb.PluginSettings.FindAsync("existing-plugin");
        var added = await verifyDb.PluginSettings.FindAsync("external-test-plugin");
        existing!.OrderIndex.Should().Be(10);
        added!.OrderIndex.Should().BeGreaterThan(existing.OrderIndex);
    }

    [Fact]
    public async Task InitializeAsync_ShouldMarkPreviouslyConfiguredPluginAsMissing()
    {
        using var workspace = PluginWorkspace.Create();
        var provider = CreateServices(workspace.ContentRoot, out var sut);

        await using (var seedScope = provider.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<RezepteDbContext>();
            db.PluginSettings.Add(CreateSetting("vanished-plugin", 0, PluginStatus.Loaded));
            await db.SaveChangesAsync();
        }

        await sut.InitializeAsync();

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var missing = await verifyDb.PluginSettings.FindAsync("vanished-plugin");
        missing!.Status.Should().Be(PluginStatus.Missing);
        missing.Error.Should().Be("Plugin was not found during startup discovery.");
    }

    [Fact]
    public async Task GetActiveHandlersAsync_ShouldNotInstantiateDisabledPlugins()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyFixturePlugin(workspace.PluginRoot);
        var provider = CreateServices(workspace.ContentRoot, out var sut);

        await sut.InitializeAsync();
        await using (var updateScope = provider.CreateAsyncScope())
        {
            var db = updateScope.ServiceProvider.GetRequiredService<RezepteDbContext>();
            var plugin = await db.PluginSettings.FindAsync("external-test-plugin");
            plugin!.Enabled = false;
            await db.SaveChangesAsync();
        }

        await using var importScope = provider.CreateAsyncScope();
        var handlers = await sut.GetActiveHandlersAsync(importScope.ServiceProvider);

        handlers.Should().NotContain(h => h.Plugin.Id == "external-test-plugin");
    }

    private static ServiceProvider CreateServices(string contentRoot, out PluginManager manager)
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<RezepteDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(contentRoot));
        services.AddLogging();
        services.AddSingleton<PluginManager>();

        var provider = services.BuildServiceProvider();
        manager = new PluginManager(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IHostEnvironment>(),
            NullLogger<PluginManager>.Instance);
        return provider;
    }

    private static PluginSetting CreateSetting(string pluginId, int orderIndex, string status)
    {
        return new PluginSetting
        {
            PluginId = pluginId,
            DisplayName = pluginId,
            AssemblyName = "TestAssembly",
            TypeName = "TestHandler",
            Enabled = true,
            OrderIndex = orderIndex,
            Status = status,
            DiscoveredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }

    private sealed class TestHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Rezepte.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRoot);
    }

    private sealed class PluginWorkspace : IDisposable
    {
        private PluginWorkspace(string contentRoot)
        {
            ContentRoot = contentRoot;
            PluginRoot = Directory.CreateDirectory(Path.Combine(contentRoot, "plugins")).FullName;
        }

        public string ContentRoot { get; }
        public string PluginRoot { get; }

        public static PluginWorkspace Create()
        {
            return new PluginWorkspace(Path.Combine(Path.GetTempPath(), "rezepte-plugin-tests", Guid.NewGuid().ToString("N")));
        }

        public void CopyFixturePlugin(string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            CopyOutputAssembly("Rezepte.Tests.PluginFixture.dll", targetDirectory);
            CopyOutputAssembly("Rezepte.Import.Abstractions.dll", targetDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(ContentRoot))
            {
                try
                {
                    Directory.Delete(ContentRoot, recursive: true);
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        public void CopyOutputAssembly(string fileName, string targetDirectory)
        {
            var source = Path.Combine(AppContext.BaseDirectory, fileName);
            File.Exists(source).Should().BeTrue($"test fixture assembly {fileName} should be copied to the test output");
            File.Copy(source, Path.Combine(targetDirectory, fileName), overwrite: true);
        }
    }
}
