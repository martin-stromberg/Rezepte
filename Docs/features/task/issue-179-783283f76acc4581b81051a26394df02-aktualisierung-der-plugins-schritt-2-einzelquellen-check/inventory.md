# Bestandsaufnahme: Manuelle Update-Prüfung für eine einzelne Pluginquelle (Schritt 2, Issue #179)

Analysiert wurde der Bestand für die neue admin-geschützte Einzelquellen-Update-Prüfung — bezogen auf die übersetzte Anforderung in [requirement.md](requirement.md): `IPluginUpdateService.CheckSourceForUpdatesAsync` (Vorschlag), Delegation über `IPluginSettingsService` mit `EnsureAdmin`, instanzübergreifende Serialisierung gegenüber `CheckForUpdatesAsync` und ein rückmeldbares Prüfergebnis.

> **Verweis:** Eine vollständige Bestandsaufnahme des gesamten Plugin-Bereichs liegt projektweit unter [Docs/projects/task/issue-179-.../inventory.md](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory.md) vor (inkl. Detaildokumenten `inventory/models.md`, `logic.md`, `enums.md`, `interfaces.md` und der Test-Baseline `inventory/tests.md`). Dieses Dokument hält nur die für Schritt 2 relevanten Fakten fest.

## Zusammenfassung

- **Einzelquellen-Check fehlt vollständig:** `IPluginUpdateService` kennt nur `CheckForUpdatesAsync` (Gesamtlauf aller `Enabled && TrustConfirmed`-Quellen). Es gibt keine Methode für eine einzelne Quelle und keinen Ergebnis-Rückgabewert — `CheckForUpdatesAsync` liefert `Task`, Fehler werden intern via `MarkSourceFailureAsync` in `PluginSource.LastError`/`LastErrorAt` abgelegt und geschluckt.
- **Wiederverwendbarer Ablauf existiert:** `ProcessSourceAsync` (privat) kapselt den kompletten Check je Quelle inkl. PAT-Auflösung (`ISystemSecretStore.GetAsync`), `GetLatestReleaseAsync`, `FindZipAsset`, `PluginSourceRelease`-Upsert (Skip bei `Installed`, Reset auf `Pending` bei Fehlerstatus) und `DownloadValidateInstallAsync`. Alle in der Anforderung geforderten Fehlerpfade (kein PAT, kein Release, kein ZIP-Asset) sind vorhanden — liefern aber aktuell `void`.
- **Vorbedingungen nur als Filter, nicht als Guard:** `Enabled`/`TrustConfirmed` werden in `CheckForUpdatesAsync` als `Where`-Klausel ausgewertet; eine Ablehnung per Ausnahme für eine per Id adressierte Quelle existiert nicht. Das `InvalidOperationException`-Muster für „nicht gefunden" ist in `PluginSettingsService` etabliert (`SetSourceEnabledAsync`, `DeleteSourceAsync`: „Pluginquelle wurde nicht gefunden.").
- **Admin-Einstieg fehlt:** `PluginSettingsService` hat das `EnsureAdmin`-Muster (Authentifizierung + `IsInRole("Admin")` oder Claim `IsAdmin=true` → sonst `UnauthorizedAccessException`), aber keine Abhängigkeit auf `IPluginUpdateService` und keine Check-Methode.
- **Serialisierung fehlt auf Service-Ebene:** `PluginUpdateService` ist `Scoped` registriert; der Start-Lauf läuft in einem eigenen Scope (`PluginUpdateHostedService` via `IServiceScopeFactory`). Ein instanzübergreifender Guard für `CheckForUpdatesAsync` existiert nicht. Muster vorhanden: statisches `InstallLock` (`SemaphoreSlim(1,1)`) in `PluginPackageInstaller` (Installationsphase) und Instanz-`_manualCheckGate` in `ApplicationUpdateSettingsService` (nur wirksam, weil der Service faktisch als Singleton verwendet wird).
- **Neustartfreie Installation vorhanden:** `IPluginPackageInstaller.InstallWithReloadTrackingAsync` → `IPluginManager.CoordinateReloadAsync` mit versionsbezogenen Verzeichnissen (`IPluginDirectoryRegistry.CreateVersionedDirectoryName`, aus Schritt 1); Status-Rückverfolgung über `beforeReload`-Callback und `PluginPackageInstallException.Status`.
- **Ergebnis-DTO fehlt:** Kein `PluginUpdateCheckResult` o. ä. Muster: `ApplicationUpdateCommandResult` (`Outcome`, `State`, `Message`, `Error`, `IsSuccess`) in `ApplicationUpdateSettingsService.cs`.
- **Konfiguration unverändert nutzbar:** `PluginUpdateOptions` (`GitHubApiBaseUrl`, `TimeoutSeconds`, `UserAgent`) gilt nur für `GitHubReleaseClient`; keine neue Konfiguration nötig.

**Test-Ausgangszustand:** Projekt-Baseline (siehe [Projekt-Tests](../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/tests.md)): `Rezepte.Tests` 609/609 grün, `Rezepte.Tests.Browser` 19/20 mit einem nachgewiesenen preexisting Fehlschlag (`RegisterTests.Register_WithDemoData_CreatesDemoData`). Für diesen Schritt wurde der Plugin-gefilterte Lauf `dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"` ausgeführt: **100 bestanden / 0 fehlgeschlagen / 0 übersprungen**. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell / DTOs](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums / Status-Konstanten](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
