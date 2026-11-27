# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `f321cfdb-0c9b-4ec1-8fc3-0789610d8ffd`
- Branch: `task/issue-239-f321cfdb0c9b4ec18fc30789610d8ffd-fehler-beim-aktualisieren-von`
- Verknüpftes Issue: `martin-stromberg/Rezepte` #239 — „Fehler beim Aktualisieren von Plugins"
- Kundenanforderung: Die manuelle Update-Prüfung einer Plugin-Quelle schlägt mit „Prüfung fehlgeschlagen. (Could not find file '/tmp/rezepte-plugin-download'.)" fehl. Benötigte Arbeitsverzeichnisse dürfen keine bereits vorhandene Verzeichnisstruktur voraussetzen; sie werden zuverlässig angelegt bzw. ein geeignetes, garantiert beschreibbares Basisverzeichnis wird verwendet, damit die Plugin-Aktualisierung wieder funktioniert.

## Fachliche Zusammenfassung

Der Plugin-Update-Pfad legt seine temporären Arbeitsverzeichnisse (Download, Paket-Extraktion, Install-Backup) unmittelbar unter `Path.GetTempPath()` an und setzt damit voraus, dass das vom Laufzeitsystem gemeldete Temp-Basisverzeichnis existiert und beschreibbar ist. Ist das nicht der Fall, scheitert die Update-Prüfung mit einer rohen Laufzeit-Exception, die der Admin-Oberfläche unverändert als `PluginCheckFailed`-Meldung mit Exception-Text angezeigt wird. Die Korrektur macht die Bereitstellung der Arbeitsverzeichnisse robust: zuverlässiges Anlegen inklusive Zwischenebenen und/oder die Nutzung eines garantiert beschreibbaren Basisverzeichnisses; kann kein Arbeitsverzeichnis bereitgestellt werden, resultiert eine aussagekräftige, fachliche (lokalisierte) Fehlermeldung statt der rohen Exception.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Plugin-Update-Prüfung einer einzelnen Plugin-Quelle (`CheckSourceForUpdatesAsync`), bestehend aus den Phasen Release-Ermittlung, Download, Validierung, Installation und Plugin-Reload. Da die automatische Startprüfung (`CheckForUpdatesAsync` via `PluginUpdateHostedService`) denselben Codepfad (`ProcessSourceAsync` → `DownloadValidateInstallAsync`) nutzt, ist sie vom selben Fehlerbild betroffen und gehört fachlich zum Scope.
- **Auslösende Ereignisse:** Administrator betätigt in der Plugin-Verwaltung (`PluginSettings.razor`) den Aktionsbutton „Auf Updates prüfen" einer Quelle; alternativ die automatische Prüfung aller aktivierten, als vertrauenswürdig bestätigten Quellen beim Anwendungsstart.
- **Erwartete Wirkung:**
  - Die Update-Prüfung pro Quelle läuft ohne die genannte `FileNotFoundException`-Meldung durch; ein gefundenes Update wird installiert und ohne Anwendungsneustart wirksam.
  - Kann ein Arbeitsverzeichnis nicht bereitgestellt werden, wird eine aussagekräftige, fachliche Fehlermeldung in der Admin-Oberfläche und in `PluginSource.LastError` angezeigt — nicht die rohe Exception-Message der Laufzeit.
- **Umgang mit fehlenden Objekten:**
  - Temp-Arbeitsverzeichnisse `rezepte-plugin-download`, `rezepte-plugin-package` und `rezepte-plugin-backup` (jeweils mit GUID-Unterebene): erzeugen — sie sind flüchtig und dürfen nicht vorausgesetzt werden; das Anlegen muss ohne Annahme einer bestehenden Zwischenstruktur funktionieren.
  - Beschreibbares Basisverzeichnis: wiederverwenden — als garantiert beschreibbarer Fallback bietet sich der verwaltete Plugin-Root `{ContentRootPath}/plugins` an (wird bereits von `PluginDirectoryRegistry` angelegt und beschrieben); alternativ ist ein konfigurierbares Basisverzeichnis denkbar (siehe Konfiguration und Offene Fragen).
- **Scope-Ausschlüsse:**
  1. Keine Änderung an der fachlichen Update-Logik selbst (Release-Ermittlung via `IGitHubReleaseClient`, Paketvalidierungsregeln, Installations-/Reload-Koordination) — nur die Bereitstellung der Arbeitsverzeichnisse und die Fehlerübersetzung.
  2. Andere `Path.GetTempPath()`-Nutzer außerhalb des Plugin-Update-Pfads (z. B. `ExportService`, `AdminExportsController`) sind nicht Teil dieser Korrektur; sie zeigen dasselbe strukturelle Muster, sind aber vom Issue nicht erfasst.
  3. Keine Änderung am Auslösemechanismus (manueller Button, Startprüfung, Fehlerdarstellung im UI-Flow) — nur die Qualität der resultierenden Fehlermeldung.

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs` (Zeile 171): `workRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-download", <guid>)` — Download-Arbeitsverzeichnis.
- `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs` (Zeile 29): `extractRoot = Path.Combine(Path.GetTempPath(), "rezepte-plugin-package", <guid>)` — Extraktionsverzeichnis der Paketvalidierung.
- `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs` (Zeile 245): `backupPath = Path.Combine(Path.GetTempPath(), "rezepte-plugin-backup", <guid>)` — Sicherung des bisherigen Plugin-Verzeichnisses für die Wiederherstellung.
- `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckErrors.cs`: voraussichtlich neue fachliche Fehlerkonstante für „Arbeitsverzeichnis nicht verfügbar/nicht anlegbar", damit die UI sie wie die übrigen bekannten Fehler lokalisiert darstellen kann.
- `Rezepte.Web/Components/Settings/PluginSettings.razor` (`MapCheckError`, Zeilen 389–404) und `Rezepte.Web/Resources/UiStrings.resx`: Zuordnung der neuen Fehlerkonstante zu einer lokalisierten Meldung.
- `Rezepte.Web/Configuration/PluginUpdateOptions.cs` (Konfigurationssection `PluginUpdates`): optionaler Ort für ein konfigurierbares Arbeitsverzeichnis — abhängig von der Entscheidung in den Offenen Fragen.
- Ggf. neue Hilfskomponente (z. B. ein Workspace-/Temp-Verzeichnisprovider) zur zentralen, beschreibbarkeitsgeprüften Beschaffung des Basisverzeichnisses — Annahme, konkrete Form in der Planung festzulegen.
- Tests: `Rezepte.Tests/Services/Import/PluginUpdateServiceTests*` (`_CheckSource`, `_Preconditions`, `_Serialization`, gemeinsame `PluginUpdateServiceTestsBase`), `PluginPackageValidatorTests`, `PluginPackageInstallerTests`, `PluginSettingsComponentTests` (u. a. Assert auf „Prüfung fehlgeschlagen.", Zeilen 143/182).

## Implementierungsansatz

- Zentrale Beschaffung des Arbeits-Basisverzeichnisses statt direktem `Path.GetTempPath()` an den drei Fundstellen: primär das System-Temp-Verzeichnis mit Beschreibbarkeitsnachweis (proberhaftes Anlegen), Fallback auf ein garantiert beschreibbares Verzeichnis — naheliegend unterhalb des ohnehin verwalteten und beschriebenen `{ContentRootPath}/plugins` (vgl. `PluginDirectoryRegistry`, Zeile 20) — oder auf einen konfigurierten Pfad.
- Die Verzeichnisanlage erfolgt weiter über `Directory.CreateDirectory` (legt Zwischenebenen selbst an); der kritische Punkt ist die Wahl und Verfügbarkeit des Basisverzeichnisses, nicht die Einzelverzeichniserzeugung.
- Scheitert die Bereitstellung des Arbeitsverzeichnisses, wird die Laufzeit-Exception in einen fachlichen Fehler (neue Konstante in `PluginUpdateCheckErrors`) übersetzt, der über `PluginUpdateCheckResult.Failed` → `MapCheckError` → `Localizer` als lokalisierte Meldung ausgegeben wird; die technische Exception bleibt im `ILogger`-Log mit Stacktrace erhalten.
- Abhängigkeiten: `IHostEnvironment.ContentRootPath` (bereits von `PluginDirectoryRegistry` genutzt) bzw. `PluginUpdateOptions` (Section `PluginUpdates`, gebunden in `ServiceCollectionExtensions`, Zeile 44); `ILogger` für die Fehlerprotokollierung.
- Cleanup-Verhalten (best-effort `Directory.Delete` mit Warnlog in `finally`) bleibt unverändert.

## Konfiguration

Das Akzeptanzkriterium verlangt „ein konfigurierbares **bzw.** garantiert beschreibbares Basisverzeichnis" — es ist also eine der beiden Varianten ausreichend. Naheliegende Optionen:

- Kein neuer Konfigurationsschlüssel: primär `Path.GetTempPath()` (respektiert `TMPDIR`/`TMP`/`TEMP`) mit Fallback auf `{ContentRootPath}/plugins`-Arbeitsverzeichnis.
- Optionaler Schlüssel in der Section `PluginUpdates` (z. B. `WorkDirectory`) in `PluginUpdateOptions`, der bei Bedarf das Basisverzeichnis explizit festlegt.

Die Entscheidung ist Teil der Planung (siehe Offene Fragen).

## Akzeptanzkriterien

1. Die Update-Prüfung pro Quelle läuft ohne die genannte Fehlermeldung „Could not find file '/tmp/rezepte-plugin-download'" durch; die Plugin-Aktualisierung funktioniert wieder wie vorgesehen.
2. Temporäre Arbeitsverzeichnisse werden ohne Annahme einer bereits vorhandenen Verzeichnisstruktur angelegt oder ein konfigurierbares bzw. garantiert beschreibbares Basisverzeichnis wird genutzt.
3. Kann das Arbeitsverzeichnis nicht angelegt werden, resultiert eine aussagekräftige, fachliche (lokalisierte) Fehlermeldung statt der rohen Laufzeit-Exception.

## Offene Fragen

1. **Umgebung der betroffenen Installation:** Docker-Container, systemd-Dienst oder andere Bereitstellung? Der `/tmp`-Pfad legt Linux nahe, ist aber nicht bestätigt.
2. **Temp-Verfügbarkeit:** Ist `/tmp` im betroffenen System vorhanden und für den App-Prozess beschreibbar? Sind `TMPDIR`/`TMP`/`TEMP` gesetzt — ggf. auf ein nicht existierendes oder schreibgeschütztes Verzeichnis?
3. **Fehlerstelle:** Vollständige Exception inkl. Stacktrace aus dem Anwendungslog — zur Bestätigung, ob der Fehler beim Anlegen des Arbeitsverzeichnisses, beim Download (`File.Create` in `GitHubReleaseClient.DownloadAssetAsync`, Zeile 73), bei der Validierung oder der Installation auftritt. Hinweis: `Directory.CreateDirectory` wirft typischerweise `DirectoryNotFoundException` („Could not find a part of the path"), nicht `FileNotFoundException` („Could not find file"); die genaue Stelle ist aus der Meldung allein nicht eindeutig ableitbar.
4. **Betroffene Version:** Release-Stand der Installation (u. a. vor oder nach der .NET-11-Migration; das Repo steht auf `net11.0` / SDK `11.0.100`).
5. **Reproduzierbarkeit und Umfang:** Tritt der Fehler bei jedem Klick und jeder Quelle auf oder nur sporadisch? Betrifft er nur eine bestimmte Quelle oder alle konfigurierten Quellen (und ggf. auch die Startprüfung)?
6. **Basisverzeichnis-Strategie:** Reicht der Fallback auf den verwalteten Plugin-Root (`{ContentRootPath}/plugins`) aus, oder soll zusätzlich ein konfigurierbarer Pfad in `PluginUpdateOptions` eingeführt werden?
