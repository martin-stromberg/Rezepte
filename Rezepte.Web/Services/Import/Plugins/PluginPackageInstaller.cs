namespace Rezepte.Web.Services.Import.Plugins;

public sealed class PluginPackageInstaller(IHostEnvironment environment, IPluginManager pluginManager, ILogger<PluginPackageInstaller> logger) : IPluginPackageInstaller
{
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    public async Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default)
    {
        if (pluginDirectories.Count == 0)
        {
            throw new InvalidOperationException("No plugin directories were provided.");
        }

        await InstallLock.WaitAsync(ct).ConfigureAwait(false);
        var pluginRoot = Path.Combine(environment.ContentRootPath, "plugins");
        var backupRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup", Guid.NewGuid().ToString("N"));
        var installedTargets = new List<string>();
        try
        {
            Directory.CreateDirectory(pluginRoot);
            Directory.CreateDirectory(backupRoot);

            foreach (var sourceDirectory in pluginDirectories)
            {
                ct.ThrowIfCancellationRequested();
                var target = Path.Combine(pluginRoot, Path.GetFileName(sourceDirectory));
                var backup = Path.Combine(backupRoot, Path.GetFileName(sourceDirectory));
                if (Directory.Exists(target))
                {
                    CopyDirectory(target, backup);
                    Directory.Delete(target, recursive: true);
                }

                CopyDirectory(sourceDirectory, target);
                installedTargets.Add(target);
            }

            await pluginManager.InitializeAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Installed {PluginDirectoryCount} plugin directorie(s)", pluginDirectories.Count);
        }
        catch
        {
            foreach (var target in installedTargets)
            {
                TryDeleteDirectory(target, logger, "Could not remove partially installed plugin directory {PluginTarget}", target);
            }

            if (Directory.Exists(backupRoot))
            {
                foreach (var backup in Directory.EnumerateDirectories(backupRoot))
                {
                    var target = Path.Combine(pluginRoot, Path.GetFileName(backup));
                    try
                    {
                        TryDeleteDirectory(target, logger, "Could not remove plugin directory {PluginTarget} before restoring backup", target);
                        CopyDirectory(backup, target);
                    }
                    catch (Exception rollbackError)
                    {
                        logger.LogError(rollbackError, "Could not restore plugin backup {PluginBackup} to {PluginTarget}", backup, target);
                    }
                }
            }

            try
            {
                await pluginManager.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception reloadError)
            {
                logger.LogError(reloadError, "Could not reinitialize plugin manager after failed plugin installation");
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(backupRoot))
            {
                TryDeleteDirectory(backupRoot, logger, "Could not remove plugin backup directory {PluginBackupRoot}", backupRoot);
            }

            InstallLock.Release();
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string directory, ILogger logger, string message, params object[] args)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, message, args);
        }
    }
}
