# Logik — Bestandsaufnahme

## `PluginPackageInstaller`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs`

Implementiert `IPluginPackageInstaller`; `Scoped` (`ServiceCollectionExtensions.cs:216`). Serialisiert Installationen über `private static readonly SemaphoreSlim InstallLock` (Zeile 12). Aufgerufen von `PluginUpdateService.DownloadValidateInstallAsync` (`PluginUpdateService.cs:150`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InstallAsync(IReadOnlyList<string>, CancellationToken)` | `public` | Delegiert an `InstallWithReloadTrackingAsync` ohne `beforeReload`. |
| `InstallWithReloadTrackingAsync(IReadOnlyList<string>, Func<CancellationToken, Task>?, CancellationToken)` | `public` | Wirft `InvalidOperationException` bei leerer Liste. Unter `InstallLock` + `pluginManager.CoordinateReloadAsync`: **In-place-Ersetzung** — je Quellverzeichnis Ziel `plugins/{Path.GetFileName(sourceDirectory)}`; vorhandenes Ziel wird nach `backupRoot` (`%TEMP%/rezepte-plugin-backup/{guid}`) kopiert, gelöscht und neu befüllt (`CopyDirectory`). Danach `beforeReload`-Callback; `CoordinateReloadAsync` reinitialisiert anschließend. Bei Fehler: `RollBackAsync` + `PluginPackageInstallException` mit `InstallFailed` (vor `replacementCompleted`) bzw. `ReloadFailed`. |
| `RollBackAsync(root, backups, targets)` | `private` (lokal) | Erneutes `CoordinateReloadAsync`: löscht teilinstallierte Ziele best-effort (`TryDeleteDirectory`) und stellt jedes Backup-Verzeichnis wieder her (`CopyDirectory`). Fehler nur Error-Log. |
| `CopyDirectory(source, target)` | `private static` | Rekursive Verzeichniskopie, `File.Copy(..., overwrite: true)`. |
| `TryDeleteDirectory(directory, logger, message, args)` | `private static` | Best-effort-Löschung; `IOException`/`UnauthorizedAccessException` → Error-Log, kein Rethrow, **keine Markierung**. |

**Relevant für die Anforderung:**
- Zielverzeichnisname = `Path.GetFileName(sourceDirectory)` — kein Versionsbezug, keine `ReleaseTag`-Kenntnis.
- Scheitert `Directory.Delete(target)` im Austausch-Callback (Zeile 56), bricht die Ausnahme den Lauf ab → `InstallFailed` + Rollback; ein „markieren und weiter"-Pfad existiert nicht.
- `backupRoot` unter `Path.GetTempPath()` ist die einzige Rücksicherung.
- Der `finally`-Block löscht `backupRoot` best-effort und gibt `InstallLock` frei.

## `PluginManager`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`

Implementiert `IPluginManager`; `Singleton` (`ServiceCollectionExtensions.cs:204`). Hält `_loadedPlugins`; Lifecycle-Serialisierung über `_lifecycleLock` (`SemaphoreSlim`) + `_syncRoot`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync(CancellationToken)` | `public` | Delegiert an `ReloadAsync`. Aufgerufen von `PluginStartupService.StartAsync`. |
| `ReloadAsync(CancellationToken)` | `public` | Unter `_lifecycleLock`: `_loadedPlugins` leeren, `InitializeCoreAsync`. |
| `CoordinateReloadAsync(Func<CancellationToken, Task>, CancellationToken)` | `public` | **Zentraler Austausch-Hook:** unter `_lifecycleLock` `_loadedPlugins` leeren → `replacePlugins(ct)` (Dateiaustausch des Installers) → `InitializeCoreAsync`. Imports sind währenddessen gesperrt, da `AcquireActiveHandlersAsync` dasselbe Lock hält. Wird von `PluginPackageInstaller` (Austausch + Rollback) genutzt. |
| `InitializeCoreAsync(CancellationToken)` | `private` | `DiscoverPlugins` → `_loadedPlugins` aus `Loaded`-Deskriptoren (Dedup je `Id` via `SelectPreferredDescriptor`) → `SynchronizeSettingsAsync`. **Keine Bereinigung markierter/verwaister Verzeichnisse.** |
| `GetActiveHandlersAsync(IServiceProvider, CancellationToken)` | `public` | Lease öffnen+schließen, `Handlers` zurück. Aufgerufen von `ImportService`/`ImportOrchestrator` (über `AcquireActiveHandlersAsync`). |
| `AcquireActiveHandlersAsync(IServiceProvider, CancellationToken)` | `public` | Hält `_lifecycleLock` bis `PluginHandlerLease` disposed; erzeugt `IImportHandler`-Instanzen für `PluginSettings` mit `Enabled && Status == Loaded` (sortiert `OrderIndex`, `DisplayName`); Instanziierungsfehler → `MarkRuntimeFailureAsync`. |
| `GetPluginsUsabilityAsync(IServiceProvider, CancellationToken)` | `public` | `CheckUsabilityAsync` je geladenem Plugin; Exceptions → `IsUsable = false`. Aufgerufen von `PluginSettingsService`. |
| `DiscoverFromDirectory(string, bool unloadAfterDiscovery)` | `public` | Discovery eines freien Verzeichnisses über `DiscoverExternalPlugins([fullRoot], ...)`; collectible Kontext für Paketvalidierung (`PluginPackageValidator`). |
| `DiscoverPlugins()` | `private` | `BuiltInImportPluginCatalog.GetPlugins()` (aktuell `[]`) + `DiscoverExternalPlugins(GetPluginRoots(), useCollectibleLoadContext: false)`. |
| `DiscoverExternalPlugins(IEnumerable<string>, bool)` | `private` | DLL-Menge = `*.dll` direkt in den Roots **plus** `GetPluginAssemblyCandidates` je **direktem** Unterverzeichnis (`Directory.EnumerateDirectories(pluginRoot)` — genau eine Ebene). Dedup ordinal-ignore-case. **Kein Ausschluss markierter Verzeichnisse, keine Tiefensuche.** |
| `GetPluginRoots()` | `private` | `{ContentRootPath}/plugins` + `{AppContext.BaseDirectory}/plugins` (existierende, dedupliziert). |
| `GetPluginAssemblyCandidates(directory)` | `private static` | Bevorzugt `{Verzeichnisname}.dll`, sonst alle `*.dll` im Verzeichnis (TopDirectoryOnly). Bei zwei verschachtelten Ebenen (`plugins/<Name>/<Version>/`) würde `<Name>.dll` nicht gefunden und auch keine DLL — verschachtelte Versionsverzeichnisse erfordern Anpassung. |
| `DiscoverFromAssembly(path, bool)` | `private` | `PluginLoadContext`; findet `IImportPlugin`-Implementierungen; Deskriptor-Status `Loaded`/`Incompatible`/`LoadFailed`; bekannte Vertrags-Assembly (`Rezepte.Import.Abstractions`) ohne Plugin wird ignoriert (`IsKnownDependencyAssembly`). |
| `FailedDescriptor(path, status, error)` | `private static` | Synthetischer Deskriptor `"{status}:{assemblyname}"`. |
| `SynchronizeSettingsAsync(db, discovered, ct)` | `private static` | Aktualisiert/erstellt `PluginSetting`; nicht mehr entdeckte Einträge → `Status = Missing`, `Error = "Plugin was not found during startup discovery."` (Zeilen 439-444) — deckt die geforderte `Missing`-Markierung entfallener Plugins ab. |
| `SelectPreferredDescriptor(descriptors)` | `private static` | Bei doppelten `Id`s: bevorzugt `Rezepte.Import.Plugins.*`-Assemblies (nicht-Platzhalter), dann `Rezepte.Web`. **Keine versions- oder verzeichnisbasierte Auswahl.** |
| `MarkRuntimeFailureAsync(...)` | `private static` | `Status = RuntimeFailed` + `Error`. |
| `PluginLoadContext` | `private sealed` | `AssemblyLoadContext` mit `AssemblyDependencyResolver`; `Rezepte.Import.Abstractions` delegiert an Default-Kontext. |
| `LifecycleLockReleaser` | `private sealed` | `IAsyncDisposable`, gibt `_lifecycleLock` frei. |

Abonnierte Events: keine. Publizierte Events: keine (Statuspersistenz läuft über `RezepteDbContext`).

## `PluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`

Implementiert `IPluginUpdateService`; `Scoped` (`ServiceCollectionExtensions.cs:217`). Aufgerufen von `PluginUpdateHostedService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckForUpdatesAsync(CancellationToken)` | `public` | Alle `PluginSource` mit `Enabled && TrustConfirmed` sequenziell via `ProcessSourceAsync`. |
| `ProcessSourceAsync(PluginSource, CancellationToken)` | `private` | Quell-Status zurücksetzen, PAT für private Quellen, `GetLatestReleaseAsync`, `FindZipAsset`; überspringt bereits `Installed`-Releases; legt `PluginSourceRelease` an bzw. setzt fehlgeschlagene Stati auf `Pending` zurück; ruft `DownloadValidateInstallAsync`. |
| `DownloadValidateInstallAsync(source, record, asset, token, ct)` | `private` | Status `Downloading` → Download nach `%TEMP%/rezepte-plugin-download/{guid}` → `Validating` → `packageValidator.ValidateAsync` → `Installing` → **`packageInstaller.InstallWithReloadTrackingAsync(validation.PluginDirectories, beforeReload, ct)`** (Zeile 150) — `record.ReleaseTag` ist hier verfügbar, wird aber **nicht** übergeben. `beforeReload` setzt `Status`/`ReloadStatus` auf `Reloading`. Erfolg → `Installed` + `LastSuccessfulReleaseTag`. Fehlermapping: `GitHubRateLimitException`→`RateLimited`, `PluginPackageInstallException.Status`→`Status`, sonst statusabhängig `DownloadFailed`/`ValidationFailed`/`ReloadFailed`/`InstallFailed`; `ReloadFailed` zusätzlich in `ReloadStatus`/`ReloadError`. Temp-Verzeichnisse im `finally` best-effort gelöscht. |
| `UpsertReleaseFailureAsync(...)` | `private` | `PluginSourceRelease` mit Fehlerstatus anlegen/aktualisieren. |
| `MarkSourceFailureAsync(source, error, ct)` | `private` | `LastError`/`LastErrorAt` der Quelle setzen. |
| `TryDeleteDirectory(directory)` | `private` | Best-effort `Directory.Delete`; `IOException`/`UnauthorizedAccessException` nur Warn-Log. |

## `PluginUpdateHostedService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`

`IHostedService` (`ServiceCollectionExtensions.cs:206`). `StartAsync` erzeugt DI-Scope und ruft `IPluginUpdateService.CheckForUpdatesAsync` einmalig beim Anwendungsstart auf; Fehler werden geloggt (`"Plugin update startup check failed."`), Cancellation geschluckt. Kein eigener Bereinigungslauf. Registriert **nach** `PluginStartupService` (Zeile 205), d. h. die Plugin-Erkennung läuft vor dem Update-Check.

## `PluginStartupService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs`

`IHostedService` (`ServiceCollectionExtensions.cs:205`). `StartAsync` → `pluginManager.InitializeAsync` — der Pfad, über den eine Startbereinigung markierter Verzeichnisse wirksam würde (aktuell nicht vorhanden).

## `PluginPackageValidator`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs`

`Scoped` (`ServiceCollectionExtensions.cs:215`). `ValidateAsync` entpackt das ZIP nach `%TEMP%/rezepte-plugin-package/{guid}` (Pfad-Traversal-Schutz, Top-Level-Datei-Whitelist `README.md`/`LICENSE`/`LICENSE.txt`/`CHANGELOG.md`), ermittelt Plugin-Unterverzeichnisse mit `*.dll` (`pluginDirectories`, Zeilen 76-79) und validiert via `pluginManager.DiscoverFromDirectory(extractRoot, unloadAfterDiscovery: true)`, dass alle Deskriptoren `Loaded` sind. Liefert die Quellverzeichnisse für den Installer. Laut Anforderung nur insoweit betroffen wie das Verzeichnismodell es erfordert.

## `BuiltInImportPluginCatalog`

Datei: `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs`

`GetPlugins()` liefert `[]` — mitgelieferte Plugins werden ausschließlich über die Dateisystem-Discovery gefunden.

## Publish-/Build-Verdrahtung (kein C#-Code)

Datei: `Rezepte.Web/Rezepte.Web.csproj`

- `ImportPluginProject`-Items (Zeilen 54-62): `Rezepte.Import.Plugins.Backup`, `.AIFoto`, `.AIUrl` mit `<PluginName>`-Metadaten.
- Target `CopyImportPluginsToOutput` (Zeilen 69-79): löscht `$(OutputPath)plugins` komplett (`RemoveDir`) und kopiert Plugin-Build-Outputs nach `plugins/%(PluginName)/` sowie externe Artefakte aus `$(ExternalImportPluginsPath)` direkt unter `plugins/`.
- Target `CopyImportPluginsToPublish` (Zeilen 81-91): analog für `$(PublishDir)plugins`.
- `ExternalImportPluginsPath` (Zeile 9): `..\external\rezepte-import-plugins-private\artifacts\plugins` — existiert in dieser Arbeitskopie nicht.

**Relevant für die Anforderung:** Publish-Plugin-Verzeichnisse (`plugins/<Projektname>/`) und quelleninstallierte Verzeichnisse liegen im selben flachen `plugins/`-Namensraum; eine Unterscheidung (Namensschema/Verwaltungsverzeichnis) existiert nicht und ist Teil der Umsetzung.

## DI-Registrierung

`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`: `IPluginManager`→`PluginManager` Singleton (204), `PluginStartupService` (205), `PluginUpdateHostedService` (206), `IGitHubReleaseClient` HttpClient (209), `ISystemSecretStore` (214), `IPluginPackageValidator` (215), `IPluginPackageInstaller` (216), `IPluginUpdateService` (217), `IPluginSettingsService` (218).
