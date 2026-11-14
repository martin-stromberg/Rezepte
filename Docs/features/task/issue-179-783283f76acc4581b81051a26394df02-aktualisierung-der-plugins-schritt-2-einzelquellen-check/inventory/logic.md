# Logik — Bestandsaufnahme (Schritt 2: Einzelquellen-Check)

Nur die für diesen Schritt relevanten Klassen. Vollständige Plugin-Bestandsaufnahme: [Projekt-inventory/logic.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/logic.md).

## `PluginUpdateService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs`
Registrierung: `AddScoped<IPluginUpdateService, PluginUpdateService>()` (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:218`)

Konstruktor-Abhängigkeiten (Zeile 17–23): `RezepteDbContext`, `IGitHubReleaseClient`, `ISystemSecretStore`, `IPluginPackageValidator`, `IPluginPackageInstaller`, `ILogger<PluginUpdateService>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CheckForUpdatesAsync(ct)` | public | Lädt alle `PluginSources` mit `Enabled && TrustConfirmed` (Zeile 31–36) und ruft sequenziell `ProcessSourceAsync` je Quelle. Rückgabe `Task` — kein Ergebnis. |
| `ProcessSourceAsync(source, ct)` | private | Kompletter Einzelquellen-Ablauf (Zeile 44–121): setzt `LastCheckedAt`, löscht `LastError`/`LastErrorAt`, speichert; PAT-Auflösung via `secretStore.GetAsync(source.SecretName)` nur bei `IsPrivate` + `SecretName` (Z. 53–56); Fehlerpfad „Private source has no stored PAT." (Z. 59); `GetLatestReleaseAsync` → Fehler „No published GitHub release was found." (Z. 66); `FindZipAsset` → `UpsertReleaseFailureAsync` mit `ValidationFailed` „Latest release has no ZIP asset." (Z. 73); Skip bei `existing.Status == Installed` (Z. 80–84); sonst Upsert: neu anlegen mit `Pending` oder Reset auf `Pending` bei `ValidationFailed`/`DownloadFailed`/`InstallFailed`/`ReloadFailed`/`RateLimited` inkl. Löschen von `Error`/`ReloadStatus`/`ReloadError` (Z. 86–109); dann `DownloadValidateInstallAsync`. Fängt alle `Exception` außer `OperationCanceledException` → `MarkSourceFailureAsync` (Z. 116–120). |
| `DownloadValidateInstallAsync(source, record, asset, token, ct)` | private | Download/Validierung/Installation (Z. 123–208): Status-Fortschreibung `Downloading` → `Validating` → `Installing` → `Reloading` → `Installed`; bei Validierungsfehler `ValidationFailed` + `MarkSourceFailureAsync`; Installation via `packageInstaller.InstallWithReloadTrackingAsync(validation.PluginDirectories, new PluginPackageInstallOptions(record.ReleaseTag, source.Id), beforeReload-Callback, ct)`; bei Erfolg `Installed`, `source.LastSuccessfulReleaseTag = record.ReleaseTag`, Fehlerfelder zurückgesetzt (Z. 157–166). Exception-Mapping: `GitHubRateLimitException` → `RateLimited`, `PluginPackageInstallException` → `installException.Status`, sonst phasenabhängig `DownloadFailed`/`ValidationFailed`/`ReloadFailed`/`InstallFailed` (Z. 174–185); `ReloadFailed` zusätzlich in `ReloadStatus`/`ReloadError`; Temp-Verzeichnisse werden im `finally` best-effort gelöscht. |
| `UpsertReleaseFailureAsync(source, release, assetId, assetName, status, error, ct)` | private | Legt `PluginSourceRelease` mit Fehlerstatus an oder aktualisiert `Status`/`Error`, danach `MarkSourceFailureAsync` (Z. 210–236). |
| `MarkSourceFailureAsync(source, error, ct)` | private | Setzt `source.LastError`/`LastErrorAt` und speichert (Z. 238–243). |
| `TryDeleteDirectory(directory)` | private | Best-effort-Löschen mit `IOException`/`UnauthorizedAccessException`-Filter (Z. 245–255). |

**Schritt-2-Befund:** Der gesamte benötigte Ablauf existiert bereits privat in `ProcessSourceAsync`; was fehlt: Laden einer Quelle per Id, Guard `Enabled`/`TrustConfirmed` als Ablehnung (statt Filter), Ergebnis-Rückgabe und ein instanzübergreifender Guard (die Klasse hat derzeit **kein** eigenes Lock/Gate; `Scoped`-Lebensdauer ⇒ Instanz-Mitglieder serialisieren nicht über Scopes hinweg).

Abonnierte Events: keine. Publizierte Events: keine.

## `PluginUpdateHostedService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateHostedService.cs`
Registrierung: `AddHostedService<PluginUpdateHostedService>()` (`ServiceCollectionExtensions.cs:207`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(cancellationToken)` | public | Erstellt eigenen DI-Scope (`IServiceScopeFactory`), löst `IPluginUpdateService` auf und ruft `CheckForUpdatesAsync` einmalig auf; `OperationCanceledException` bei Cancellation wird geschluckt, andere Fehler nur geloggt (Z. 15–30). |
| `StopAsync(cancellationToken)` | public | No-op (`Task.CompletedTask`). |

**Schritt-2-Befund:** Zweiter Einstiegspunkt neben dem neuen manuellen Check; läuft in separatem Scope ⇒ eigene `PluginUpdateService`-Instanz und eigener `RezepteDbContext`. Serialisierung muss instanzübergreifend sein (z. B. statischer `SemaphoreSlim`).

## `PluginSettingsService`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`
Registrierung: `AddScoped<IPluginSettingsService, PluginSettingsService>()` (`ServiceCollectionExtensions.cs:219`)

Konstruktor-Abhängigkeiten (Z. 16–21): `RezepteDbContext`, `IPluginManager`, `IServiceProvider`, optional `IHttpContextAccessor?`, optional `ISystemSecretStore?`. **Keine** Abhängigkeit auf `IPluginUpdateService` — diese wäre für Schritt 2 neu zu injizieren.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetPluginsAsync(ct)` | public | Plugin-Liste + Usability via `pluginManager.GetPluginsUsabilityAsync`. **Kein** `EnsureAdmin`. |
| `GetSourcesAsync(ct)` | public | `EnsureAdmin`; liefert `PluginSourceSettingsItem` inkl. `LastSuccessfulReleaseTag`, `LastError`, `LastCheckedAt`, `LastErrorAt` (Z. 62–84). |
| `SaveSourceAsync(request, ct)` | public | `EnsureAdmin`; `GitHubRepository.Parse`; `InvalidOperationException` bei fehlender Trust-Bestätigung (Z. 99, deutsch), Duplikat (Z. 107) oder nicht gefundener Quelle (Z. 119); PAT nur in `ISystemSecretStore` (`SecretName`-Muster `plugin-source-{id}-pat`, Z. 137). |
| `SetSourceEnabledAsync(sourceId, enabled, ct)` | public | `EnsureAdmin`; `InvalidOperationException("Pluginquelle wurde nicht gefunden.")` bei unbekannter Id (Z. 158–159). |
| `DeleteSourceAsync(sourceId, ct)` | public | `EnsureAdmin`; gleiches Not-found-Muster; löscht Secret + Datensatz (Z. 170–182). |
| `SetEnabledAsync(pluginId, enabled, ct)` | public | **Kein** `EnsureAdmin`; `InvalidOperationException` bei unbekanntem Plugin. |
| `MoveAsync(pluginId, direction, ct)` | public | **Kein** `EnsureAdmin`; `InvalidOperationException` bei unbekanntem Plugin. |
| `EnsureAdmin()` | private | Wirft `UnauthorizedAccessException("Nur Administratoren dürfen Pluginquellen verwalten.")` wenn nicht authentifiziert oder weder `IsInRole("Admin")` noch Claim `IsAdmin=true` (Z. 237–249). |

**Schritt-2-Befund:** Die neue Admin-Methode (Vorschlag `CheckSourceForUpdatesAsync`) kann das exakte `EnsureAdmin`-Muster der Quellen-Methoden übernehmen; `EnsureAdmin` ist auf die Quellen-Verwaltung zugeschnitten (Meldungstext, bewusst nicht auf `SetEnabledAsync`/`MoveAsync` angewendet).

## Serialisierungs-/Lock-Muster im Bestand

| Muster | Ort | Wirkungsbereich | Befund für Schritt 2 |
|--------|-----|-----------------|----------------------|
| `private static readonly SemaphoreSlim InstallLock = new(1, 1)` | `PluginPackageInstaller.cs:13` | instanzübergreifend (statisch), umschließt die komplette Installationsphase inkl. `CoordinateReloadAsync` (Z. 51–92) | Bleibt für die Installationsphase bestehen; deckt die Check-Phase (Release-Abruf, DB-Upserts) nicht ab. |
| `private readonly SemaphoreSlim _manualCheckGate = new(1, 1)` | `ApplicationUpdateSettingsService.cs:64` | Instanz-Feld; wirksam, weil `ApplicationUpdateSettingsService` faktisch einmalig verwendet wird — `CheckAsync` wartet via `WaitAsync`/`Release` in `try/finally` (Z. 171–186) | Muster für „manueller Check wartet" — für `PluginUpdateService` aber wegen `Scoped`-Lebensdauer nur als **statisches** Feld wirksam. |
| `_lifecycleLock` | `PluginManager` | interner Reload-Schutz | Nicht direkt relevant (siehe Projekt-inventory). |

## Muster für Ergebnis-Rückgabe

`ApplicationUpdateCommandResult` (`ApplicationUpdateSettingsService.cs:316–339`): `sealed record(string Outcome, string State, string? Message, string? Error)` mit `IsSuccess`-Ableitung und `FromResult`-Factory. Wird von `CheckAsync`/`DownloadAsync`/`InstallAsync` zurückgegeben — Referenz für das in der Anforderung vorgeschlagene `PluginUpdateCheckResult`.

## Neustartfreie Wirksamkeit (unverändert genutzt)

`IPluginPackageInstaller.InstallWithReloadTrackingAsync` → `pluginManager.CoordinateReloadAsync` (`PluginPackageInstaller.cs:64–72`): Verzeichnis-Tausch + `UpdateInstallState` (versionsbezogene Namen via `directoryRegistry.CreateVersionedDirectoryName`, Z. 48) + `MarkDroppedSourceEntries` (per `PluginPackageInstallOptions.SourceKey`) erfolgen innerhalb des koordinierten Reloads; `beforeReload`-Callback setzt den `PluginSourceRelease`-Status auf `Reloading`. Rollback-Pfad `RollBackAsync` wirft `PluginPackageInstallException` mit `ReloadFailed`/`InstallFailed` (Z. 78–79). Aus Schritt 1 vorhanden — kein Umbau nötig.
