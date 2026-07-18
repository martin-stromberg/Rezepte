# Code-Review: Automatischer Download neuer Plugins

Status: Befunde vorhanden

## Befunde

1. **Hoch - Produktiver Reload ersetzt den Plugin-Lebenszyklus nicht kontrolliert**

   Dateien: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs:38`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:93`

   Nach dem Dateiaustausch ruft der Installer weiterhin direkt `PluginManager.InitializeAsync` auf. Der produktive Pfad laedt die neuen Assemblies anschliessend wieder mit nicht sammelbaren `AssemblyLoadContext`s, ohne laufende Import-Handler zu sperren oder alte LoadContexts abzulösen. Dadurch koennen bestehende Handler noch mit alten Typen laufen, waehrend der Dateibestand bereits ersetzt ist; zusaetzlich bleiben alte Assemblies im Prozess geladen. Die zweite Runde hat die temporaere Validierungs-Discovery verbessert, aber der eigentliche Reload-Vertrag aus dem Plan bleibt fuer produktive Installationen offen.

2. **Hoch - Reloadfehler werden nicht als eigener persistenter Zustand erfasst**

   Dateien: `Rezepte.Web/Entities/PluginSourceRelease.cs:14`, `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs:5`, `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs:132`

   `PluginSourceRelease` und `PluginSourceReleaseStatus` modellieren weiterhin nur Download, Validierung, Installation und generisches `InstallFailed`. Der Reload ist Teil von `packageInstaller.InstallAsync`, wird aber nicht mit eigenem Status, Zeitpunkt oder Fehlertext persistiert. Wenn `PluginManager.InitializeAsync` nach erfolgreichem Kopieren fehlschlaegt, landet der Release-Datensatz nur als `InstallFailed`; aus der Historie ist nicht nachvollziehbar, ob der Dateiaustausch oder der anschliessende Reload gescheitert ist. Der Plan fordert ausdruecklich Download-, Validierungs-, Installations- und Reloadstatus sowie nachvollziehbare Zwischen- und Fehlerzustaende.

3. **Mittel - Teilweise kopierte neue Pluginziele koennen nach einem Austauschfehler liegen bleiben**

   Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs:34`

   Neue Zielverzeichnisse werden erst nach erfolgreichem `CopyDirectory(sourceDirectory, target)` in `installedTargets` aufgenommen. Scheitert das Kopieren eines neuen Plugins nach `Directory.CreateDirectory(target)` oder nach einzelnen Dateien, kennt der Rollback dieses Ziel nicht und loescht es nicht. Damit kann ein unvollstaendiges Pluginverzeichnis im aktiven `plugins`-Ordner verbleiben. Das verletzt den Rollback-Vertrag, nach einem Austauschfehler den vorherigen Bestand wiederherzustellen bzw. neue fehlerhafte Ziele zu entfernen.

4. **Mittel - GitHub-Rate-Limits und kontrollierte Wiederholung sind weiterhin nicht implementiert**

   Dateien: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs:23`, `Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs:51`

   Der GitHub-Client behandelt nur `404` gesondert und nutzt fuer alle anderen HTTP-Fehler `EnsureSuccessStatusCode`. Es gibt keine Auswertung von `429`, `403`-Rate-Limit-Antworten oder `Retry-After`, keinen Backoff und keinen dedizierten persistierten Rate-Limit-Status. Da `PluginUpdateService` fehlgeschlagene Releases beim naechsten Start wieder auf `Pending` setzt, koennen temporaere GitHub-Fehler zwar spaeter erneut versucht werden, sie werden aber nicht kontrolliert klassifiziert oder gedrosselt. Der Plan verlangt kontrollierte Behandlung von API-Fehlern, Rate-Limits, Timeouts und Wiederholungen.

## Fehlende Tests

- Kein Test fuer produktive Reload-Koordination mit laufenden Handlern oder alten nicht sammelbaren `AssemblyLoadContext`s.
- Kein Test fuer persistierte Reloadstatus, Reloadzeitpunkt und Reloadfehler.
- Kein Installer-Test fuer Kopierfehler bei einem neuen Pluginziel, bei dem ein teilweise kopiertes Zielverzeichnis zurueckbleiben kann.
- Kein GitHub-Clienttest fuer `429`/`Retry-After`, Rate-Limit-`403`, Timeout, Cancellation, private Authentifizierung und PAT-Ausschluss aus Logs.
- Kein breiter Validator-Test fuer absolute Pfade, unerlaubte Inhalte und beschaedigte ZIPs; abgedeckt sind aktuell Pfadueberlauf und Mehrfach-Pluginverzeichnisse.
- Kein Integrationspfad fuer UI-State und Logs, der sicherstellt, dass Secretwerte nie ausgegeben werden.

## Erledigte Befunde aus Runde 1

- `PluginSettingsService.EnsureAdmin` weist fehlenden `HttpContext`, nicht authentifizierte Benutzer und Nicht-Admins inzwischen ab; ein Test fuer fehlenden `HttpContext` ist vorhanden.
- Bereits fehlgeschlagene Releases werden bei einem spaeteren Startlauf erneut auf `Pending` gesetzt und verarbeitet.
- Die temporaere Validierungs-Discovery verwendet nun einen collectible LoadContext und speichert fuer diesen Pfad keinen `HandlerType`.
- Der Installer versucht nach einem Reloadfehler Backupbestand und PluginManager wiederherzustellen; der Rollback ist aber noch nicht in allen Austauschfehlern vollstaendig.

## Verifikation

Nicht erneut ausgefuehrt. Laut `test-results.md` wurden die relevanten Import-/Plugin-Tests zuletzt mit 37/37 bestanden ausgefuehrt. Der vollstaendige Lauf hatte 162/163 bestandene Tests; der verbleibende Fehlschlag betrifft `Rezepte.Tests.Deployment.DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks` beim Publish externer Pluginprojekte.

Es wurden keine produktiven Codeaenderungen vorgenommen.
