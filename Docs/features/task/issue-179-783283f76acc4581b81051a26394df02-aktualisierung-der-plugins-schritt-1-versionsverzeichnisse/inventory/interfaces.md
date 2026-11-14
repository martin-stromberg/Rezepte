# Interfaces — Bestandsaufnahme

## `IPluginPackageInstaller`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginPackageInstaller.cs`

Implementiert von `PluginPackageInstaller` (`Scoped`); in Tests gefaket (`FakePackageInstaller`, `FailingPackageInstaller` in `PluginUpdateServiceTests`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InstallAsync` | `IReadOnlyList<string> pluginDirectories`, `CancellationToken ct = default` | `Task` | Installiert die validierten Plugin-Verzeichnisse unter `plugins/` (aktuell in-place). |
| `InstallWithReloadTrackingAsync` | `IReadOnlyList<string> pluginDirectories`, `Func<CancellationToken, Task> beforeReload`, `CancellationToken ct = default` | `Task` | **Default-Interface-Implementierung:** ruft `beforeReload` dann `InstallAsync`. `PluginPackageInstaller` überschreibt sie (Signatur dort mit nullable `Func<...>?`). Wird von `PluginUpdateService` genutzt, um den `Reloading`-Status zu persistieren. |

**Relevant für die Anforderung:** Kein Parameter für Release-Tag/Versionsangabe — die für Versionsverzeichnisnamen nötige Signaturerweiterung betrifft beide Methoden bzw. ein einzuführendes Descriptor-Objekt. Test-Fakes implementieren nur `InstallAsync` (die Default-Methode fängt die Signaturänderung nur teilweise ab).

## `IPluginManager`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs`

Implementiert von `PluginManager` (`Singleton`); in Tests gestubbt (`FailingPluginManager`, `SuccessfulPluginManager` in `PluginPackageInstallerTests`; `FakePluginManager` in `PluginSettingsServiceTests`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InitializeAsync` | `CancellationToken ct = default` | `Task` | Initiale Discovery beim Start (via `PluginStartupService`). |
| `DiscoverFromDirectory` | `string pluginRoot`, `bool unloadAfterDiscovery = false` | `IReadOnlyList<ImportPluginDescriptor>` | Default-Implementierung `[]`; `PluginManager` liefert echte Discovery (Paketvalidierung). |
| `GetActiveHandlersAsync` | `IServiceProvider`, `CancellationToken ct = default` | `Task<IReadOnlyList<PluginImportHandler>>` | Aktive Handler für `Enabled && Loaded`-Plugins. |
| `AcquireActiveHandlersAsync` | `IServiceProvider`, `CancellationToken ct = default` | `Task<PluginHandlerLease>` | Default-Implementierung ohne Lock; `PluginManager` hält `_lifecycleLock` bis Lease-Dispose. Aufgerufen von `ImportService` und `ImportOrchestrator`. |
| `ReloadAsync` | `CancellationToken ct = default` | `Task` | Default: `InitializeAsync(ct)`. |
| `CoordinateReloadAsync` | `Func<CancellationToken, Task> replacePlugins`, `CancellationToken ct = default` | `Task` | Default: `replacePlugins` dann `ReloadAsync`. `PluginManager`: Lock + leeren + Callback + `InitializeCoreAsync`. **Erweiterungspunkt für Bereinigung markierter Verzeichnisse beim Austausch.** |
| `GetPluginsUsabilityAsync` | `IServiceProvider`, `CancellationToken ct = default` | `Task<IReadOnlyDictionary<string, PluginUsabilityResult>>` | Usability-Ergebnisse je Plugin-`Id` (Default: leeres Dictionary). |

## `IPluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginUpdateService.cs`

Implementiert von `PluginUpdateService` (`Scoped`); einzige Methode `CheckForUpdatesAsync(CancellationToken ct = default)` → `Task`. Aufgerufen von `PluginUpdateHostedService`. Kein manueller Einzelquellen-Trigger (Scope-Ausschluss der Anforderung).

## `IPluginPackageValidator`

Datei: `Rezepte.Web/Services/Import/Plugins/IPluginPackageValidator.cs`

`ValidateAsync(string zipPath, CancellationToken ct = default)` → `Task<PluginPackageValidationResult>`. Implementiert von `PluginPackageValidator`; in Tests `FakePackageValidator`.

## `ISystemSecretStore` / `IGitHubReleaseClient` (angrenzend)

- `ISystemSecretStore` (`ISystemSecretStore.cs`): `StoreAsync`, `GetAsync`, `DeleteAsync` — PAT-Verwaltung, vom Versionsverzeichnis-Modell unberührt.
- `IGitHubReleaseClient` (`IGitHubReleaseClient.cs`): `GetLatestReleaseAsync(GitHubRepository, string? pat, ct)` → `GitHubReleaseInfo?`; `DownloadAssetAsync(GitHubReleaseAsset, string targetPath, string? pat, ct)`. Liefert `TagName`, aus dem `ReleaseTag` entsteht.
