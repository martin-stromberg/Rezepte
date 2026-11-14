using System.Text.Json;

namespace Rezepte.Web.Services.Import.Plugins;

/// <summary>
/// Default <see cref="IPluginDirectoryRegistry"/> implementation managing the install state below
/// <c>{ContentRootPath}/plugins</c> in the file <c>.install-state.json</c>.
/// </summary>
/// <param name="environment">The environment parameter.</param>
/// <param name="logger">The logger parameter.</param>
/// <returns>The result.</returns>
public sealed class PluginDirectoryRegistry(IHostEnvironment environment, ILogger<PluginDirectoryRegistry> logger) : IPluginDirectoryRegistry
{
    private const string StateFileName = ".install-state.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly object _stateLock = new();

    private string PluginRoot => Path.GetFullPath(Path.Combine(environment.ContentRootPath, "plugins"));

    private string StateFilePath => Path.Combine(PluginRoot, StateFileName);

    /// <inheritdoc />
    public bool IsManagedRoot(string pluginRoot)
    {
        if (string.IsNullOrWhiteSpace(pluginRoot))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(pluginRoot), PluginRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public PluginInstallState LoadState()
    {
        lock (_stateLock)
        {
            try
            {
                if (!File.Exists(StateFilePath))
                {
                    return new PluginInstallState();
                }

                var state = JsonSerializer.Deserialize<PluginInstallState>(File.ReadAllText(StateFilePath), SerializerOptions);
                return state ?? new PluginInstallState();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read plugin install state file {StateFile}; continuing with an empty state", StateFilePath);
                return new PluginInstallState();
            }
        }
    }

    /// <inheritdoc />
    public void SaveState(PluginInstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_stateLock)
        {
            Directory.CreateDirectory(PluginRoot);
            var temporaryPath = $"{StateFilePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, SerializerOptions));
            File.Move(temporaryPath, StateFilePath, overwrite: true);
        }
    }

    /// <inheritdoc />
    public string CreateVersionedDirectoryName(string baseName, string releaseTag)
    {
        if (string.IsNullOrWhiteSpace(baseName))
        {
            throw new ArgumentException("The plugin base name must not be empty.", nameof(baseName));
        }

        if (string.IsNullOrWhiteSpace(releaseTag))
        {
            throw new ArgumentException("The release tag must not be empty.", nameof(releaseTag));
        }

        var sanitizedTag = SanitizeDirectoryNamePart(releaseTag);
        if (sanitizedTag.Length == 0)
        {
            throw new ArgumentException($"The release tag '{releaseTag}' cannot be used for a versioned plugin directory.", nameof(releaseTag));
        }

        return $"{baseName}@{sanitizedTag}";
    }

    /// <inheritdoc />
    public bool TryParseVersionedDirectoryName(string directoryName, out string baseName, out string releaseTag)
    {
        baseName = string.Empty;
        releaseTag = string.Empty;
        if (string.IsNullOrEmpty(directoryName))
        {
            return false;
        }

        var separatorIndex = directoryName.IndexOf('@', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == directoryName.Length - 1)
        {
            return false;
        }

        baseName = directoryName[..separatorIndex];
        releaseTag = directoryName[(separatorIndex + 1)..];
        return true;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetExcludedDirectoryNames(string pluginRoot, PluginInstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(pluginRoot))
        {
            return excluded;
        }

        foreach (var pendingDelete in state.PendingDeletes)
        {
            excluded.Add(pendingDelete);
        }

        var versionedByBaseName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var flatNames = new List<string>();
        foreach (var directoryName in Directory.EnumerateDirectories(pluginRoot).Select(Path.GetFileName))
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                continue;
            }

            if (TryParseVersionedDirectoryName(directoryName, out var baseName, out _))
            {
                if (!versionedByBaseName.TryGetValue(baseName, out var candidates))
                {
                    candidates = [];
                    versionedByBaseName[baseName] = candidates;
                }

                candidates.Add(directoryName);
            }
            else
            {
                flatNames.Add(directoryName);
            }
        }

        foreach (var (baseName, candidates) in versionedByBaseName)
        {
            var effective = SelectEffectiveDirectory(baseName, candidates, state);
            foreach (var candidate in candidates)
            {
                if (!string.Equals(candidate, effective, StringComparison.OrdinalIgnoreCase))
                {
                    excluded.Add(candidate);
                }
            }
        }

        foreach (var flatName in flatNames)
        {
            if (versionedByBaseName.ContainsKey(flatName))
            {
                excluded.Add(flatName);
            }
        }

        return excluded;
    }

    /// <inheritdoc />
    public string? SelectEffectiveDirectory(string baseName, IReadOnlyList<string> directoryNames, PluginInstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var entry = state.Installs.FirstOrDefault(e => string.Equals(e.BaseName, baseName, StringComparison.OrdinalIgnoreCase));
        if (entry is not null)
        {
            var registered = directoryNames.FirstOrDefault(n => string.Equals(n, entry.DirectoryName, StringComparison.OrdinalIgnoreCase));
            if (registered is not null)
            {
                return registered;
            }
        }

        return directoryNames
            .OrderByDescending(n => n, VersionedDirectoryNameComparer.Instance)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public void MarkPendingDeletion(PluginInstallState state, string directoryName)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(directoryName) || state.PendingDeletes.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        state.PendingDeletes.Add(directoryName);
    }

    /// <inheritdoc />
    public void CleanupMarkedDirectories(PluginInstallState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_stateLock)
        {
            var registeredNames = state.Installs
                .Select(e => e.DirectoryName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var remaining = new List<string>();
            foreach (var directoryName in state.PendingDeletes)
            {
                if (registeredNames.Contains(directoryName))
                {
                    logger.LogWarning("Skipping pending deletion of {PluginDirectory} because it is registered as an effective plugin installation", directoryName);
                    continue;
                }

                if (!TryGetPendingDeletionPath(directoryName, out var path))
                {
                    logger.LogWarning("Ignoring pending deletion entry {PendingDirectoryName} that does not name a direct child of the plugin root", directoryName);
                    continue;
                }

                if (!Directory.Exists(path))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogError(ex, "Could not delete plugin directory {PluginDirectory} marked for deletion", path);
                    remaining.Add(directoryName);
                }
            }

            state.PendingDeletes.Clear();
            state.PendingDeletes.AddRange(remaining);
        }

        SaveState(state);
    }

    private bool TryGetPendingDeletionPath(string directoryName, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            return false;
        }

        // Only a direct child segment of the plugin root may be deleted; entries such as
        // ".", ".." or nested/absolute paths would escape the managed root otherwise.
        // GetFullPath throws for malformed persisted entries (e.g. "a:b" or '\0'); treat
        // them as invalid instead of aborting cleanup for all pending deletions.
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Combine(PluginRoot, directoryName));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            logger.LogWarning("Ignoring pending deletion entry {PendingDirectoryName} that cannot be resolved to a path", directoryName);
            return false;
        }
        if (!string.Equals(Path.GetDirectoryName(fullPath), PluginRoot, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(fullPath), directoryName, StringComparison.Ordinal))
        {
            return false;
        }

        path = fullPath;
        return true;
    }

    private static string SanitizeDirectoryNamePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var characters = value.ToCharArray();
        for (var i = 0; i < characters.Length; i++)
        {
            if (characters[i] == '@' || invalid.Contains(characters[i]))
            {
                characters[i] = '-';
            }
        }

        return new string(characters);
    }

    private sealed class VersionedDirectoryNameComparer : IComparer<string>
    {
        public static readonly VersionedDirectoryNameComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            var versionX = TryParseTag(x);
            var versionY = TryParseTag(y);
            if (versionX is not null && versionY is not null)
            {
                var result = versionX.CompareTo(versionY);
                return result != 0 ? result : string.CompareOrdinal(x, y);
            }

            if (versionX is not null)
            {
                return 1;
            }

            if (versionY is not null)
            {
                return -1;
            }

            return string.CompareOrdinal(x, y);
        }

        private static Version? TryParseTag(string? directoryName)
        {
            if (directoryName is null)
            {
                return null;
            }

            var separatorIndex = directoryName.IndexOf('@', StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return null;
            }

            var tag = directoryName[(separatorIndex + 1)..].TrimStart('v', 'V');
            return Version.TryParse(tag, out var version) ? version : null;
        }
    }
}
