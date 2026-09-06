namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the package installer.
/// </summary>
/// <param name="environment">The environment parameter.</param>
/// <param name="pluginManager">The plugin manager parameter.</param>
/// <param name="logger">The logger parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginPackageInstaller(IHostEnvironment environment, IPluginManager pluginManager, ILogger<PluginPackageInstaller> logger) : IPluginPackageInstaller
{
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    /// <summary>
    /// installs the async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task InstallAsync(IReadOnlyList<string> pluginDirectories, CancellationToken ct = default)
        => await InstallWithReloadTrackingAsync(pluginDirectories, null, ct).ConfigureAwait(false);

    /// <summary>
    /// installs the with reload tracking async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="beforeReload">The before reload parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task InstallWithReloadTrackingAsync(IReadOnlyList<string> pluginDirectories, Func<CancellationToken, Task>? beforeReload, CancellationToken ct = default)
    {
        if (pluginDirectories.Count == 0)
        {
            throw new InvalidOperationException("No plugin directories were provided.");
        }

        await InstallLock.WaitAsync(ct).ConfigureAwait(false);
        var pluginRoot = Path.Combine(environment.ContentRootPath, "plugins");
        var backupRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup", Guid.NewGuid().ToString("N"));
        var installedTargets = new List<string>();
        var replacementCompleted = false;
        try
        {
            Directory.CreateDirectory(pluginRoot);
            Directory.CreateDirectory(backupRoot);

            await pluginManager.CoordinateReloadAsync(token =>
            {
                foreach (var sourceDirectory in pluginDirectories)
                {
                    token.ThrowIfCancellationRequested();
                    var target = Path.Combine(pluginRoot, Path.GetFileName(sourceDirectory));
                    var backup = Path.Combine(backupRoot, Path.GetFileName(sourceDirectory));
                    installedTargets.Add(target);
                    if (Directory.Exists(target))
                    {
                        CopyDirectory(target, backup);
                        Directory.Delete(target, recursive: true);
                    }

                    CopyDirectory(sourceDirectory, target);
                }

                replacementCompleted = true;
                if (beforeReload is not null)
                {
                    return beforeReload(token);
                }

                return Task.CompletedTask;
            }, ct).ConfigureAwait(false);
            logger.LogInformation("Installed {PluginDirectoryCount} plugin directorie(s)", pluginDirectories.Count);
        }
        catch (Exception ex)
        {
            await RollBackAsync(pluginRoot, backupRoot, installedTargets).ConfigureAwait(false);
            var status = replacementCompleted ? PluginSourceReleaseStatus.ReloadFailed : PluginSourceReleaseStatus.InstallFailed;
            throw new PluginPackageInstallException(status, ex.Message, ex);
        }
        finally
        {
            if (Directory.Exists(backupRoot))
            {
                TryDeleteDirectory(backupRoot, logger, "Could not remove plugin backup directory {PluginBackupRoot}", backupRoot);
            }

            InstallLock.Release();
        }

        async Task RollBackAsync(string root, string backups, IReadOnlyList<string> targets)
        {
            try
            {
                await pluginManager.CoordinateReloadAsync(_ =>
                {
                    foreach (var target in targets)
                    {
                        TryDeleteDirectory(target, logger, "Could not remove partially installed plugin directory {PluginTarget}", target);
                    }

                    if (Directory.Exists(backups))
                    {
                        foreach (var backup in Directory.EnumerateDirectories(backups))
                        {
                            var target = Path.Combine(root, Path.GetFileName(backup));
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

                    return Task.CompletedTask;
                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception reloadError)
            {
                logger.LogError(reloadError, "Could not reinitialize plugin manager after failed plugin installation");
            }
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
