# Anforderungsübersetzung: Aktualisierung der Plugins (Issue #179)

## Fachliche Zusammenfassung

Die Update-Prüfung der GitHub-Pluginquellen wird für den Anwender transparent gemacht und um eine manuelle, quellenbezogene Update-Prüfung in der Admin-Oberfläche (`PluginSettings.razor`) erweitert. Gefundene Updates sollen ohne Anwendungsneustart wirksam werden: Neue Pluginversionen werden in versionsbezogenen Pluginverzeichnissen installiert, der bisherige Pluginbestand wird über den koordinierten Reload (`IPluginManager.CoordinateReloadAsync`) ausgetauscht, und nicht löschbare Altverzeichnisse werden zur späteren Löschung markiert. Ergänzend werden die Aktionsbuttons der Pluginquellen als Symbolbuttons dargestellt, und als `Missing` markierte Plugins können aus der Pluginverwaltung entfernt werden.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine einzelne GitHub-Pluginquelle (`PluginSource`) inklusive ihrer Release-Historie (`PluginSourceRelease`) und der daraus unter `plugins/` installierten Pluginverzeichnisse; ergänzend einzelne `PluginSetting`-Datensätze für die Entfernung vermisster Plugins.
- **Auslösende Ereignisse:**
  - Bestehend: automatische Prüfung aller aktivierten (`Enabled`) und vertrauensbestätigten (`TrustConfirmed`) Quellen einmalig beim Anwendungsstart über `PluginUpdateHostedService` → `IPluginUpdateService.CheckForUpdatesAsync`.
  - Neu: manuelle Nutzeraktion „Auf Updates prüfen" pro erfasster Pluginquelle in der Admin-Oberfläche.
  - Neu: manuelle Nutzeraktion „Entfernen" bei Plugins mit Status `PluginStatus.Missing` (`"Plugin was not found during startup discovery."`).
  - Systemereignis: Bereinigung zur Löschung markierter Alt-Pluginverzeichnisse beim nächsten Anwendungsstart bzw. beim nächsten koordinierten Reload.
- **Erwartete Wirkung:**
  - Der Anwender erkennt in der Pluginverwaltung, nach welchem Prinzip die Prüfung stattfindet (automatisch beim Anwendungsstart sowie manuell pro Quelle), und sieht je Quelle `LastCheckedAt`, `LastSuccessfulReleaseTag` und `LastError`.
  - Ein manuell ausgelöster Check installiert ein gefundenes neues Release und macht es ohne Neustart sofort wirksam; nachfolgende Importe verwenden die neuen Pluginversionen. Plugins, die im neuen Release nicht mehr enthalten sind, fallen weg und erhalten den Status `Missing`.
  - Altverzeichnisse, die wegen offener Datei-/Verzeichnishandles nicht gelöscht werden können, bleiben nicht im wirksamen Pluginbestand: Sie werden zur Löschung markiert und von der Discovery ausgeschlossen.
  - `Missing`-Plugins lassen sich aus der Verwaltung entfernen; werden sie bei einer späteren Erfassung wieder gefunden, werden sie wie neue Plugins behandelt (neue `DiscoveredAt`, an das Ende der Reihenfolge angehängt).
- **Umgang mit fehlenden Objekten:**
  - Kein neues Release vorhanden bzw. Release bereits installiert (`PluginSourceReleaseStatus.Installed`): Check wird als „kein Update" bzw. „übersprungen" behandelt — bestehendes Skip-Verhalten wird beibehalten.
  - Versionsverzeichnis der neuen Version: wird bei der Installation neu erzeugt.
  - Nicht löschbares Altverzeichnis: wird zur Löschung markiert (verwerfen) und bei der nächsten Bereinigung entfernt.
  - `PluginSetting`-Datensatz eines `Missing`-Plugins: kann über die neue Entfernen-Aktion verworfen werden; Wiederentdeckung erzeugt den Datensatz neu.
- **Scope-Ausschlüsse:**
  - Keine automatische periodische Hintergrundprüfung — die Anforderung verlangt Transparenz über das Prüfprinzip plus manuellen Button (siehe Offene Fragen).
  - Kein Update-Check für deaktivierte oder nicht vertrauensbestätigte Quellen; der manuelle Check folgt denselben Voraussetzungen wie der Start-Check (`Enabled && TrustConfirmed`) — Annahme, zu bestätigen.
  - Kein Downgrade-/Rollback-Angebot auf ältere Pluginversionen über die UI (bestehendes Installations-Rollback bei Fehlern bleibt).
  - Keine Änderung an Paketvalidierung (`PluginPackageValidator`), Vertragsprüfung, GitHub-Anbindung (`IGitHubReleaseClient`) oder PAT-Verwaltung (`ISystemSecretStore`).
  - Mitgelieferte Plugins aus dem Publish (`plugins/<Projektname>/`, `BuiltInImportPluginCatalog`) unterliegen nicht der Quellen-Update-Prüfung.

## Betroffene Klassen und Komponenten

- **Datenmodell / Entities (`Rezepte.Web/Entities/`):**
  - `PluginSource` — Bestandsfelder `LastCheckedAt`, `LastError`, `LastErrorAt`, `LastSuccessfulReleaseTag` werden für die Transparenz-Anzeige genutzt; ggf. keine Schemaänderung.
  - `PluginSourceRelease` — unverändert; `ReloadStatus`/`ReloadError` dokumentieren den Austausch.
  - `PluginSetting` — unverändert; neue Löschoperation für Datensätze mit Status `PluginStatus.Missing`.
  - Neu: Persistenz-Mechanismus für zur Löschung markierte Pluginverzeichnisse (z. B. Markerdatei bzw. Pending-Delete-Verzeichnis; Umsetzungsentscheidung, siehe Offene Fragen).
- **Logikklassen / Services (`Rezepte.Web/Services/Import/Plugins/`):**
  - `IPluginUpdateService` / `PluginUpdateService` — neue Methode für den Einzelquellen-Check (z. B. `CheckSourceForUpdatesAsync(string sourceId, CancellationToken)`), Wiederverwendung von `ProcessSourceAsync`; Schutz gegen parallele Checks.
  - `PluginUpdateHostedService` — Start-Prüfung bleibt; Erweiterung um die Bereinigung markierter Altverzeichnisse beim Start.
  - `IPluginSettingsService` / `PluginSettingsService` — neue Methoden für den manuellen Quellen-Check (Delegation an `IPluginUpdateService`, Admin-Prüfung via `EnsureAdmin`) und für das Entfernen von `Missing`-Plugins.
  - `IPluginPackageInstaller` / `PluginPackageInstaller` — Installation in versionsbezogene Zielverzeichnisse statt In-place-Ersetzung; Löschen bzw. Markieren von Altverzeichnissen innerhalb von `CoordinateReloadAsync`; angepasstes Rollback.
  - `PluginManager` / `IPluginManager` — Discovery (`DiscoverExternalPlugins`, `GetPluginAssemblyCandidates`) muss zur Löschung markierte Verzeichnisse überspringen; `SynchronizeSettingsAsync` liefert weiterhin `Missing`-Markierung und Neu-Erfassung.
  - `PluginSourceSettingsItem` — ggf. erweitert um Anzeige-/Statusfelder für den Check (z. B. laufende Prüfung).
- **Interfaces:** Erweiterung von `IPluginUpdateService` und `IPluginSettingsService` (s. o.).
- **Enums / Status:** `PluginSourceReleaseStatus` und `PluginStatus` (`Missing`, `Loaded`, `Incompatible`, `LoadFailed`, `RuntimeFailed`) sind ausreichend; keine neuen Status zwingend erforderlich.
- **UI-Komponenten:**
  - `Rezepte.Web/Components/Settings/PluginSettings.razor` — Hinweistext zum Prüfprinzip, Symbolbutton „Auf Updates prüfen" je Pluginquelle, Umstellung der Buttons „Bearbeiten"/„Löschen" auf Symbolbuttons (`bi`-Klassen; Bootstrap Icons sind bereits in `App.razor` eingebunden), „Entfernen"-Aktion für `Missing`-Plugins, Busy-/Ergebnisanzeige pro Check.
  - `Rezepte.Web/Resources/UiStrings.resx` — neue lokalisierte Strings (Tooltips, Prüfprinzip-Text, Ergebnis- und Fehlermeldungen); vorhandene Keys wie `UpdatePat`, `TrustThisSource`, `CheckNow` dienen als Muster.
- **Tests:**
  - `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` — Einzelquellen-Check, Skip bereits installierter Releases, Fehlerstatus.
  - `Rezepte.Tests/Services/Import/PluginPackageInstallerTests.cs` — versionsbezogene Verzeichnisse, Lösch-/Markierverhalten für Altverzeichnisse, Rollback.
  - `Rezepte.Tests/Services/Import/PluginManagerTests.cs` — Discovery ignoriert Löschmarkierungen; `Missing`-Markierung und Wieder-Neuerfassung.
  - `Rezepte.Tests/Services/Import/PluginSettingsServiceTests.cs` — Entfernen von `Missing`-Plugins.
  - `Rezepte.Tests.Browser` — UI-Tests für den neuen Prüf-Button, Symbolbuttons und die Entfernen-Aktion.
- **Dokumentation:** `Docs/help/import-plugins.md` und `Docs/features.md` — Beschreibung des Prüfprinzips (Start + manuell), der versionsbezogenen Verzeichnisse, der Löschmarkierung und der Entfernen-Aktion.

## Implementierungsansatz

- **Einzelquellen-Check:** `PluginUpdateService` erhält eine Methode, die eine Quelle per Id lädt, die Vorbedingungen (`Enabled`, `TrustConfirmed`, ggf. hinterlegtes PAT via `ISystemSecretStore`) prüft und die bestehende `ProcessSourceAsync`-Logik (Release abrufen → `PluginSourceRelease` anlegen/fortschreiben → Download → `PluginPackageValidator` → `PluginPackageInstaller` → Status speichern) ausführt. Parallele Prüfungen werden über einen Guard serialisiert (analog `InstallLock` in `PluginPackageInstaller`); die UI blendet den Button während eines Laufs aus bzw. zeigt den Fortschritt über die Statusfelder der Quelle.
- **Versionsbezogene Pluginverzeichnisse (Vorschlag des Auftraggebers):** Der Installer legt neue Releases in einem Verzeichnis mit Versionsnummer im Namen ab (z. B. `plugins/<Name>-<ReleaseTag>/`; Namensschema im Detail zu definieren). Innerhalb von `IPluginManager.CoordinateReloadAsync` werden die Altverzeichnisse der aktualisierten Plugins gelöscht; schlägt das Löschen wegen offener Handles fehl, wird das Verzeichnis zur Löschung markiert (z. B. Umbenennen in einen Pending-Delete-Bereich oder Markerdatei) und von `DiscoverExternalPlugins` ausgeschlossen. Die Bereinigung markierter Verzeichnisse erfolgt beim nächsten Anwendungsstart vor der Discovery bzw. beim nächsten koordinierten Austausch.
- **Entladen/Austausch:** `PluginManager` lädt produktive Plugin-Assemblies aktuell in einem nicht-collectible `AssemblyLoadContext` (`useCollectibleLoadContext: false` in `DiscoverExternalPlugins`/`DiscoverFromAssembly`); ein echtes Entladen ist damit nicht möglich — daher der versionsbezogene Ansatz: Die neue Version wird aus ihrem eigenen Verzeichnis geladen, die alte Assembly bleibt im Prozess, erhält aber keine neuen Handler mehr (`_loadedPlugins` wird beim Reload ersetzt, Handler werden pro Import über `AcquireActiveHandlersAsync` frisch erzeugt). Ob zusätzlich auf collectible Load Contexts umgestellt wird, ist eine gesonderte Umsetzungsentscheidung und nicht Voraussetzung.
- **Entfernen von `Missing`-Plugins:** `IPluginSettingsService` erhält eine Löschmethode für `PluginSetting`-Datensätze (nur Status `Missing`, Admin-Prüfung via `EnsureAdmin`). Bei Wiederentdeckung greift der bestehende Pfad in `SynchronizeSettingsAsync`, der unbekannte Plugins hinten an die Reihenfolge anhängt. Zu beachten: `PluginSettings` sind Teil des Systemexports (`ExportService`), ein wiederhergestelltes Backup kann den Eintrag zurückbringen.
- **UI-Umsetzung:** In `PluginSettings.razor` wird ein erläuternder Text zum Prüfprinzip ergänzt (Start-Check + manuelle Prüfung je Quelle). Die Aktionsbuttons der Quellen (`Bearbeiten`, `Löschen`, neu `Auf Updates prüfen`) werden als Symbolbuttons mit `bi`-Icons und lokalisierten Tooltips/`aria-label` umgesetzt; für `Missing`-Plugins kommt ein Symbolbutton „Entfernen" in der Pluginliste hinzu.

## Konfiguration

Keine neue Konfiguration zwingend erforderlich. Die Transparenz-Anforderung wird über einen Hinweistext in der Pluginverwaltung erfüllt, nicht über Konfiguration. Optional — abhängig von der Klärung der offenen Frage zur periodischen Prüfung — wäre ein Intervall konfigurierbar zu machen (z. B. `PluginUpdates:CheckInterval`, analog den Einstellungen `ApplicationUpdates` für Programmupdates).

## Offene Fragen

1. Soll neben der Start- und der manuellen Prüfung zusätzlich eine automatische periodische Prüfung (z. B. stündlich oder täglich) eingeführt werden? Die Problembeschreibung legt nahe, dass die Prüfung nur beim Anwendungsstart zu selten ist; die Anforderung verlangt explizit nur Transparenz plus manuellen Button.
2. Gilt für den manuellen „Auf Updates prüfen"-Button dieselbe Vorbedingung wie für den Start-Check (`Enabled && TrustConfirmed`), oder darf er auch bei deaktivierten bzw. nicht bestätigten Quellen ausgelöst werden? (Annahme: gleiche Vorbedingungen, Button sonst deaktiviert.)
3. Soll beim Entfernen eines `Missing`-Plugins ausschließlich der Verwaltungseintrag (`PluginSetting`) gelöscht werden? (Annahme: ja — per Definition existieren die Plugindateien nicht mehr im Pluginstamm; Reste außerhalb würden bei Discovery ohnehin nicht gefunden.)
4. Welches Namensschema sollen die versionsbezogenen Pluginverzeichnisse verwenden (z. B. `<PluginName>-<ReleaseTag>` vs. `<PluginName>/<ReleaseTag>`), und wird die Version aus dem Release-Tag oder aus `IImportPlugin.Version` bezogen? (Umsetzungsentscheidung — Annahme: Release-Tag.)
5. Wie wird die Löschmarkierung persistiert — Markerdatei/Umbenennen im Dateisystem oder Datenbank-Eintrag? (Umsetzungsentscheidung; Auswirkung auf Export/Backup beachten.)
6. Soll das Ergebnis eines manuellen Checks in der UI als direkte Rückmeldung angezeigt werden (z. B. „kein Update gefunden", „v0.3.5 installiert") zusätzlich zur Aktualisierung der Statusfelder `LastCheckedAt`/`LastSuccessfulReleaseTag`/`LastError`? (Annahme: ja.)
