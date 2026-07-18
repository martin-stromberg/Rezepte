using Microsoft.EntityFrameworkCore;
using Rezepte.Web.Data;
using Rezepte.Web.Entities;
using System.Reflection;
using System.Runtime.Loader;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginManager : IPluginManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<PluginManager> _logger;
    private readonly object _syncRoot = new();
    private IReadOnlyDictionary<string, ImportPluginDescriptor> _loadedPlugins = new Dictionary<string, ImportPluginDescriptor>();

    public PluginManager(IServiceScopeFactory scopeFactory, IHostEnvironment environment, ILogger<PluginManager> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var discovered = DiscoverPlugins();
        lock (_syncRoot)
        {
            _loadedPlugins = discovered
                .Where(p => p.Status == PluginStatus.Loaded && p.HandlerType is not null)
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, SelectPreferredDescriptor);
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RezepteDbContext>();
        await SynchronizeSettingsAsync(db, discovered, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PluginImportHandler>> GetActiveHandlersAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, ImportPluginDescriptor> loaded;
        lock (_syncRoot)
        {
            loaded = _loadedPlugins;
        }

        var db = serviceProvider.GetRequiredService<RezepteDbContext>();
        var settings = await db.PluginSettings
            .AsNoTracking()
            .Where(p => p.Enabled && p.Status == PluginStatus.Loaded)
            .OrderBy(p => p.OrderIndex)
            .ThenBy(p => p.DisplayName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var handlers = new List<PluginImportHandler>();
        foreach (var setting in settings)
        {
            if (!loaded.TryGetValue(setting.PluginId, out var plugin) || plugin.HandlerType is null)
            {
                continue;
            }

            try
            {
                var handler = (IImportHandler)ActivatorUtilities.CreateInstance(serviceProvider, plugin.HandlerType);
                handlers.Add(new PluginImportHandler(plugin, handler));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not create import handler for plugin {PluginId}", setting.PluginId);
                await MarkRuntimeFailureAsync(serviceProvider, setting.PluginId, ex.Message, ct).ConfigureAwait(false);
            }
        }

        return handlers;
    }

    public IReadOnlyList<ImportPluginDescriptor> DiscoverFromDirectory(string pluginRoot, bool unloadAfterDiscovery = false)
    {
        var fullRoot = Path.GetFullPath(pluginRoot);
        if (!Directory.Exists(fullRoot))
        {
            return [];
        }

        return DiscoverExternalPlugins([fullRoot], unloadAfterDiscovery).ToList();
    }

    private IReadOnlyList<ImportPluginDescriptor> DiscoverPlugins()
    {
        var plugins = new List<ImportPluginDescriptor>();
        plugins.AddRange(BuiltInImportPluginCatalog.GetPlugins());
        plugins.AddRange(DiscoverExternalPlugins(GetPluginRoots(), useCollectibleLoadContext: false));
        return plugins;
    }

    private IEnumerable<ImportPluginDescriptor> DiscoverExternalPlugins(IEnumerable<string> pluginRoots, bool useCollectibleLoadContext)
    {
        var dlls = pluginRoots
            .SelectMany(pluginRoot => Directory.EnumerateFiles(pluginRoot, "*.dll", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateDirectories(pluginRoot).SelectMany(GetPluginAssemblyCandidates)))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var discovered = new List<ImportPluginDescriptor>();
        foreach (var dll in dlls)
        {
            discovered.AddRange(DiscoverFromAssembly(dll, useCollectibleLoadContext));
        }

        return discovered;
    }

    private IEnumerable<string> GetPluginRoots()
    {
        var candidates = new[]
        {
            Path.Combine(_environment.ContentRootPath, "plugins"),
            Path.Combine(AppContext.BaseDirectory, "plugins")
        };

        return candidates
            .Where(Directory.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetPluginAssemblyCandidates(string directory)
    {
        var expectedAssembly = Path.Combine(directory, $"{Path.GetFileName(directory)}.dll");
        if (File.Exists(expectedAssembly))
        {
            return [expectedAssembly];
        }

        return Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
    }

    private IEnumerable<ImportPluginDescriptor> DiscoverFromAssembly(string path, bool useCollectibleLoadContext)
    {
        var result = new List<ImportPluginDescriptor>();
        PluginLoadContext? loadContext = null;
        try
        {
            loadContext = new PluginLoadContext(path, useCollectibleLoadContext);
            var assembly = loadContext.LoadFromAssemblyPath(path);
            var pluginTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IImportPlugin).IsAssignableFrom(t))
                .ToList();

            if (pluginTypes.Count == 0)
            {
                if (IsKnownDependencyAssembly(path))
                {
                    return result;
                }

                result.Add(FailedDescriptor(path, PluginStatus.Incompatible, "Assembly contains no IImportPlugin implementation."));
                return result;
            }

            foreach (var type in pluginTypes)
            {
                try
                {
                    var plugin = (IImportPlugin?)Activator.CreateInstance(type);
                    if (plugin is null || string.IsNullOrWhiteSpace(plugin.Id))
                    {
                        result.Add(FailedDescriptor(path, PluginStatus.Incompatible, $"Plugin type {type.FullName} has no stable id."));
                        continue;
                    }

                    if (!typeof(IImportHandler).IsAssignableFrom(plugin.HandlerType))
                    {
                        result.Add(new ImportPluginDescriptor(
                            plugin.Id,
                            plugin.DisplayName,
                            plugin.Description,
                            plugin.Version,
                            assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(path),
                            plugin.HandlerType.FullName ?? string.Empty,
                            null,
                            plugin.DefaultPriority,
                            PluginStatus.Incompatible,
                            "Configured handler type does not implement IImportHandler."));
                        continue;
                    }

                    result.Add(new ImportPluginDescriptor(
                        plugin.Id,
                        plugin.DisplayName,
                        plugin.Description,
                        plugin.Version,
                        assembly.GetName().Name ?? Path.GetFileNameWithoutExtension(path),
                        plugin.HandlerType.FullName ?? plugin.HandlerType.Name,
                        useCollectibleLoadContext ? null : plugin.HandlerType,
                        plugin.DefaultPriority,
                        PluginStatus.Loaded,
                        null));
                }
                catch (Exception ex)
                {
                    result.Add(FailedDescriptor(path, PluginStatus.LoadFailed, ex.Message));
                }
            }
        }
        catch (Exception ex)
        {
            result.Add(FailedDescriptor(path, PluginStatus.LoadFailed, ex.Message));
        }
        finally
        {
            if (useCollectibleLoadContext)
            {
                loadContext?.Unload();
            }
        }

        return result;
    }

    private static ImportPluginDescriptor FailedDescriptor(string path, string status, string error)
    {
        var id = $"{status.ToLowerInvariant()}:{Path.GetFileNameWithoutExtension(path)}";
        return new ImportPluginDescriptor(id, Path.GetFileName(path), null, "unknown", Path.GetFileName(path), string.Empty, null, 0, status, error);
    }

    private static bool IsKnownDependencyAssembly(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        return string.Equals(fileName, typeof(IImportPlugin).Assembly.GetName().Name, StringComparison.Ordinal);
    }

    private static async Task SynchronizeSettingsAsync(RezepteDbContext db, IReadOnlyList<ImportPluginDescriptor> discovered, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var existing = await db.PluginSettings.ToDictionaryAsync(p => p.PluginId, ct).ConfigureAwait(false);
        var nextOrder = existing.Count == 0 ? 0 : existing.Values.Max(p => p.OrderIndex) + 1;

        var plugins = discovered.GroupBy(p => p.Id).Select(g => SelectPreferredDescriptor(g));
        if (existing.Count == 0)
        {
            plugins = plugins
                .OrderBy(p => p.DefaultPriority)
                .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Id, StringComparer.Ordinal);
        }

        foreach (var plugin in plugins)
        {
            if (existing.TryGetValue(plugin.Id, out var setting))
            {
                setting.DisplayName = plugin.DisplayName;
                setting.Description = plugin.Description;
                setting.AssemblyName = plugin.AssemblyName;
                setting.TypeName = plugin.TypeName;
                setting.Status = plugin.Status;
                setting.Error = plugin.Error;
                setting.LastSeenAt = now;
            }
            else
            {
                db.PluginSettings.Add(new PluginSetting
                {
                    PluginId = plugin.Id,
                    DisplayName = plugin.DisplayName,
                    Description = plugin.Description,
                    AssemblyName = plugin.AssemblyName,
                    TypeName = plugin.TypeName,
                    Enabled = plugin.Status == PluginStatus.Loaded,
                    OrderIndex = nextOrder++,
                    Status = plugin.Status,
                    Error = plugin.Error,
                    DiscoveredAt = now,
                    LastSeenAt = now
                });
            }
        }

        var discoveredIds = discovered.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var setting in existing.Values.Where(p => !discoveredIds.Contains(p.PluginId)))
        {
            setting.Status = PluginStatus.Missing;
            setting.Error = "Plugin was not found during startup discovery.";
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static ImportPluginDescriptor SelectPreferredDescriptor(IEnumerable<ImportPluginDescriptor> descriptors)
    {
        return descriptors
            .OrderByDescending(p => p.AssemblyName.StartsWith("Rezepte.Import.Plugins.", StringComparison.Ordinal) && !IsPlaceholder(p))
            .ThenBy(p => p.AssemblyName == "Rezepte.Web")
            .First();
    }

    private static bool IsPlaceholder(ImportPluginDescriptor descriptor)
    {
        return descriptor.Version.Contains("placeholder", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task MarkRuntimeFailureAsync(IServiceProvider serviceProvider, string pluginId, string error, CancellationToken ct)
    {
        var db = serviceProvider.GetRequiredService<RezepteDbContext>();
        var setting = await db.PluginSettings.FirstOrDefaultAsync(p => p.PluginId == pluginId, ct).ConfigureAwait(false);
        if (setting is null)
        {
            return;
        }

        setting.Status = PluginStatus.RuntimeFailed;
        setting.Error = error;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath, bool isCollectible) : base(isCollectible)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == typeof(IImportPlugin).Assembly.GetName().Name)
            {
                return null;
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
        }
    }
}
