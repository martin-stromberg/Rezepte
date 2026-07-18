# Code-Review: Automatischer Download neuer Plugins

Status: Befunde vorhanden

## Befunde

1. **Hoch - Serverautorisierung faellt bei fehlendem HttpContext offen aus**

   Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:181`

   `EnsureAdmin()` gibt ohne Fehler zurueck, wenn `httpContextAccessor?.HttpContext?.User` null ist. Damit sind `GetSourcesAsync`, `SaveSourceAsync`, `SetSourceEnabledAsync` und `DeleteSourceAsync` nur dann geschuetzt, wenn zufaellig ein HTTP-Kontext vorhanden ist. Das widerspricht dem Plan, der serverseitige Autorisierung als massgeblich fordert, und ist fuer Blazor-/Serviceaufrufe riskant, weil ein kontextloser Aufruf administrative Pluginquellen inklusive privater Quellen anlegen, aktivieren oder loeschen kann. Die Methode sollte fail-closed arbeiten und nur einen authentifizierten Admin-Principal akzeptieren; Tests sollten den Null-HttpContext-Fall explizit abdecken.

2. **Hoch - Fehlgeschlagene Releases werden dauerhaft nie wieder verarbeitet**

   Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateService.cs:75`

   Bereits vorhandene Release-Datensaetze mit `ValidationFailed`, `DownloadFailed` oder `InstallFailed` werden immer uebersprungen. Damit wird eine temporaere Stoerung, z. B. Netzwerk-/Rate-Limit-Fehler, ein unvollstaendiger Download oder ein inzwischen korrigiertes Asset mit gleicher Release-/Asset-ID, dauerhaft blockiert. Der Plan fordert, dass fehlgeschlagene Releases nicht endlos im selben Lauf erneut verarbeitet werden, aber ein erneuter Versuch bei spaeterem Start oder administrativer Konfigurationsaenderung moeglich bleibt. Der aktuelle Test `CheckForUpdatesAsync_ShouldSkipAlreadyFailedReleaseVersion` schreibt dieses falsche Dauerverhalten fest. Noetig ist eine Retry-/Reset-Strategie, z. B. pro Startlauf, nach Aenderung der Quelle, nach PAT-Rotation oder nach expliziter administrativer Freigabe.

3. **Mittel - Validierungs-Discovery laedt Plugin-Assemblies dauerhaft aus dem temporaeren Verzeichnis**

   Dateien: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidator.cs:74`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:145`, `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:314`

   Die Paketvalidierung ruft `pluginManager.DiscoverFromDirectory(extractRoot)` auf. Dieser Pfad laedt die Assemblies mit einem nicht sammelbaren `AssemblyLoadContext` und instanziiert Plugin-Typen. Dadurch bleiben Assemblies aus dem temporaeren Entpackverzeichnis im Prozess geladen, obwohl `PluginUpdateService` das Verzeichnis danach loeschen will. Bei wiederholten Startlaeufen kann das Speicher binden, Datei-/DLL-Locks verursachen und die Validierung schlechter von der spaeteren produktiven Installation trennen. Fuer die temporaere Lauffaehigkeitspruefung sollte ein separater, collectible Validierungs-LoadContext verwendet oder die Discovery so erweitert werden, dass sie temporaere Loads nach der Pruefung wieder freigibt.

4. **Mittel - Rollback kann selbst fehlschlagen und den Pluginbestand unvollstaendig zuruecklassen**

   Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs:41`

   Im Fehlerpfad werden neu kopierte Ziele geloescht und Backups wiederhergestellt, aber diese Restore-Operationen sind nicht abgesichert. Wenn `Directory.Delete` oder `CopyDirectory` waehrend des Rollbacks fehlschlaegt, verlaesst die Methode den Catch vor `pluginManager.InitializeAsync(CancellationToken.None)` und vor weiteren Backups. Das widerspricht dem Plan, wonach bei Austausch- oder Reloadfehlern der vorherige Bestand wiederhergestellt und aktiv bleiben muss. Der Rollback sollte robust pro Verzeichnis arbeiten, Restore-Fehler separat loggen und sicherstellen, dass der PluginManager nach bestmoeglicher Wiederherstellung neu initialisiert wird; dazu fehlen fokussierte Installer-/Rollbacktests.

## Fehlende Tests

- Kein Test fuer `PluginSettingsService` mit fehlendem `HttpContext` bzw. nicht authentifiziertem Principal; genau dieser Fall ist aktuell offen.
- Kein Test fuer spaetere Retry- oder Reset-Semantik fehlgeschlagener Releases. Der vorhandene Test in `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs:35` deckt nur dauerhaftes Skippen ab.
- Kein Test fuer Installer-Rollback bei Kopier-, Loesch- oder Reloadfehlern.
- Kein Integrationstest fuer temporaere Discovery, der sicherstellt, dass Validierungsassemblies nicht dauerhaft aus dem Temp-Verzeichnis geladen bleiben.

## Verifikation

`dotnet test --no-restore` wurde ausgefuehrt. Ergebnis: Build/Testlauf startet, 159 Tests erfolgreich, 1 Test fehlgeschlagen: `Rezepte.Tests.Deployment.DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks`. Der Fehler entsteht beim `dotnet publish` der externen Plugin-Projekte `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl`, deren Referenz auf `Rezepte.Web` im publish-Kontext nicht aufgeloest wird. Zusaetzlich meldet NuGet eine bekannte hoch eingestufte Sicherheitswarnung fuer `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.
