# Interfaces — Bestandsaufnahme

Alle Interfaces liegen in `Rezepte.Web/Services/Import/Plugins/`.

## `IPluginUpdateService`

Datei: `IPluginUpdateService.cs` — implementiert von `PluginUpdateService` (`Scoped`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckForUpdatesAsync` | `CancellationToken ct = default` | `Task` | Prüft **alle** aktivierten, vertrauensbestätigten Quellen einmalig (Start-Check). Keine Einzelquellen-Variante vorhanden. |

## `IPluginSettingsService`

Datei: `IPluginSettingsService.cs` — implementiert von `PluginSettingsService` (`Scoped`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetPluginsAsync` | `CancellationToken ct = default` | `Task<IReadOnlyList<PluginSettingsItem>>` | Pluginliste inkl. `Usability` für die Admin-UI |
| `GetSourcesAsync` | `CancellationToken ct = default` | `Task<IReadOnlyList<PluginSourceSettingsItem>>` | Quellenliste inkl. Statusfelder (`EnsureAdmin`) |
| `SaveSourceAsync` | `PluginSourceSaveRequest request`, `CancellationToken ct` | `Task` | Quelle anlegen/aktualisieren inkl. PAT-Handling (`EnsureAdmin`) |
| `SetSourceEnabledAsync` | `string sourceId`, `bool enabled`, `CancellationToken ct` | `Task` | Quelle aktivieren/deaktivieren (`EnsureAdmin`) |
| `DeleteSourceAsync` | `string sourceId`, `CancellationToken ct` | `Task` | Quelle + Secret löschen (`EnsureAdmin`) |
| `SetEnabledAsync` | `string pluginId`, `bool enabled`, `CancellationToken ct` | `Task` | Plugin aktivieren/deaktivieren |
| `MoveAsync` | `string pluginId`, `int direction`, `CancellationToken ct` | `Task` | Plugin in der Reihenfolge verschieben |

Nicht vorhanden: Löschmethode für `PluginSetting`-Datensätze (z. B. `Missing`-Plugins) und Delegation an einen manuellen Quellen-Check.

## `IPluginPackageInstaller`

Datei: `IPluginPackageInstaller.cs` — implementiert von `PluginPackageInstaller` (`Scoped`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InstallAsync` | `IReadOnlyList<string> pluginDirectories`, `CancellationToken ct` | `Task` | Installiert Pluginverzeichnisse unter `plugins/` (In-place) |
| `InstallWithReloadTrackingAsync` | `IReadOnlyList<string> pluginDirectories`, `Func<CancellationToken, Task> beforeReload`, `CancellationToken ct` | `Task` | Default-Interface-Methode: `beforeReload` vor `InstallAsync`; `PluginPackageInstaller` überschreibt sie, um den Callback nach dem Dateiaustausch innerhalb von `CoordinateReloadAsync` aufzurufen |

## `IPluginManager`

Datei: `IPluginManager.cs` — implementiert von `PluginManager` (`Singleton`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InitializeAsync` | `CancellationToken ct = default` | `Task` | Erst-Discovery beim Start |
| `DiscoverFromDirectory` | `string pluginRoot`, `bool unloadAfterDiscovery = false` | `IReadOnlyList<ImportPluginDescriptor>` | Default-Methode `[]`; Discovery eines beliebigen Verzeichnisses (Validierung nutzt `unloadAfterDiscovery: true`) |
| `GetActiveHandlersAsync` | `IServiceProvider serviceProvider`, `CancellationToken ct` | `Task<IReadOnlyList<PluginImportHandler>>` | Frische Handler-Instanzen für aktivierte, `Loaded`-Plugins |
| `AcquireActiveHandlersAsync` | `IServiceProvider serviceProvider`, `CancellationToken ct` | `Task<PluginHandlerLease>` | Default-Methode; Lease hält `_lifecycleLock` bis Dispose |
| `ReloadAsync` | `CancellationToken ct` | `Task` | Default-Methode → `InitializeAsync` |
| `CoordinateReloadAsync` | `Func<CancellationToken, Task> replacePlugins`, `CancellationToken ct` | `Task` | Default-Methode: `replacePlugins` + `ReloadAsync`; `PluginManager` koordiniert Dateiaustausch und Neudiscovery unter einem Lock |
| `GetPluginsUsabilityAsync` | `IServiceProvider serviceProvider`, `CancellationToken ct` | `Task<IReadOnlyDictionary<string, PluginUsabilityResult>>` | Default-Methode leeres Dictionary; Usability-Prüfung je geladenem Plugin |

## `IGitHubReleaseClient`

Datei: `IGitHubReleaseClient.cs` — implementiert von `GitHubReleaseClient` (`HttpClient`-Registrierung).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetLatestReleaseAsync` | `GitHubRepository repository`, `string? personalAccessToken`, `CancellationToken ct` | `Task<GitHubReleaseInfo?>` | Neuestes Release inkl. Assets |
| `DownloadAssetAsync` | `GitHubReleaseAsset asset`, `string targetPath`, `string? personalAccessToken`, `CancellationToken ct` | `Task` | Asset herunterladen |

## `ISystemSecretStore`

Datei: `ISystemSecretStore.cs` — implementiert von `DataProtectionSystemSecretStore` (`Scoped`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StoreAsync` | `string name`, `string secret`, `CancellationToken ct` | `Task` | Secret ablegen (PAT) |
| `GetAsync` | `string name`, `CancellationToken ct` | `Task<string?>` | Secret lesen |
| `DeleteAsync` | `string name`, `CancellationToken ct` | `Task` | Secret löschen |

## `IPluginPackageValidator`

Datei: `IPluginPackageValidator.cs` — implementiert von `PluginPackageValidator` (`Scoped`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ValidateAsync` | `string zipPath`, `CancellationToken ct` | `Task<PluginPackageValidationResult>` | ZIP entpacken, Struktur prüfen, Discovery-Probe |
