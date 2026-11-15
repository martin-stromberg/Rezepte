namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// plugins the package installer.
/// </summary>
/// <param name="environment">The environment parameter.</param>
/// <param name="pluginManager">The plugin manager parameter.</param>
/// <param name="directoryRegistry">The directory registry parameter.</param>
/// <param name="logger">The logger parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginPackageInstaller(IHostEnvironment environment, IPluginManager pluginManager, IPluginDirectoryRegistry directoryRegistry, ILogger<PluginPackageInstaller> logger) : IPluginPackageInstaller
{
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    /// <summary>
    /// installs the async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="options">The options parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task InstallAsync(IReadOnlyList<string> pluginDirectories, PluginPackageInstallOptions options, CancellationToken ct = default)
        => await InstallWithReloadTrackingAsync(pluginDirectories, options, null, ct).ConfigureAwait(false);

    /// <summary>
    /// installs the with reload tracking async.
    /// </summary>
    /// <param name="pluginDirectories">The plugin directories parameter.</param>
    /// <param name="options">The options parameter.</param>
    /// <param name="beforeReload">The before reload parameter.</param>
    /// <param name="ct">The ct parameter.</param>
    public async Task InstallWithReloadTrackingAsync(IReadOnlyList<string> pluginDirectories, PluginPackageInstallOptions options, Func<CancellationToken, Task>? beforeReload, CancellationToken ct = default)
    {
        if (pluginDirectories.Count == 0)
        {
            throw new InvalidOperationException("No plugin directories were provided.");
        }

        ArgumentNullException.ThrowIfNull(options);

        var installs = pluginDirectories.Select(sourceDirectory =>
        {
            var baseName = Path.GetFileName(sourceDirectory);
            if (string.IsNullOrEmpty(baseName) || baseName.Contains('@', StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"The plugin directory name '{baseName}' cannot be used for a versioned installation.");
            }

            return new PendingInstall(sourceDirectory, baseName, directoryRegistry.CreateVersionedDirectoryName(baseName, options.ReleaseTag));
        }).ToList();

        await InstallLock.WaitAsync(ct).ConfigureAwait(false);
        var pluginRoot = Path.Combine(environment.ContentRootPath, "plugins");
        var replacementCompleted = false;
        PluginInstallState? stateSnapshot = null;
        InstallOperation? operation = null;
        try
        {
            Directory.CreateDirectory(pluginRoot);
            var state = directoryRegistry.LoadState();
            stateSnapshot = directoryRegistry.LoadState();
            var activeOperation = new InstallOperation(state, installs, pluginRoot);
            operation = activeOperation;

            await pluginManager.CoordinateReloadAsync(token =>
            {
                ReplaceDirectories(activeOperation, token);
                UpdateInstallState(activeOperation, options);
                MarkDroppedSourceEntries(activeOperation, options);
                directoryRegistry.SaveState(activeOperation.State);
                replacementCompleted = true;
                return beforeReload?.Invoke(token) ?? Task.CompletedTask;
            }, ct).ConfigureAwait(false);
            logger.LogInformation("Installed {PluginDirectoryCount} plugin directorie(s)", pluginDirectories.Count);
        }
        catch (Exception ex)
        {
            await RollBackAsync(stateSnapshot, pluginRoot, operation).ConfigureAwait(false);
            var status = replacementCompleted ? PluginSourceReleaseStatus.ReloadFailed : PluginSourceReleaseStatus.InstallFailed;
            throw new PluginPackageInstallException(status, ex.Message, ex);
        }
        finally
        {
            if (operation is not null)
            {
                foreach (var backup in operation.Backups)
                {
                    TryDeleteDirectory(backup.BackupPath, logger, "Could not remove the plugin install backup {PluginBackup}", backup.BackupPath);
                }
            }

            InstallLock.Release();
        }
    }

    private void ReplaceDirectories(InstallOperation operation, CancellationToken token)
    {
        directoryRegistry.CleanupMarkedDirectories(operation.State);

        foreach (var install in operation.Installs)
        {
            token.ThrowIfCancellationRequested();
            var target = Path.Combine(operation.PluginRoot, install.TargetName);
            operation.NewTargetNames.Add(install.TargetName);
            if (Directory.Exists(target))
            {
                // A same-tag reinstall replaces a directory that stays registered as the effective
                // installation. Loaded assemblies can block renaming or deleting it, so its content
                // is backed up by copy to make the original installation restorable on rollback.
                var isRegisteredInstall = operation.State.Installs.Any(e => string.Equals(e.DirectoryName, install.TargetName, StringComparison.OrdinalIgnoreCase));
                if (isRegisteredInstall)
                {
                    operation.Backups.Add(BackupDirectory(target));
                }

                try
                {
                    Directory.Delete(target, recursive: true);
                }
                catch (Exception deleteError) when (deleteError is IOException or UnauthorizedAccessException)
                {
                    // A registered directory is restored from its backup during rollback; marking it
                    // as pending would persist the active installation as deletion candidate until then.
                    if (!isRegisteredInstall)
                    {
                        directoryRegistry.MarkPendingDeletion(operation.State, install.TargetName);
                        directoryRegistry.SaveState(operation.State);
                    }

                    throw;
                }
            }

            CopyDirectory(install.SourceDirectory, target);
        }
    }

    private void UpdateInstallState(InstallOperation operation, PluginPackageInstallOptions options)
    {
        var managedNames = Directory.EnumerateDirectories(operation.PluginRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
        var newTargets = new HashSet<string>(operation.Installs.Select(i => i.TargetName), StringComparer.OrdinalIgnoreCase);

        foreach (var install in operation.Installs)
        {
            foreach (var directoryName in managedNames)
            {
                if (!newTargets.Contains(directoryName)
                    && directoryRegistry.TryParseVersionedDirectoryName(directoryName, out var baseName, out _)
                    && string.Equals(baseName, install.BaseName, StringComparison.OrdinalIgnoreCase))
                {
                    directoryRegistry.MarkPendingDeletion(operation.State, directoryName);
                }
            }

            var replaced = operation.State.Installs.FirstOrDefault(e => string.Equals(e.BaseName, install.BaseName, StringComparison.OrdinalIgnoreCase));
            if (replaced is not null)
            {
                if (!newTargets.Contains(replaced.DirectoryName))
                {
                    directoryRegistry.MarkPendingDeletion(operation.State, replaced.DirectoryName);
                }

                operation.State.Installs.Remove(replaced);
            }

            operation.State.Installs.Add(new PluginInstallEntry
            {
                BaseName = install.BaseName,
                DirectoryName = install.TargetName,
                ReleaseTag = options.ReleaseTag,
                SourceKey = options.SourceKey
            });
        }
    }

    private void MarkDroppedSourceEntries(InstallOperation operation, PluginPackageInstallOptions options)
    {
        if (options.SourceKey is null)
        {
            return;
        }

        var incomingBaseNames = new HashSet<string>(operation.Installs.Select(i => i.BaseName), StringComparer.OrdinalIgnoreCase);
        var dropped = operation.State.Installs
            .Where(e => string.Equals(e.SourceKey, options.SourceKey, StringComparison.Ordinal)
                     && !incomingBaseNames.Contains(e.BaseName))
            .ToList();
        foreach (var entry in dropped)
        {
            directoryRegistry.MarkPendingDeletion(operation.State, entry.DirectoryName);
            operation.State.Installs.Remove(entry);
        }
    }

    private async Task RollBackAsync(PluginInstallState? snapshot, string pluginRoot, InstallOperation? operation)
    {
        var targetNames = (IReadOnlyList<string>?)operation?.NewTargetNames ?? [];
        var backups = (IReadOnlyList<InstallBackup>?)operation?.Backups ?? [];
        try
        {
            await pluginManager.CoordinateReloadAsync(_ =>
            {
                var rollbackState = snapshot ?? directoryRegistry.LoadState();
                foreach (var backup in backups)
                {
                    RestoreBackup(backup);
                }

                var registeredNames = rollbackState.Installs
                    .Select(e => e.DirectoryName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var targetName in targetNames)
                {
                    if (!registeredNames.Contains(targetName))
                    {
                        directoryRegistry.MarkPendingDeletion(rollbackState, targetName);
                    }
                }

                directoryRegistry.SaveState(rollbackState);
                foreach (var targetName in targetNames)
                {
                    if (registeredNames.Contains(targetName))
                    {
                        continue;
                    }

                    TryDeleteDirectory(Path.Combine(pluginRoot, targetName), logger, "Could not remove partially installed plugin directory {PluginTarget}", Path.Combine(pluginRoot, targetName));
                }

                return Task.CompletedTask;
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception reloadError)
        {
            logger.LogError(reloadError, "Could not reinitialize plugin manager after failed plugin installation");
        }
    }

    private InstallBackup BackupDirectory(string target)
    {
        var backupPath = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup", Guid.NewGuid().ToString("N"));
        try
        {
            CopyDirectory(target, backupPath);
            return new InstallBackup(target, backupPath);
        }
        catch
        {
            // The record is only returned after a complete copy, so the caller never registers
            // a partial backup; remove the leftover temp directory best-effort here instead.
            TryDeleteDirectory(backupPath, logger, "Could not remove the partial plugin install backup {PluginBackup}", backupPath);
            throw;
        }
    }

    private void RestoreBackup(InstallBackup backup)
    {
        TryDeleteDirectory(backup.TargetPath, logger, "Could not remove the partially installed plugin directory {PluginTarget}", backup.TargetPath);
        try
        {
            CopyDirectory(backup.BackupPath, backup.TargetPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogError(ex, "Could not fully restore the plugin directory {PluginTarget} from its backup", backup.TargetPath);
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

    private sealed record PendingInstall(string SourceDirectory, string BaseName, string TargetName);

    private sealed record InstallBackup(string TargetPath, string BackupPath);

    private sealed class InstallOperation(PluginInstallState state, IReadOnlyList<PendingInstall> installs, string pluginRoot)
    {
        public PluginInstallState State { get; } = state;

        public IReadOnlyList<PendingInstall> Installs { get; } = installs;

        public string PluginRoot { get; } = pluginRoot;

        public List<string> NewTargetNames { get; } = [];

        public List<InstallBackup> Backups { get; } = [];
    }
}
