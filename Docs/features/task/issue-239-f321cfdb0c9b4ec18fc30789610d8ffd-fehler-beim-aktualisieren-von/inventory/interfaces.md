# Interfaces

Contracts im Plugin-Update-Pfad (Issue #239).

## `IPluginUpdateService`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginUpdateService.cs`

Implementiert von `PluginUpdateService` (scoped, `ServiceCollectionExtensions:219`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckForUpdatesAsync` | `ct` | `Task` | Startprüfung aller aktivierten, vertrauten Quellen (von `PluginUpdateHostedService` aufgerufen) |
| `CheckSourceForUpdatesAsync` | `sourceId`, `ct` | `Task<PluginUpdateCheckResult>` | Manuelle Prüfung einer Quelle (von `PluginSettingsService` aufgerufen) |

## `IGitHubReleaseClient`
Datei: `Rezepte.Web/Services/Import/Plugins/IGitHubReleaseClient.cs`

Implementiert von `GitHubReleaseClient` (typed `HttpClient`, `ServiceCollectionExtensions:211`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetLatestReleaseAsync` | `repository`, `personalAccessToken`, `ct` | `Task<GitHubReleaseInfo?>` | Neustes Release inkl. Assets; `null` bei 404 |
| `DownloadAssetAsync` | `asset`, `targetPath`, `personalAccessToken`, `ct` | `Task` | Lädt das Asset in `targetPath` (`File.Create`) — erwartet existierendes Elternverzeichnis |

## `IPluginPackageValidator`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginPackageValidator.cs`

Implementiert von `PluginPackageValidator` (scoped, `ServiceCollectionExtensions:217`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ValidateAsync` | `zipPath`, `ct` | `Task<PluginPackageValidationResult>` | Extrahiert ZIP in Temp-Verzeichnis, prüft Pfade/Top-Level-Regeln, Discovery-Probe |

## `IPluginPackageInstaller`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginPackageInstaller.cs`

Implementiert von `PluginPackageInstaller` (scoped, `ServiceCollectionExtensions:218`). `InstallWithReloadTrackingAsync` hat eine Default-Implementierung, die `InstallAsync` nach `beforeReload` aufruft — `PluginPackageInstaller` überschreibt sie mit der Reload-Koordination.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InstallAsync` | `pluginDirectories`, `options`, `ct` | `Task` | Versionierte Installation in `{ContentRootPath}/plugins` |
| `InstallWithReloadTrackingAsync` | `pluginDirectories`, `options`, `beforeReload`, `ct` | `Task` | Installation mit Status-Callback vor dem Reload |

## `IPluginDirectoryRegistry`
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginDirectoryRegistry.cs`

Implementiert von `PluginDirectoryRegistry` (singleton, `ServiceCollectionExtensions:206`). Relevanz für das Issue: verwaltet und beschreibt `{ContentRootPath}/plugins`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsManagedRoot` | `pluginRoot` | `bool` | Prüft, ob der Pfad der verwaltete Plugin-Root ist |
| `LoadState` | — | `PluginInstallState` | Liest `.install-state.json` (fail-open) |
| `SaveState` | `state` | `void` | Persistiert den State atomisch (legt `PluginRoot` an) |
| `CreateVersionedDirectoryName` | `baseName`, `releaseTag` | `string` | Erzeugt `<base>@<tag>`-Verzeichnisnamen |
| `TryParseVersionedDirectoryName` | `directoryName`, `out baseName`, `out releaseTag` | `bool` | Zerlegt versionierte Verzeichnisnamen |
| `GetExcludedDirectoryNames` | `pluginRoot`, `state` | `IReadOnlySet<string>` | Von Discovery auszuschließende Namen |
| `SelectEffectiveDirectory` | `baseName`, `directoryNames`, `state` | `string?` | Effektives Verzeichnis je Basisname |
| `MarkPendingDeletion` | `state`, `directoryName` | `void` | Markiert Verzeichnis zum Löschen |
| `CleanupMarkedDirectories` | `state` | `void` | Löscht markierte Verzeichnisse best-effort |

## `IPluginManager` (relevante Member)
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs`

Implementiert von `PluginManager` (singleton, `ServiceCollectionExtensions:205`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `InitializeAsync` | `ct` | `Task` | (Neu-)Initialisierung/Reload der Plugins |
| `DiscoverFromDirectory` | `pluginRoot`, `unloadAfterDiscovery` | `IReadOnlyList<ImportPluginDescriptor>` | Discovery-Probe aus einem Verzeichnis (Default-Impl. `[]`) |
| `CoordinateReloadAsync` | `replacePlugins`, `ct` | `Task` | Default-Impl.: `replacePlugins` dann `ReloadAsync`; `PluginPackageInstaller` hängt die Ersetzung + State-Persistierung in den Callback |
| `ReloadAsync` | `ct` | `Task` | Default-Impl.: `InitializeAsync` |

## `IPluginSettingsService` (relevanter Member)
Datei: `Rezepte.Web/Services/Import/Plugins/IPluginSettingsService.cs`

Implementiert von `PluginSettingsService` (scoped, `ServiceCollectionExtensions:220`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CheckSourceForUpdatesAsync` | `sourceId`, `ct` | `Task<PluginUpdateCheckResult>` | Admin-geprüfte Delegation an `IPluginUpdateService` |

**Befund:** Es existiert **kein** Interface/eine Abstraktion für die Bereitstellung temporärer Arbeitsverzeichnisse — die drei Produktiv-Fundstellen rufen `Path.GetTempPath()` direkt auf.
