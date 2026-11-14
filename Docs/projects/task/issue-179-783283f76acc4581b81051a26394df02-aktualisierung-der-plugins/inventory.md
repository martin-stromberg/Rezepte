# Bestandsaufnahme: Aktualisierung der Plugins (Issue #179)

Analysiert wurde der Bestand rund um die GitHub-Pluginquellen, die Update-Prüfung, die Paketinstallation, den Plugin-Reload und die Admin-Oberfläche (`PluginSettings.razor`) — bezogen auf die übersetzte Anforderung in [requirement.md](requirement.md): transparenter Hinweis auf das Prüfprinzip, manueller „Auf Updates prüfen"-Button je Quelle mit neustartfreiem Update über versionsbezogene Verzeichnisse, Symbolbuttons sowie Entfernen von `Missing`-Plugins.

## Zusammenfassung

- **Automatische Update-Prüfung existiert:** `PluginUpdateHostedService` ruft beim Anwendungsstart einmalig `IPluginUpdateService.CheckForUpdatesAsync` auf, das alle aktivierten (`Enabled`) und vertrauensbestätigten (`TrustConfirmed`) `PluginSource`-Datensätze über `ProcessSourceAsync` verarbeitet (Release abrufen → `PluginSourceRelease` anlegen → Download → `PluginPackageValidator` → `PluginPackageInstaller`). Es gibt **keine** periodische Hintergrundprüfung und **keinen** manuellen Einzelquellen-Check — `IPluginUpdateService` kennt nur den Gesamtlauf.
- **Transparenz-Felder sind vorhanden:** `PluginSource` besitzt bereits `LastCheckedAt`, `LastError`, `LastErrorAt`, `LastSuccessfulReleaseTag`; `PluginSourceSettingsItem` führt alle vier Felder. Die UI zeigt Tag, Fehler und Prüfzeitpunkt an, erklärt aber nicht das Prüfprinzip. `Docs/help/import-plugins.md` dokumentiert das Prinzip bereits textlich.
- **Reload ohne Neustart existiert, aber In-place:** `PluginPackageInstaller` ersetzt Pluginverzeichnisse unter `plugins/` in-place (gleicher Verzeichnisname, Backup+Kopie) innerhalb von `IPluginManager.CoordinateReloadAsync`. Es gibt **keine** versionsbezogenen Verzeichnisse und **keinen** Mechanismus zur Markierung nicht löschbarer Altverzeichnisse (kein Pending-Delete, keine Markerdatei — nichts davon existiert).
- **Entladen ist nicht vorgesehen:** Produktive Plugins werden in einem nicht-collectible `AssemblyLoadContext` geladen (`useCollectibleLoadContext: false` in `DiscoverPlugins`/`DiscoverExternalPlugins`); collectible Kontexte werden nur bei der Paketvalidierung (`DiscoverFromDirectory(..., unloadAfterDiscovery: true)`) verwendet. Der Austausch wirkt über das Ersetzen von `_loadedPlugins` und frische Handler pro Import (`AcquireActiveHandlersAsync` hält `_lifecycleLock`).
- **`Missing`-Status existiert, Entfernen fehlt:** `PluginManager.SynchronizeSettingsAsync` markiert nicht mehr entdeckte Plugins mit `PluginStatus.Missing` + `"Plugin was not found during startup discovery."` und legt neu entdeckte Plugins hinten an die Reihenfolge an — die Wiedererfassung würde also funktionieren. `IPluginSettingsService`/`PluginSettingsService` haben jedoch **keine** Löschmethode für `PluginSetting`-Datensätze (nur `DeleteSourceAsync` für Quellen).
- **UI nutzt Textbuttons:** `PluginSettings.razor` verwendet für „Bearbeiten"/„Löschen" bei Quellen Textbuttons (`btn btn-outline-*` mit Text), bei der Plugin-Reihenfolge Pfeil-Buttons mit `title`-Attribut. `bi`-Symbolbuttons existieren dort noch nicht; Bootstrap Icons sind in `App.razor` (CDN bootstrap-icons@1.11.3) eingebunden. Es gibt keinen Prüf-Button, keinen Hinweistext zum Prüfprinzip und keine Entfernen-Aktion für `Missing`-Plugins.
- **Serialisierung teilweise vorhanden:** `PluginPackageInstaller` serialisiert Installationen über ein statisches `InstallLock` (`SemaphoreSlim`); `PluginManager` über `_lifecycleLock`. `PluginUpdateService` selbst hat keinen Guard gegen parallele `CheckForUpdatesAsync`-Aufrufe.
- **Konfiguration:** `PluginUpdateOptions` (Abschnitt `PluginUpdates`) enthält nur `GitHubApiBaseUrl`, `TimeoutSeconds`, `UserAgent` — kein Prüfintervall.

**Test-Ausgangszustand:** `dotnet test Rezepte.sln` (Debug) liefert **609/609 bestanden** in `Rezepte.Tests` und **19 bestanden / 1 fehlgeschlagen** in `Rezepte.Tests.Browser`. Der Fehlschlag `RegisterTests.Register_WithDemoData_CreatesDemoData` (`System.TimeoutException`, Demo-Daten-Zähler erreichen erwarteten Stand nicht) ist in zwei Läufen reproduzierbar nachgewiesen und damit ein dokumentierter bestehender Fehler außerhalb des Anforderungsbereichs. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
