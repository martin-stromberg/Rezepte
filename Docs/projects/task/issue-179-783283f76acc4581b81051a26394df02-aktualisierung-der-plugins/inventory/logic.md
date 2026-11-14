# Logik — Bestandsaufnahme

## `PluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`

Implementiert `IPluginUpdateService`; registriert als `Scoped` (`ServiceCollectionExtensions.cs:217`). Konstruktor-Dependencies: `RezepteDbContext`, `IGitHubReleaseClient`, `ISystemSecretStore`, `IPluginPackageValidator`, `IPluginPackageInstaller`, `ILogger<PluginUpdateService>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckForUpdatesAsync(CancellationToken)` | `public` | Lädt alle Quellen mit `Enabled && TrustConfirmed` (sortiert nach `Owner`, `Repository`) und ruft je Quelle `ProcessSourceAsync` sequenziell auf. Kein Guard gegen parallele Aufrufe. |
| `ProcessSourceAsync(PluginSource, CancellationToken)` | `private` | Setzt `LastCheckedAt`, löscht `LastError`/`LastErrorAt`; prüft PAT-Pflicht für private Quellen (`"Private source has no stored PAT."`); ruft `GetLatestReleaseAsync`; bei fehlendem Release `"No published GitHub release was found."`; bei fehlendem ZIP-Asset `ValidationFailed` via `UpsertReleaseFailureAsync`; überspringt bereits `Installed`-Releases (Log `"Skipping plugin source ..."`); legt `PluginSourceRelease` an bzw. setzt zuvor fehlgeschlagene Stati (`ValidationFailed`, `DownloadFailed`, `InstallFailed`, `ReloadFailed`, `RateLimited`) auf `Pending` zurück; ruft `DownloadValidateInstallAsync`. Fehler werden geloggt und via `MarkSourceFailureAsync` persistiert. |
| `DownloadValidateInstallAsync(PluginSource, PluginSourceRelease, GitHubReleaseAsset, string?, CancellationToken)` | `private` | Statusübergänge `Downloading` → Download in `%TEMP%/rezepte-plugin-download/{guid}` → `Validating` → `PluginPackageValidator.ValidateAsync` → `Installing` → `packageInstaller.InstallWithReloadTrackingAsync` (mit `beforeReload`-Callback, der `Reloading` in `Status`/`ReloadStatus` schreibt) → `Installed` + `LastSuccessfulReleaseTag`. Fehler werden auf `RateLimited`/`DownloadFailed`/`ValidationFailed`/`InstallFailed`/`ReloadFailed` (aus `PluginPackageInstallException.Status` bzw. aktuellem Status) abgebildet; Temp-Verzeichnisse werden im `finally` best-effort gelöscht (`TryDeleteDirectory`, `IOException`/`UnauthorizedAccessException` nur geloggt). |
| `UpsertReleaseFailureAsync(...)` | `private` | Legt `PluginSourceRelease` mit Fehlerstatus an oder aktualisiert ihn; ruft `MarkSourceFailureAsync`. |
| `MarkSourceFailureAsync(PluginSource, string, CancellationToken)` | `private` | Setzt `LastError` + `LastErrorAt` auf der Quelle. |
| `TryDeleteDirectory(string)` | `private` | Best-effort `Directory.Delete(recursive: true)` mit Warn-Log bei `IOException`/`UnauthorizedAccessException`. |

**Für die Anforderung relevant:** Es existiert kein öffentlicher Einstieg für einen Einzelquellen-Check (`CheckSourceForUpdatesAsync` o. ä.) — `ProcessSourceAsync` ist privat und erwartet eine bereits geladene `PluginSource`.

## `PluginUpdateHostedService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`

`IHostedService`, registriert in `ServiceCollectionExtensions.cs:206`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | `public` | Erzeugt einen DI-Scope, ruft `IPluginUpdateService.CheckForUpdatesAsync` einmalig auf; `OperationCanceledException` bei Shutdown wird geschluckt, andere Fehler geloggt (`"Plugin update startup check failed."`). Keine Bereinigung markierter Verzeichnisse, keine periodische Wiederholung. |
| `StopAsync(CancellationToken)` | `public` | No-op (`Task.CompletedTask`). |

## `PluginStartupService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs`

`IHostedService` (`ServiceCollectionExtensions.cs:205`), delegiert `StartAsync` an `IPluginManager.InitializeAsync` — also die Start-Discovery vor dem Update-Check (Reihenfolge der `AddHostedService`-Aufrufe).

## `PluginPackageInstaller`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs`

Implementiert `IPluginPackageInstaller`; `Scoped` (`ServiceCollectionExtensions.cs:216`). Serialisiert Installationen über `private static readonly SemaphoreSlim InstallLock = new(1, 1)`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InstallAsync(IReadOnlyList<string>, CancellationToken)` | `public` | Delegiert an `InstallWithReloadTrackingAsync` ohne `beforeReload`. |
| `InstallWithReloadTrackingAsync(IReadOnlyList<string>, Func<CancellationToken, Task>?, CancellationToken)` | `public` | Wirft bei leerer Liste. Unter `InstallLock` + `pluginManager.CoordinateReloadAsync`: **In-place-Ersetzung** — je Quellverzeichnis wird das Ziel `plugins/{Verzeichnisname}` nach `%TEMP%/rezepte-plugin-backup/{guid}` gesichert, gelöscht und neu kopiert (`CopyDirectory`). Anschließend `beforeReload`-Callback, dann Reload. Bei Fehler: `RollBackAsync` (löscht neue Ziele, stellt Backups wieder her, erneutes `CoordinateReloadAsync`), dann `PluginPackageInstallException` mit Status `InstallFailed` bzw. `ReloadFailed` (je nachdem ob `replacementCompleted`). Zielverzeichnisname = `Path.GetFileName(sourceDirectory)` — **kein** Versionsschema. |
| `CopyDirectory(string, string)` | `private static` | Rekursive Verzeichniskopie mit `File.Copy(..., overwrite: true)`. |
| `TryDeleteDirectory(...)` | `private static` | Best-effort-Löschung, `IOException`/`UnauthorizedAccessException` → Error-Log. |

**Für die Anforderung relevant:** Es gibt kein versionsbezogenes Zielverzeichnis und keine Löschmarkierung; ein nicht löschbares Altverzeichnis würde `Directory.Delete` werfen lassen → Fehlerstatus + Rollback, kein Pending-Delete-Pfad.

## `PluginManager`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`

Implementiert `IPluginManager`; `Singleton` (`ServiceCollectionExtensions.cs:204`). Hält `_loadedPlugins` (Dictionary `PluginId` → `ImportPluginDescriptor`) und serialisiert Lifecycle über `_lifecycleLock` (`SemaphoreSlim`) sowie `_syncRoot`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync(CancellationToken)` | `public` | Delegiert an `ReloadAsync`. |
| `ReloadAsync(CancellationToken)` | `public` | Unter `_lifecycleLock`: leert `_loadedPlugins`, ruft `InitializeCoreAsync`. |
| `CoordinateReloadAsync(Func<CancellationToken, Task>, CancellationToken)` | `public` | Unter `_lifecycleLock`: leert `_loadedPlugins`, führt `replacePlugins` (Dateiaustausch) aus, danach `InitializeCoreAsync`. Wird von `PluginPackageInstaller` genutzt; Imports sind währenddessen gesperrt, weil `AcquireActiveHandlersAsync` dasselbe Lock hält. |
| `InitializeCoreAsync(CancellationToken)` | `private` | `DiscoverPlugins` → `_loadedPlugins` aus `Loaded`-Deskriptoren (Dedup via `SelectPreferredDescriptor`) → `SynchronizeSettingsAsync`. |
| `GetActiveHandlersAsync(IServiceProvider, CancellationToken)` | `public` | Lease öffnen+schließen, `Handlers` zurückgeben. |
| `AcquireActiveHandlersAsync(IServiceProvider, CancellationToken)` | `public` | Hält `_lifecycleLock` bis `PluginHandlerLease` disposed; erzeugt frische `IImportHandler`-Instanzen via `ActivatorUtilities` für `PluginSettings` mit `Enabled && Status == Loaded`, sortiert nach `OrderIndex`, `DisplayName`. Instanziierungsfehler → `MarkRuntimeFailureAsync` (`RuntimeFailed`). |
| `GetPluginsUsabilityAsync(IServiceProvider, CancellationToken)` | `public` | Instanziiert `IImportPlugin` (`PluginType`) und ruft `CheckUsabilityAsync`; Exceptions → `IsUsable = false`. |
| `DiscoverFromDirectory(string, bool unloadAfterDiscovery)` | `public` | Discovery eines freien Verzeichnisses; mit `unloadAfterDiscovery: true` collectible `AssemblyLoadContext` (für Paketvalidierung). |
| `DiscoverPlugins()` | `private` | `BuiltInImportPluginCatalog.GetPlugins()` (aktuell leer) + `DiscoverExternalPlugins(GetPluginRoots(), useCollectibleLoadContext: false)` — **nicht-collectible** Ladekontexte, echtes Entladen nicht möglich. |
| `DiscoverExternalPlugins(IEnumerable<string>, bool)` | `private` | Sammelt `*.dll` direkt in den Plugin-Roots plus Kandidaten aus Unterverzeichnissen (`GetPluginAssemblyCandidates`), dedupliziert, ruft `DiscoverFromAssembly`. **Kein** Filter für Löschmarkierungen. |
| `GetPluginRoots()` | `private` | `{ContentRoot}/plugins` und `{BaseDirectory}/plugins` (existierende, dedupliziert). |
| `GetPluginAssemblyCandidates(string)` | `private static` | Bevorzugt `{Verzeichnisname}.dll`, sonst alle `*.dll` im Verzeichnis. |
| `DiscoverFromAssembly(string, bool)` | `private` | `PluginLoadContext` (collectible optional); findet `IImportPlugin`-Implementierungen; erzeugt `ImportPluginDescriptor` mit Status `Loaded`, `Incompatible` (kein Plugin / keine stabile Id / Handler-Typ ungültig) oder `LoadFailed`; bekannte Vertrags-Assembly (`Rezepte.Import.Abstractions`) ohne Plugin wird ignoriert. |
| `FailedDescriptor(...)` | `private static` | Synthetischer Deskriptor `"{status}:{assemblyname}"` für Fehlerfälle. |
| `SynchronizeSettingsAsync(RezepteDbContext, IReadOnlyList<ImportPluginDescriptor>, CancellationToken)` | `private static` | Aktualisiert vorhandene `PluginSetting` (Name, Assembly, Status, `LastSeenAt`); legt neue hinten an (`OrderIndex = max+1`, `Enabled` nur bei `Loaded`); Erstlauf sortiert nach `DefaultPriority`. Nicht mehr entdeckte Einträge → `Status = Missing`, `Error = "Plugin was not found during startup discovery."`. |
| `SelectPreferredDescriptor(...)` | `private static` | Bevorzugt produktive `Rezepte.Import.Plugins.*`-Assemblies gegenüber Platzhaltern/`Rezepte.Web` bei doppelten Ids. |
| `MarkRuntimeFailureAsync(...)` | `private static` | Setzt `Status = RuntimeFailed` + `Error`. |
| `PluginLoadContext` | `private sealed` | `AssemblyLoadContext` mit `AssemblyDependencyResolver`; `Rezepte.Import.Abstractions` wird an den Default-Kontext delegiert (`return null`). |
| `LifecycleLockReleaser` | `private sealed` | `IAsyncDisposable`, gibt `_lifecycleLock` frei. |

## `PluginSettingsService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

Implementiert `IPluginSettingsService`; `Scoped` (`ServiceCollectionExtensions.cs:218`). Dependencies: `RezepteDbContext`, `IPluginManager`, `IServiceProvider`, optional `IHttpContextAccessor`, `ISystemSecretStore`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetPluginsAsync(CancellationToken)` | `public` | `PluginSettings` nach `OrderIndex`, `DisplayName` als `PluginSettingsItem`; ergänzt `Usability` via `pluginManager.GetPluginsUsabilityAsync`. **Kein** `EnsureAdmin`. |
| `GetSourcesAsync(CancellationToken)` | `public` | `EnsureAdmin`; `PluginSources` als `PluginSourceSettingsItem` inkl. Statusfelder. |
| `SaveSourceAsync(PluginSourceSaveRequest, CancellationToken)` | `public` | `EnsureAdmin`; parst URL via `GitHubRepository.Parse`, verlangt `TrustConfirmed` für neue Quellen, Duplikatprüfung auf `Owner`+`Repository`, speichert PAT nur im `ISystemSecretStore` (`SecretName`-Verweis), entfernt Secret bei Umstellung auf öffentlich. |
| `SetSourceEnabledAsync(string, bool, CancellationToken)` | `public` | `EnsureAdmin`; schaltet `Enabled` einer Quelle. |
| `DeleteSourceAsync(string, CancellationToken)` | `public` | `EnsureAdmin`; löscht Quelle + zugehöriges Secret. |
| `SetEnabledAsync(string, bool, CancellationToken)` | `public` | Schaltet `Enabled` eines `PluginSetting`; **kein** `EnsureAdmin`. |
| `MoveAsync(string, int, CancellationToken)` | `public` | Vertauscht `OrderIndex` mit Nachbar; **kein** `EnsureAdmin`. |
| `EnsureAdmin()` | `private` | Wirft `UnauthorizedAccessException`, wenn kein authentifizierter Admin (`IsInRole("Admin")` oder Claim `IsAdmin=true`). |

**Für die Anforderung relevant:** Keine Methode zum Entfernen eines `PluginSetting` und keine Methode für einen manuellen Quellen-Check vorhanden.

## `PluginPackageValidator`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs`

`Scoped` (`ServiceCollectionExtensions.cs:215`). `ValidateAsync` entpackt das ZIP nach `%TEMP%/rezepte-plugin-package/{guid}` mit Pfad-Traversal-Schutz und Top-Level-Datei-Whitelist (`README.md`, `LICENSE`, `LICENSE.txt`, `CHANGELOG.md`), ermittelt Plugin-Unterverzeichnisse mit `*.dll` und prüft via `pluginManager.DiscoverFromDirectory(extractRoot, unloadAfterDiscovery: true)` (collectible Context), ob alle Deskriptoren `Loaded` sind. Von der Anforderung explizit ausgeschlossen (keine Änderung nötig).

## `PluginSettings.razor` (UI)

Datei: `Rezepte.Web/Components/Settings/PluginSettings.razor`; eingebunden in `Rezepte.Web/Components/Pages/Settings.razor`. Injiziert `IPluginSettingsService` und `IStringLocalizer<UiStrings>`.

- **Quellen-Formular:** `EditForm` mit URL, `Privat`, `Aktiv`, PAT (`Localizer["UpdatePat"]`), `TrustConfirmed`-Checkbox (`Localizer["TrustThisSource"]`), Buttons „Speichern"/„Abbrechen" (Textbuttons).
- **Quellen-Tabelle:** Spalten Aktiv-Switch, Quelle, Status (Badge `LastSuccessfulReleaseTag`, `LastError`, `LastCheckedAt`), Aktionen: Textbuttons „Bearbeiten" und „Löschen" (`btn btn-outline-*`). **Kein** „Auf Updates prüfen"-Button, **kein** Hinweistext zum Prüfprinzip.
- **Plugin-Tabelle:** Spalten Aktiv-Switch, Name (inkl. `Error`), Status-Badge (`StatusClass`), Assembly, Reihenfolge (Pfeil-Buttons `↑`/`↓` mit `title`-Attribut, `Localizer["MoveUp"]`/`MoveDown`, `aria-label` `Localizer["PluginOrder"]`). `Missing` → `badge text-bg-warning`. **Keine** Entfernen-Aktion.
- **Code-Behind:** `LoadAsync`, `SaveSourceAsync`, `SetSourceEnabledAsync`, `DeleteSourceAsync`, `EditSource`, `ResetSourceForm`, `SetEnabledAsync`, `MoveAsync`, `StatusClass`, `SourceForm` (private Klasse). Fehler werden pauschal als `message` angezeigt; `isSaving`/`isLoading` als Busy-Flags. Einige UI-Texte sind hartkodiert deutsch statt lokalisiert.

## Weitere relevante Infrastruktur

- `BuiltInImportPluginCatalog.GetPlugins()` (`Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs`) liefert aktuell eine leere Liste — mitgelieferte Plugins (`plugins/<Projektname>/` im Publish) laufen über die normale Discovery, unterliegen aber nicht dem Quellen-Update.
- `GitHubRepository` (`Rezepte.Web/Services/Import/Plugins/GitHubRepository.cs`): Record mit `Parse`/`TryParse` (nur `https://github.com/{owner}/{repo}`).
- `GitHubReleaseClient` (`Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs`): `HttpClient`-basiert (`ServiceCollectionExtensions.cs:209`), wirft `GitHubRateLimitException` bei Rate-Limits.
- `DataProtectionSystemSecretStore` (`Rezepte.Web/Services/Import/Plugins/DataProtectionSystemSecretStore.cs`): `Scoped`-Implementierung von `ISystemSecretStore`.
- DI-Registrierung: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`, Zeilen 44 (`PluginUpdateOptions`), 204–218.
- Dokumentation des Ist-Zustands: `Docs/help/import-plugins.md` (beschreibt bereits „einmalig beim Start", kein manueller Button) und `Docs/features.md` (Zeile 42–48).
