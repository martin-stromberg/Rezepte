# Plan-Review: Automatischer Download neuer Plugins

## Status

**Nicht vollstaendig umgesetzt.** Die Nacharbeit aus `review.1.md` und `review-code.1.md` ist teilweise eingearbeitet, aber mehrere Anforderungen des Plans bleiben offen.

## Befunde

### 1. Hoch - Reload ist nicht als eigener, nachvollziehbarer Zustand persistiert

`PluginSourceRelease` kennt weiterhin nur Download-, Validierungs- und Installationsstatus sowie Zeitpunkte. Ein Reloadstatus und ein Reloadfehler fehlen. `PluginPackageInstaller` ruft nach dem Dateiaustausch direkt `PluginManager.InitializeAsync` auf. Bei einem Reloadfehler wird zwar ein Wiederherstellungsversuch ausgefuehrt, der Release-Datensatz wird danach aber nicht mit einem eigenen Reloadfehler aktualisiert. Damit sind die Statusanforderungen aus den Umsetzungsschritten 1, 4 und 5 nicht vollstaendig erfuellt.

### 2. Hoch - Plugin-Lebenszyklus beim produktiven Reload bleibt unkoordiniert

Die temporäre Validierung verwendet inzwischen einen collectible `AssemblyLoadContext`, was den frueheren Befund abschwaecht. Der produktive Reload laedt jedoch weiterhin neue Assemblies, ohne laufende Import-Handler oder die bereits geladenen nicht sammelnden `AssemblyLoadContext`s kontrolliert abzulösen. Ein Dateiaustausch mit anschliessendem `InitializeAsync` erfuellt daher die geforderte Lebenszykluskoordination nicht vollstaendig.

### 3. Hoch - GitHub-Rate-Limits und kontrollierte Wiederholung fehlen

`GitHubReleaseClient` behandelt nur `404` speziell; alle anderen HTTP-Fehler laufen ueber `EnsureSuccessStatusCode`. Es gibt keine Auswertung von `429` bzw. `Retry-After`, keinen Retry-/Backoff-Mechanismus und keinen dedizierten persistierten Rate-Limit-Status. Die Timeout- und Cancellation-Unterstuetzung des `HttpClient` ist vorhanden, reicht fuer die im Plan geforderte kontrollierte Fehlerbehandlung aber nicht aus.

### 4. Mittel - Rollback ist nicht in allen Austauschfehlern robust

Der Installer sichert bestehende Pluginverzeichnisse und versucht nach einem Fehler eine Wiederherstellung. Ein neu angelegtes Ziel wird jedoch erst nach erfolgreichem Kopieren in `installedTargets` aufgenommen. Scheitert das Kopieren eines neuen Plugins teilweise, kann ein unvollständiges Zielverzeichnis deshalb ausserhalb der anschliessend bereinigten Liste verbleiben. Ausserdem wird ein fehlgeschlagener Restore nur geloggt; der Dienst kann den vorherigen Bestand dann nicht garantieren.

### 5. Mittel - Abnahmetests decken die offenen Verträge nicht vollstaendig ab

Vorhanden sind fokussierte Tests fuer Admin-Zugriff ohne `HttpContext`, Retry eines fehlerhaften Release-Datensatzes, variable ZIP-Namen, einen ZIP-Pfadueberlauf, Mehrfach-Pluginverzeichnisse und einen Reloadfehler mit einfachem Rollback. Es fehlen weiterhin Tests fuer:

- GitHub-Authentifizierung, `429`/`Retry-After`, Timeout, Cancellation und PAT-Ausschluss aus Requests und Logs;
- absolute Pfade, unerlaubte Inhalte und beschaedigte ZIPs;
- persistierte Reloadstatus und Reloadfehler;
- laufende Handler, nicht sammelnde LoadContexts und produktives Reloadverhalten;
- Kopier-/Loeschfehler waehrend des Rollbacks sowie unvollstaendige neue Ziele;
- einmalige Hosted-Service-Ausfuehrung, parallele Laeufe und alle Statusuebergaenge;
- vollstaendige UI-/Integrationspruefung fuer Administratorgrenzen und Geheimnisausschluss.

## Erledigte Nacharbeit

- `PluginSettingsService.EnsureAdmin` weist fehlenden oder nicht authentifizierten Kontext jetzt ab und prueft einen Admin-Principal serverseitig.
- Bereits gespeicherte Fehlerstatus `ValidationFailed`, `DownloadFailed` und `InstallFailed` werden bei einem spaeteren Startlauf wieder auf `Pending` gesetzt und erneut verarbeitet.
- Die temporaere Discovery fordert einen collectible LoadContext an und gibt ihn nach der Pruefung zum Entladen frei.
- Der Installer versucht nach einem Reloadfehler den Backupbestand wiederherzustellen und initialisiert den PluginManager erneut.

## Verifikation

Ausgefuehrt:

```text
dotnet test Rezepte.Tests\\Rezepte.Tests.csproj --no-restore --filter "FullyQualifiedName~Rezepte.Tests.Services.Import" --logger "console;verbosity=minimal"
```

Ergebnis: **37 Tests bestanden, 0 fehlgeschlagen**.

Der vollstaendige Lauf ergibt **162 von 163 Tests bestanden**. Der einzige Fehlschlag ist `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks`; beim Linux-Publish koennen die externen Projekte `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` die Referenz auf `Rezepte.Web` nicht aufloesen. Zusaetzlich wird die bekannte `NU1903`-Warnung zu `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 ausgegeben.

Es wurden keine produktiven Codeaenderungen vorgenommen.
