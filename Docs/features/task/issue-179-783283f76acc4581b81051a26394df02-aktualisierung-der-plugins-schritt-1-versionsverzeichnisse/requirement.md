# Übersetzte Anforderung: Versionsverzeichnisse für Plugin-Installationen (Issue #179, Schritt 1)

## Fachliche Zusammenfassung

Die Installation von Plugin-Paketen aus GitHub-Pluginquellen (`PluginUpdateService` → `PluginPackageInstaller`) legt neue Pluginversionen künftig in eigenen, aus dem Release-Tag abgeleiteten Versionsverzeichnissen unterhalb des Plugin-Stammverzeichnisses `plugins/` ab, statt das bisherige Plugin-Verzeichnis an Ort und Stelle zu ersetzen. Beim koordinierten Austausch über `IPluginManager.CoordinateReloadAsync` werden die Verzeichnisse der Vorgängerversionen entfernt; nicht löschbare Altverzeichnisse werden persistent als zu löschen markiert, von der Plugin-Erkennung ausgeschlossen und beim nächsten Anwendungsstart bzw. beim nächsten koordinierten Austausch nachbereinigt. Das bestehende Fehlerverhalten (Rücksicherung des vorherigen Stands über `PluginSourceReleaseStatus.InstallFailed`/`ReloadFailed` und Backup-Rollback) bleibt erhalten und wird auf das Versionsverzeichnis-Modell angepasst.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine Release-Installation einer GitHub-Pluginquelle — d. h. das ZIP-Asset eines GitHub-Releases mit einem oder mehreren validierten Plugin-Verzeichnissen (`PluginPackageValidationResult.PluginDirectories`), das über `PluginUpdateService.ProcessSourceAsync` / `DownloadValidateInstallAsync` heruntergeladen, validiert und installiert wird.
- **Auslösende Ereignisse:**
  - Startprüfung aktivierter und vertrauensbestätigter `PluginSource`-Einträge beim Anwendungsstart über `PluginUpdateHostedService` → `IPluginUpdateService.CheckForUpdatesAsync`.
  - Interner Bereinigungsauslöser: beim Anwendungsstart (vor bzw. im Rahmen der Plugin-Erkennung durch `PluginManager.InitializeAsync`) sowie bei jedem koordinierten Austausch werden verwaiste, als zu löschen markierte Verzeichnisse entfernt.
- **Erwartete Wirkung:**
  - Nach erfolgreicher Installation liegt der wirksame Pluginbestand in versionsbezogenen Verzeichnissen unter `plugins/`; die Plugin-Erkennung (`PluginManager.DiscoverPlugins`/`DiscoverExternalPlugins`) lädt ausschließlich die jeweils aktuelle Version eines Plugins.
  - Verzeichnisse vorheriger Versionen sind entfernt oder — bei nicht löschbaren Handles — persistent markiert, aus der Erkennung ausgeschlossen und werden beim nächsten Start/Austausch bereinigt. Sie sind damit nicht Teil des wirksamen Bestands (kein Status `Loaded`, keine Handler).
  - Plugins, die im neuen Release nicht mehr enthalten sind, erhalten in den Plugin-Einstellungen den Status `PluginStatus.Missing` (bisheriger Mechanismus über `SynchronizeSettingsAsync`).
  - Bei fehlgeschlagener Installation oder fehlgeschlagenem Austausch bleibt der vorherige wirksame Bestand erhalten bzw. wird wiederhergestellt; der `PluginSourceRelease`-Status verhält sich wie bisher (`InstallFailed` vs. `ReloadFailed` via `PluginPackageInstallException`).
- **Umgang mit fehlenden Objekten:**
  - Versionsverzeichnis der neuen Version: wird bei jeder Installation neu erzeugt (Ableitung aus `PluginSourceRelease.ReleaseTag`).
  - Löschmarkierung für nicht löschbare Altverzeichnisse: wird im Dateisystem erzeugt, sobald ein `Directory.Delete` auf ein Altverzeichnis scheitert (`IOException`/`UnauthorizedAccessException`); konkrete Form (z. B. Marker-Datei im Verzeichnis, Umbenennung mit Suffix) ist im Rahmen der Umsetzung festzulegen.
  - Altverzeichnisse ohne Markierung, die nicht zum neuen Bestand gehören: werden beim koordinierten Austausch verworfen (gelöscht).
- **Scope-Ausschlüsse:**
  - Mitgelieferte Plugins aus dem Anwendungs-Publish (`plugins/<Projektname>/`, z. B. `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`) sind von der Versionsverzeichnis- und Löschmarkierungs-Behandlung ausgenommen; ihre Verzeichnisse werden nicht versioniert, nicht gelöscht und nicht markiert.
  - Kein manueller Update-Trigger und kein neuer UI-Ablauf: Auslöser bleibt die bisherige Startprüfung.
  - Kein Mehrversionen-Betrieb: Es ist kein paralleles Laden mehrerer Versionen desselben Plugins vorgesehen; markierte Altverzeichnisse sind unwirksam, nicht ladbar.
  - Keine Änderung am Validierungsverhalten des ZIP-Pakets (`PluginPackageValidator`) über die für das neue Verzeichnismodell notwendigen Anpassungen hinaus.
  - Keine Konfigurierbarkeit des Namensschemas oder des Löschmarkierungs-Mechanismus.

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs` — zentrale Umstellung: statt In-Place-Ersetzen (`Directory.Delete(target)` + `CopyDirectory`) Anlage versionsbezogener Zielverzeichnisse, Entfernen bzw. Markieren der Altverzeichnisse, angepasstes Backup-/Rollback-Verhalten.
- `Rezepte.Web/Services/Import/Plugins/IPluginPackageInstaller.cs` — voraussichtlich Signaturerweiterung von `InstallAsync`/`InstallWithReloadTrackingAsync`, da die Ableitung der Versionsverzeichnisnamen den Release-Tag (oder eine äquivalente Versionsangabe) erfordert.
- `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs` — Übergabe des Release-Tags (`record.ReleaseTag`) an den Installer.
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs` — Plugin-Erkennung: Ausschluss als zu löschen markierter Verzeichnisse aus `DiscoverExternalPlugins`/`GetPluginAssemblyCandidates`, korrekte Behandlung verschachtelter Versionsverzeichnisse, ggf. Auswahl der wirksamen Version bei mehreren vorhandenen Versionsverzeichnissen desselben Plugins; Bereinigung markierter Verzeichnisse beim Start (`InitializeCoreAsync`) und im koordinierten Austausch (`CoordinateReloadAsync`).
- `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs` — kein eigenes Verhalten, aber Startpfad, über den die Startbereinigung wirksam wird.
- Neu: Hilfskomponente für Verzeichnisnamens-Ableitung und Löschmarkierung (z. B. eigenständige Klasse/Helper im `Plugins`-Ordner), falls nicht in `PluginPackageInstaller`/`PluginManager` integriert — Festlegung im Rahmen der Umsetzung.
- `PluginStatus`/`PluginSourceReleaseStatus` — voraussichtlich unverändert; `Missing`, `InstallFailed`, `ReloadFailed` decken die geforderten Zustände ab.
- Tests unter `Rezepte.Tests/` — neue und angepasste Tests für Installation in Versionsverzeichnisse, Markierung nicht löschbarer Altverzeichnisse, Ausschluss markierter Verzeichnisse aus der Erkennung, Bereinigung beim Start/Austausch, `Missing`-Status für entfallene Plugins und Rollback im neuen Verzeichnismodell.

## Implementierungsansatz

- Erweiterungspunkt ist der bestehende Austausch-Hook: `PluginPackageInstaller.InstallWithReloadTrackingAsync` führt den Dateiaustausch bereits innerhalb von `IPluginManager.CoordinateReloadAsync` aus (Sperrung neuer Imports über `_lifecycleLock`, anschließendes `InitializeCoreAsync`). Das neue Verzeichnismodell wird in diesen Callback verlagert: neue Pluginverzeichnisse werden unter einem aus dem Release-Tag abgeleiteten Namen kopiert, danach werden die Verzeichnisse der bisherigen Versionen gelöscht.
- Der Release-Tag muss vom `PluginUpdateService` (aus `PluginSourceRelease.ReleaseTag`) bis in den Installer gereicht werden — `IPluginPackageInstaller` erhält entsprechend einen zusätzlichen Parameter oder ein Paket-Descriptor-Objekt.
- Nicht löschbare Altverzeichnisse (`IOException`/`UnauthorizedAccessException` beim `Directory.Delete`) erhalten eine persistente Dateisystem-Markierung; `PluginManager` schließt markierte Verzeichnisse beim Aufbau der DLL-Kandidaten (`DiscoverExternalPlugins`, `GetPluginAssemblyCandidates`, `GetPluginRoots`) aus und versucht die Bereinigung zu Beginn von `InitializeCoreAsync` bzw. innerhalb des koordinierten Austauschs erneut.
- Rollback: statt des bisherigen Temp-Backups (`backupRoot` unter `Path.GetTempPath()`) kann der vorherige Stand aus den noch vorhandenen Altversionsverzeichnissen wiederhergestellt werden — das genaue Rollback-Modell (Backup beibehalten vs. Altversion als Rücksicherung) ist im Rahmen der Umsetzung zu entscheiden; das beobachtbare Verhalten (vorheriger Bestand bleibt bzw. wird wieder wirksam, Status `InstallFailed`/`ReloadFailed` via `PluginPackageInstallException`) bleibt unverändert.
- Abgrenzung Publish-Plugins vs. quelleninstallierte Plugins: Die Erkennung muss Publish-Plugin-Verzeichnisse (`plugins/<Projektname>/` aus dem Anwendungs-Publish) von versionsverwalteten Verzeichnissen unterscheiden können — z. B. über das Namensschema oder ein Verwaltungsverzeichnis. Diese Unterscheidung ist Teil der Umsetzung.
- Abhängigkeiten: `PluginManager` (Erkennung, `SynchronizeSettingsAsync` für `Missing`-Status), `IPluginManager.CoordinateReloadAsync` (Koordination), `PluginPackageValidator`/`PluginPackageValidationResult` (liefert Quellverzeichnisse), `RezepteDbContext`/`PluginSourceRelease` (Status-Tracking bleibt unverändert).

## Konfiguration

Keine Konfiguration vorgesehen. Namensschema der Versionsverzeichnisse und die Form der Löschmarkierung werden fest im Code verankert.

## Offene Fragen

- **Namensschema (im Rahmen der Umsetzung festzulegen):** Flache Versionsverzeichnisse unter `plugins/` (z. B. `plugins/<PluginName>@<ReleaseTag>/`) oder zweistufig (z. B. `plugins/<PluginName>/<ReleaseTag>/`)? Zu beachten: `GetPluginAssemblyCandidates`/`DiscoverExternalPlugins` betrachten heute nur direkte Unterordner — eine zweistufige Struktur erfordert eine tiefere bzw. gezieltere Kandidatenermittlung.
- **Form der Löschmarkierung (im Rahmen der Umsetzung festzulegen):** Marker-Datei im Altverzeichnis, Umbenennung (Suffix/Präfix) oder ein zentrales Löschverzeichnis? Die Markierung muss auch dann wirken, wenn das Verzeichnis selbst nicht mehr beschreibbar/umbenennbar ist (Handle-Szenario) — Umbenennung kann in diesem Fall ebenfalls scheitern.
- **Identifikation der „eigenen" verwalteten Verzeichnisse:** Woran erkennt der Installer/die Bereinigung zuverlässig, welche Unterverzeichnisse von `plugins/` zu quelleninstallierten Plugins gehören (und damit versioniert/gelöscht werden dürfen) und welche Publish-Plugins sind? Namentliche Kollisionen (Plugin-Name im Release vs. Publish-Ordnername) sind abzusichern.
- **Mehrere Pluginquellen/Plugins pro Release:** Ein Release kann mehrere Plugin-Verzeichnisse enthalten (`pluginDirectories.Count > 1`). Gilt der Release-Tag je Quelle für alle enthaltenen Plugins gemeinsam, und was passiert, wenn zwei Quellen dasselbe Plugin ausliefern?
- **Rollback-Detail:** Soll das bestehende Temp-Backup (`backupRoot`) zusätzlich zum Altversionsverzeichnis beibehalten werden, oder ersetzt das Altversionsverzeichnis das Backup? Bei Letzterem ist zu klären, wie ein Altverzeichnis, das bereits als zu löschen markiert war, wieder wirksam gemacht wird.
- **Bereinigungszeitpunkt „nächster koordinierter Austausch":** Erfolgt die Markierungs-Bereinigung im selben `CoordinateReloadAsync`-Lauf vor dem Austausch, und gilt „nächster Anwendungsstart" als Start vor oder nach der ersten Plugin-Erkennung?
