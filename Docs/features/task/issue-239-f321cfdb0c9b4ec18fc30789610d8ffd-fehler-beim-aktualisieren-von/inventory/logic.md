# Logik

Logikklassen im Plugin-Update-Pfad (Issue #239). Zentrale Befundstellen sind mit **[Temp-Pfad]** markiert.

## `PluginUpdateService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`

Koordiniert die Update-Prüfung pro Quelle. Prozessweite Serialisierung über statisches `SemaphoreSlim CheckGate` (Zeile 25). Abhängigkeiten: `RezepteDbContext`, `IGitHubReleaseClient`, `ISystemSecretStore`, `IPluginPackageValidator`, `IPluginPackageInstaller`, `ILogger<PluginUpdateService>` — **keine** `IHostEnvironment`- oder Options-Abhängigkeit.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckForUpdatesAsync(ct)` | public | Startprüfung: alle `Enabled && TrustConfirmed`-Quellen laden und je `ProcessSourceAsync` abarbeiten (aufgerufen von `PluginUpdateHostedService`) |
| `CheckSourceForUpdatesAsync(sourceId, ct)` | public | Manuelle Prüfung einer Quelle; Vorbedingungen `SourceNotFound`/`SourceDisabled`/`SourceNotTrusted` → `PluginUpdateCheckResult.Failed`; aufgerufen von `PluginSettingsService` |
| `ProcessSourceAsync(source, ct)` | private | Release-Ermittlung via `gitHubReleaseClient.GetLatestReleaseAsync`, PAT-Auflösung, Zip-Asset-Auswahl, Upsert des `PluginSourceRelease`-Datensatzes, Übergabe an `DownloadValidateInstallAsync`; `catch (Exception)` → `MarkSourceFailureAsync(ex.Message)` + `Failed(ex.Message)` (rohe Exception-Message, Zeilen 161–166) |
| `DownloadValidateInstallAsync(source, record, asset, token, ct)` | private | **[Temp-Pfad]** Zeile 171: `workRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-download", Guid.NewGuid().ToString("N"))`; Zeile 172 `Directory.CreateDirectory(workRoot)`; Download → `zipPath` via `DownloadAssetAsync`, Validierung via `packageValidator.ValidateAsync`, Installation via `packageInstaller.InstallWithReloadTrackingAsync`; `catch (Exception)` mappt auf Release-Status (`RateLimited`/`InstallFailed`/`ReloadFailed` bzw. phasenabhängig `DownloadFailed`/`ValidationFailed`) und gibt `Failed(ex.Message)` zurück; `finally` räumt `validation.ExtractedRoot` und `workRoot` best-effort auf |
| `UpsertReleaseFailureAsync(...)` | private | Legt/aktualisiert einen fehlgeschlagenen Release-Datensatz und markiert die Quelle |
| `MarkSourceFailureAsync(source, error, ct)` | private | Setzt `source.LastError`/`LastErrorAt` und speichert |
| `TryDeleteDirectory(directory)` | private | Best-effort `Directory.Delete(recursive: true)`, fängt `IOException`/`UnauthorizedAccessException` mit Warnlog |

Publizierte Events: keine. Abonnierte Events: keine.

## `PluginPackageValidator`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs`

Validiert das ZIP-Paket (Pfadtraversal-/Top-Level-Regeln, DLL-Verzeichnisse, Discovery-Probe). Abhängigkeiten: `IPluginManager`, `ILogger<PluginPackageValidator>` — **kein** `IHostEnvironment`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ValidateAsync(zipPath, ct)` | public | **[Temp-Pfad]** Zeile 29: `extractRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-package", Guid.NewGuid().ToString("N"))`; Zeile 30 `Directory.CreateDirectory(extractRoot)` — erfolgt **außerhalb** des `try`, ein Fehler beim Anlegen propagiert also als Exception (nicht als `Failed`-Ergebnis); extrahiert Einträge mit Pfad-Sicherheitschecks, sammelt DLL-Verzeichnisse, ruft `pluginManager.DiscoverFromDirectory(extractRoot, unloadAfterDiscovery: true)`; `catch` übersetzt nur `InvalidDataException` bzw. `IOException`/`UnauthorizedAccessException` in `Failed(...)` — andere Fehler beim Verzeichnisanlegen laufen unbehandelt nach oben |

## `PluginPackageInstaller`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs`

Installiert validierte Plugin-Verzeichnisse versioniert unter `{ContentRootPath}/plugins` mit Rollback. Abhängigkeiten: `IHostEnvironment`, `IPluginManager`, `IPluginDirectoryRegistry`, `ILogger<PluginPackageInstaller>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InstallAsync(pluginDirectories, options, ct)` | public | Delegiert an `InstallWithReloadTrackingAsync` ohne Reload-Callback |
| `InstallWithReloadTrackingAsync(pluginDirectories, options, beforeReload, ct)` | public | Serialisiert via `SemaphoreSlim InstallLock`; `pluginRoot = Path.Combine(environment.ContentRootPath, "plugins")` (Zeile 52) + `Directory.CreateDirectory(pluginRoot)`; führt Ersetzung + State-Update innerhalb `pluginManager.CoordinateReloadAsync` aus; Fehler → `RollBackAsync` + `PluginPackageInstallException` |
| `ReplaceDirectories(operation, token)` | private | `CleanupMarkedDirectories`, löscht/kopiert Zielverzeichnisse; registrierte Ziele werden vorher via `BackupDirectory` gesichert |
| `UpdateInstallState` / `MarkDroppedSourceEntries` | private | Pflegt `PluginInstallState.Installs`/`PendingDeletes` |
| `RollBackAsync(snapshot, pluginRoot, operation)` | private | Stellt Backups wieder her, markiert Teilinstallationen zum Löschen, erneuter `CoordinateReloadAsync` |
| `BackupDirectory(target)` | private | **[Temp-Pfad]** Zeile 245: `backupPath = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup", Guid.NewGuid().ToString("N"))`; kopiert das bisherige Verzeichnis dorthin (`CopyDirectory` legt Zwischenebenen an); bei Fehler partielles Backup best-effort löschen und rethrow |
| `RestoreBackup(backup)` | private | Stellt `backup.BackupPath` nach `backup.TargetPath` wieder her |
| `CopyDirectory(source, target)` | private static | `Directory.CreateDirectory` für Ziel + alle Unterverzeichnisse, `File.Copy` je Datei |
| `TryDeleteDirectory(directory, logger, message, args)` | private static | Best-effort-Löschen mit Error-Log |

Private Records: `PendingInstall`, `InstallBackup(TargetPath, BackupPath)`, `InstallOperation` (State/Installs/PluginRoot/NewTargetNames/Backups).

## `PluginDirectoryRegistry`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginDirectoryRegistry.cs`

Verwaltet `{ContentRootPath}/plugins` (`PluginRoot`, Zeile 20) und `.install-state.json`. Relevanz für das Issue: `PluginRoot` ist der bereits beschriebene, verwaltete Basisordner (möglicher Fallback für Arbeitsverzeichnisse); `SaveState` legt `PluginRoot` bei Bedarf an (Zeile 64).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsManagedRoot(pluginRoot)` | public | Vergleich gegen `PluginRoot` (OrdinalIgnoreCase, FullPath) |
| `LoadState()` | public | Liest `.install-state.json`; fehlende/korrupte Datei → leerer State (fail-open mit Warnlog) |
| `SaveState(state)` | public | `Directory.CreateDirectory(PluginRoot)` + atomisches Schreiben über Temp-Datei + `File.Move` |
| `CreateVersionedDirectoryName(baseName, releaseTag)` | public | Erzeugt `<baseName>@<sanitizedTag>` |
| `TryParseVersionedDirectoryName(...)` | public | Splittet am ersten `@` |
| `GetExcludedDirectoryNames(pluginRoot, state)` | public | Namen, die von der Discovery auszuschließen sind |
| `SelectEffectiveDirectory(...)` | public | Registrierte Installation gewinnt, sonst höchste Version |
| `MarkPendingDeletion(state, directoryName)` | public | Fügt Namen zu `PendingDeletes` hinzu |
| `CleanupMarkedDirectories(state)` | public | Löscht markierte Verzeichnisse best-effort, persistiert State |

## `GitHubReleaseClient`
Datei: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs`

HTTP-Client für die GitHub-API (`IOptions<PluginUpdateOptions>`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetLatestReleaseAsync(repository, pat, ct)` | public | `GET /repos/{o}/{r}/releases/latest`; 404 → `null`; Rate-Limit → `GitHubRateLimitException` |
| `DownloadAssetAsync(asset, targetPath, pat, ct)` | public | Streamt das Asset; Zeile 73 `File.Create(targetPath)` — schlägt fehl, wenn das Elternverzeichnis (der Work-Root) nicht existiert/beschreibbar ist |

## `PluginSettingsService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`

`CheckSourceForUpdatesAsync(sourceId, ct)` (Zeilen 192–196): `EnsureAdmin()` + Delegation an `IPluginUpdateService.CheckSourceForUpdatesAsync` — keine eigene Fehlerbehandlung.

## `PluginUpdateHostedService`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`

`IHostedService`; `StartAsync` löst im eigenen Scope `IPluginUpdateService.CheckForUpdatesAsync` aus; fängt Exceptions nur zum Loggen (Zeilen 26–29). Nutzt denselben Codepfad → vom selben Fehlerbild betroffen.

## `PluginSettings` (Blazor-Komponente)
Datei: `Rezepte.Web/Components/Settings/PluginSettings.razor`

Admin-Oberfläche für Plugin-Quellen.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckSourceAsync(source)` | private | Button-Handler „Auf Updates prüfen"; ruft `PluginSettingsService.CheckSourceForUpdatesAsync`; `catch` → `"{PluginCheckFailed} ({ex.Message})"`; `Failed`-Outcome → `MapCheckError(result.Error)`; lädt danach die Quellenliste neu |
| `MapCheckError(error)` | private | Zeilen 389–404: Switch über die `PluginUpdateCheckErrors`-Konstanten → lokalisierte `UiStrings`-Schlüssel; **unbekannte Fehlertexte** (inkl. roher Exception-Messages) → `"{PluginCheckFailed} ({error})"` |
| `CheckButtonTitle/EditButtonTitle/DeleteButtonTitle` | private | Lokalisierte Tooltips/Disabled-Begründungen |
| `LoadAsync`/`ReloadSourcesAsync`/`SaveSourceAsync`/`SetSourceEnabledAsync`/`EditSource`/`DeleteSourceAsync`/`RemovePluginEntryAsync`/`SetEnabledAsync`/`MoveAsync`/`ResetSourceForm` | private | Quellen- und Plugin-Verwaltung (nicht update-bezogen) |

## Zusammenhänge

- `PluginSettings.razor` → `IPluginSettingsService.CheckSourceForUpdatesAsync` → `IPluginUpdateService.CheckSourceForUpdatesAsync` → `ProcessSourceAsync` → `DownloadValidateInstallAsync` → `IGitHubReleaseClient.DownloadAssetAsync` → `IPluginPackageValidator.ValidateAsync` → `IPluginPackageInstaller.InstallWithReloadTrackingAsync` → `IPluginManager.CoordinateReloadAsync` + `IPluginDirectoryRegistry`.
- Alle drei `Path.GetTempPath()`-Fundstellen (`PluginUpdateService:171`, `PluginPackageValidator:29`, `PluginPackageInstaller:245`) liegen auf diesem Pfad; eine fehlschlagende Verzeichnisbereitstellung landet als rohe `Exception.Message` in `PluginSource.LastError` und via `Failed(error)` im `MapCheckError`-Fallback der UI.
- Außerhalb des Scope, gleiches Muster: `ExportService.cs:277` (`rezepte-export-*.zip`), `AdminExportsController.cs:105` (`rezepte-restore-*.zip`).
