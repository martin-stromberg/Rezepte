using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using Rezepte.Web.Services.Import.Plugins;
using System.Diagnostics;
using System.Text;
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
    public async Task InitializeAsync_ShouldDiscoverExternalPluginFromApplicationBaseDirectory()
    {
        using var workspace = PluginWorkspace.CreateWithoutPluginRoot();
        var pluginFolder = Directory.CreateDirectory(Path.Combine(
            AppContext.BaseDirectory,
            "plugins",
            $"Rezepte.Tests.PluginFixture.{Guid.NewGuid():N}")).FullName;

        try
        {
            workspace.CopyFixturePlugin(pluginFolder);
            await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

            await sut.InitializeAsync();

            var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
            var plugin = await db.PluginSettings.FindAsync("external-test-plugin");
            plugin.Should().NotBeNull();
            plugin!.Status.Should().Be(PluginStatus.Loaded);
            plugin.Error.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(pluginFolder))
            {
                try
                {
                    Directory.Delete(pluginFolder, recursive: true);
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }
        }
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
    public async Task InitializeAsync_ShouldDiscoverProductiveExternalImportPlugins()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyProductivePlugins();
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var pluginIds = await db.PluginSettings
            .AsNoTracking()
            .Where(p => p.Status == PluginStatus.Loaded)
            .OrderBy(p => p.PluginId)
            .Select(p => p.PluginId)
            .ToListAsync();

        pluginIds.Should().Contain([
            "ai-foto",
            "ai-url",
            "backup"
        ]);
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly()
    {
        if (!PluginWorkspace.ExternalPluginRepositoryExists())
        {
            return;
        }

        using var workspace = PluginWorkspace.Create();
        workspace.PublishExternalPlugin("Rezepte.Import.Plugins.Chefkoch");
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var plugin = await db.PluginSettings.FindAsync("chefkoch");
        plugin.Should().NotBeNull();
        plugin!.Status.Should().Be(PluginStatus.Loaded);
        plugin.Enabled.Should().BeTrue();

        var handlers = await sut.GetActiveHandlersAsync(scope.ServiceProvider);
        var chefkoch = handlers.Should().ContainSingle(h => h.Plugin.Id == "chefkoch").Subject;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PluginWorkspace.ReadChefkochFixture()));
        (await chefkoch.Handler.CanHandleAsync(stream, "chefkoch-recipe.html")).Should().BeTrue();
        stream.Position = 0;
        var result = await chefkoch.Handler.HandleAsync(stream, "chefkoch-recipe.html", null, "cookbook-1", "user-1");
        result.Success.Should().BeTrue();
        result.ImportedRecipes.Should().NotBeNull();
        result.ImportedRecipes!.Should().ContainSingle(r => r.Title == "Chefkoch-Demo-Rezept");
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly()
    {
        if (!PluginWorkspace.ExternalPluginRepositoryExists())
        {
            return;
        }

        using var workspace = PluginWorkspace.Create();
        foreach (var pluginProject in PluginWorkspace.ExternalOnlinePluginProjects)
        {
            workspace.PublishExternalPlugin(pluginProject);
        }

        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var pluginIds = await db.PluginSettings
            .AsNoTracking()
            .Where(p => p.Status == PluginStatus.Loaded)
            .OrderBy(p => p.PluginId)
            .Select(p => p.PluginId)
            .ToListAsync();

        pluginIds.Should().Contain([
            "chefkoch",
            "fifth-source",
            "fourth-source",
            "second-source",
            "sixth-source",
            "third-source"
        ]);

        var handlers = await sut.GetActiveHandlersAsync(scope.ServiceProvider);
        handlers.Select(h => h.Plugin.Id).Should().Contain([
            "chefkoch",
            "fifth-source",
            "fourth-source",
            "second-source",
            "sixth-source",
            "third-source"
        ]);

        foreach (var pluginProject in PluginWorkspace.ExternalOnlinePluginProjects)
        {
            File.Exists(Path.Combine(workspace.PluginRoot, pluginProject, "Rezepte.Import.Abstractions.dll")).Should().BeFalse();
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldUseDefaultPriorityForInitialOrder()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyProductivePlugins();
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();

        await sut.InitializeAsync();

        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        var pluginIds = await db.PluginSettings
            .AsNoTracking()
            .Where(p => p.Status == PluginStatus.Loaded)
            .OrderBy(p => p.OrderIndex)
            .Select(p => p.PluginId)
            .ToListAsync();

        pluginIds.Should().EndWith(["ai-foto", "ai-url"]);
        pluginIds.Where(id => id.StartsWith("ai-", StringComparison.Ordinal)).Should().HaveCount(2);
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
        workspace.CopyProductivePlugins();
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
        var aiFoto = await verifyDb.PluginSettings.FindAsync("ai-foto");
        var aiUrl = await verifyDb.PluginSettings.FindAsync("ai-url");
        existing!.OrderIndex.Should().Be(10);
        added!.OrderIndex.Should().BeGreaterThan(existing.OrderIndex);
        aiFoto!.OrderIndex.Should().BeGreaterThan(existing.OrderIndex);
        aiUrl!.OrderIndex.Should().BeGreaterThan(existing.OrderIndex);
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
    public void DiscoverFromDirectory_ShouldNotKeepAssembliesLoadedFromTemporaryDirectory()
    {
        using var workspace = PluginWorkspace.Create();
        var tempPluginFolder = Directory.CreateDirectory(Path.Combine(workspace.ContentRoot, "validation", "Rezepte.Tests.PluginFixture")).FullName;
        workspace.CopyFixturePlugin(tempPluginFolder);
        CreateServices(workspace.ContentRoot, out var sut).Dispose();

        var descriptors = sut.DiscoverFromDirectory(Path.Combine(workspace.ContentRoot, "validation"), unloadAfterDiscovery: true);

        descriptors.Should().Contain(p => p.Id == "external-test-plugin" && p.Status == PluginStatus.Loaded);
        for (var i = 0; i < 5; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (IsCodeCoverageCollectorAttached())
        {
            // The in-process coverlet.core data collector retains references to every assembly it observes,
            // which keeps collectible AssemblyLoadContexts from unloading regardless of product behavior.
            return;
        }

        AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => a.Location)
            .Should()
            .NotContain(path => path.StartsWith(Path.Combine(workspace.ContentRoot, "validation"), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCodeCoverageCollectorAttached()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => string.Equals(a.GetName().Name, "coverlet.core", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public async Task DefaultCheckUsabilityAsync_ShouldReturnUsable_ForPluginWithoutOverride()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyProductivePlugins();
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();
        await sut.InitializeAsync();

        var results = await sut.GetPluginsUsabilityAsync(scope.ServiceProvider);

        results.Should().ContainKey("backup");
        results["backup"].IsUsable.Should().BeTrue();
        results["backup"].Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPluginsUsabilityAsync_ShouldReturnResultsForLoadedPlugins()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyProductivePlugins();
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();
        await sut.InitializeAsync();

        var results = await sut.GetPluginsUsabilityAsync(scope.ServiceProvider);

        results.Keys.Should().Contain(["backup", "ai-foto", "ai-url"]);
    }

    [Fact]
    public async Task GetPluginsUsabilityAsync_ShouldTreatCheckExceptionAsNotUsable()
    {
        using var workspace = PluginWorkspace.Create();
        workspace.CopyFixturePlugin(workspace.PluginRoot);
        await using var scope = CreateServices(workspace.ContentRoot, out var sut).CreateAsyncScope();
        await sut.InitializeAsync();

        var results = await sut.GetPluginsUsabilityAsync(scope.ServiceProvider);

        results.Should().ContainKey("throwing-usability-plugin");
        results["throwing-usability-plugin"].IsUsable.Should().BeFalse();
        results["throwing-usability-plugin"].Issues.Should().ContainSingle();
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
        public static IReadOnlyList<string> ExternalOnlinePluginProjects { get; } =
        [
            "Rezepte.Import.Plugins.Chefkoch",
            "Rezepte.Import.Plugins.SecondSource",
            "Rezepte.Import.Plugins.ThirdSource",
            "Rezepte.Import.Plugins.FourthSource",
            "Rezepte.Import.Plugins.FifthSource",
            "Rezepte.Import.Plugins.SixthSource"
        ];

        public static PluginWorkspace Create()
        {
            return new PluginWorkspace(Path.Combine(Path.GetTempPath(), "rezepte-plugin-tests", Guid.NewGuid().ToString("N")));
        }

        public static PluginWorkspace CreateWithoutPluginRoot()
        {
            var workspace = new PluginWorkspace(Path.Combine(Path.GetTempPath(), "rezepte-plugin-tests", Guid.NewGuid().ToString("N")));
            Directory.Delete(workspace.PluginRoot, recursive: true);
            return workspace;
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

        public void CopyProductivePlugins()
        {
            foreach (var pluginName in new[]
            {
                "Rezepte.Import.Plugins.Backup",
                "Rezepte.Import.Plugins.AIFoto",
                "Rezepte.Import.Plugins.AIUrl"
            })
            {
                var targetDirectory = Directory.CreateDirectory(Path.Combine(PluginRoot, pluginName)).FullName;
                CopyOutputAssembly($"{pluginName}.dll", targetDirectory);
                CopyOutputAssembly("Rezepte.Import.Abstractions.dll", targetDirectory);
            }
        }

        public void PublishExternalPlugin(string projectName)
        {
            var externalRoot = GetExternalPluginRepositoryRoot();
            var projectPath = Path.Combine(
                externalRoot,
                projectName,
                $"{projectName}.csproj");
            File.Exists(projectPath).Should().BeTrue("the external plugin project should exist");

            var targetDirectory = Directory.CreateDirectory(Path.Combine(PluginRoot, projectName)).FullName;
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "publish",
                    projectPath,
                    "-c",
                    "Debug",
                    "-o",
                    targetDirectory,
                    "--no-self-contained"
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            process.Should().NotBeNull();
            process!.WaitForExit();
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.ExitCode.Should().Be(0, output);

            var adjacentContract = Path.Combine(targetDirectory, "Rezepte.Import.Abstractions.dll");
            if (File.Exists(adjacentContract))
            {
                File.Delete(adjacentContract);
            }
        }

        public static string ReadChefkochFixture()
        {
            var fixturePath = Path.Combine(
                GetExternalPluginRepositoryRoot(),
                "tests",
                "fixtures",
                "chefkoch-recipe.html");
            return File.ReadAllText(fixturePath);
        }

        public static bool ExternalPluginRepositoryExists()
        {
            return Directory.Exists(GetExternalPluginRepositoryRoot());
        }

        private static string GetExternalPluginRepositoryRoot()
        {
            var configured = Environment.GetEnvironmentVariable("REZEPTE_EXTERNAL_PLUGINS_PATH");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(FindRepositoryRoot(), "external", "rezepte-import-plugins-private")
                : configured;
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Rezepte.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
