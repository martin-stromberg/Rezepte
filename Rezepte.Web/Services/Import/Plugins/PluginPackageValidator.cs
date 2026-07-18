using System.IO.Compression;

namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginPackageValidator(IPluginManager pluginManager, ILogger<PluginPackageValidator> logger) : IPluginPackageValidator
{
    private static readonly HashSet<string> AllowedTopLevelFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md",
        "LICENSE",
        "LICENSE.txt",
        "CHANGELOG.md"
    };

    public Task<PluginPackageValidationResult> ValidateAsync(string zipPath, CancellationToken ct = default)
    {
        var extractRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-package", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractRoot);
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(entry.FullName))
                {
                    continue;
                }

                var normalizedName = entry.FullName.Replace('\\', '/');
                if (Path.IsPathRooted(normalizedName) || normalizedName.Contains("../", StringComparison.Ordinal) || normalizedName.StartsWith("..", StringComparison.Ordinal))
                {
                    return Task.FromResult(PluginPackageValidationResult.Failed("ZIP archive contains an unsafe path.", extractRoot));
                }

                var destination = Path.GetFullPath(Path.Combine(extractRoot, normalizedName));
                if (!destination.StartsWith(extractRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(destination, extractRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(PluginPackageValidationResult.Failed("ZIP archive escapes the extraction directory.", extractRoot));
                }

                if (normalizedName.EndsWith("/", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(destination);
                    continue;
                }

                var topLevel = normalizedName.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (topLevel is null)
                {
                    continue;
                }

                if (!normalizedName.Contains('/', StringComparison.Ordinal) && !AllowedTopLevelFiles.Contains(topLevel))
                {
                    return Task.FromResult(PluginPackageValidationResult.Failed($"Unexpected top-level file '{topLevel}'.", extractRoot));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: false);
            }

            var pluginDirectories = Directory.EnumerateDirectories(extractRoot)
                .Where(d => Directory.EnumerateFiles(d, "*.dll", SearchOption.TopDirectoryOnly).Any())
                .Select(Path.GetFullPath)
                .ToList();

            if (pluginDirectories.Count == 0)
            {
                return Task.FromResult(PluginPackageValidationResult.Failed("No plugin directory with an assembly was found.", extractRoot));
            }

            var discovered = pluginManager.DiscoverFromDirectory(extractRoot, unloadAfterDiscovery: true);
            if (discovered.Count == 0)
            {
                return Task.FromResult(PluginPackageValidationResult.Failed("Assembly discovery found no plugins.", extractRoot));
            }

            var failures = discovered.Where(p => p.Status != PluginStatus.Loaded).ToList();
            if (failures.Count > 0)
            {
                var error = string.Join("; ", failures.Select(p => $"{p.AssemblyName}: {p.Error ?? p.Status}"));
                return Task.FromResult(PluginPackageValidationResult.Failed(error, extractRoot));
            }

            logger.LogInformation("Validated plugin package {PluginPackage} with {PluginCount} plugin(s)", Path.GetFileName(zipPath), discovered.Count);
            return Task.FromResult(new PluginPackageValidationResult(true, null, extractRoot, pluginDirectories, discovered));
        }
        catch (InvalidDataException ex)
        {
            return Task.FromResult(PluginPackageValidationResult.Failed($"Invalid ZIP archive: {ex.Message}", extractRoot));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(PluginPackageValidationResult.Failed($"Could not extract ZIP archive: {ex.Message}", extractRoot));
        }
    }
}
