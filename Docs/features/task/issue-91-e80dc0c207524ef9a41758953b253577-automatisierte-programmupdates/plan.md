# Umsetzungsplan - Automatisierte Programmupdates

## Zielbild

Die Webanwendung erhaelt eine klar gekapselte Integration fuer `msTools.Updater`. Vor jeder Installation einer neuen Version wird synchron ein Update-Backup erstellt. Die Installation darf nur fortfahren, wenn der Export vollstaendig und erfolgreich in ein per `appsettings.json` konfiguriertes Backup-Verzeichnis geschrieben wurde und die Retention fehlerfrei oder kontrolliert angewendet werden konnte.

Da die konkrete lokale API von `msTools.Updater` in der Bestandsaufnahme nicht verfuegbar war, wird die Umsetzung zweistufig geplant:

1. Eine robuste interne Adapter-Grenze wird in `Rezepte.Web` eingefuehrt.
2. Die reale externe Updater-API wird dahinter angebunden, sobald Paketname, Registrierungs-API und Pre-Install-Semantik verifiziert sind.

Die externe API, die spaeter konkret angebunden werden muss, ist:

- Paket- oder Projektname von `msTools.Updater`
- DI-/Host-Registrierung des Updaters
- Konfigurationsmodell des Updaters
- Release-Quelle und Update-Check-Mechanismus
- Pre-Install-Hook inklusive Signatur
- Semantik fuer async Warten, Cancellation und Installationsabbruch bei Fehlern

## Nicht-Ziele

- Der vorhandene Plugin-Updater wird nicht fuer Web-App-Updates zweckentfremdet.
- Release-Ermittlung, Download und Austausch der Web-App-Binaries werden nicht nachgebaut, sofern `msTools.Updater` diese Aufgaben bereitstellt.
- Der bestehende Background-Job-Export wird nicht als Pre-Install-Ablauf verwendet, weil der Updater synchron auf das Ergebnis warten muss.

## Geplante Aenderungen

### 1. Konfiguration fuer Update-Backups

Neue Options-Klasse unter `Rezepte.Web/Configuration`, z. B. `UpdateBackupOptions`:

- `Directory`
- `RetentionCount`
- `IncludeImages`
- `IncludePdf`
- optional `SystemInitiatorUserId`

Neue Section in `Rezepte.Web/appsettings.json`, z. B.:

```json
"UpdateBackups": {
  "Directory": "update-backups",
  "RetentionCount": 5,
  "IncludeImages": true,
  "IncludePdf": false,
  "SystemInitiatorUserId": "system-update-backup"
}
```

Registrierung in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` ueber `services.Configure<UpdateBackupOptions>(configuration.GetSection("UpdateBackups"))`.

Validierungsregeln:

- `Directory` darf nicht leer sein.
- Relative Pfade werden gegen `IHostEnvironment.ContentRootPath` aufgeloest.
- Vollpfade sind erlaubt und werden geloggt.
- `RetentionCount` muss mindestens `1` sein.
- Fehlerhafte Konfiguration verhindert die Installation.

### 2. Systemischer Export-Initiator

Der automatische Update-Export darf nicht von einem angemeldeten Admin-Benutzer abhaengen. Dafuer wird ein expliziter systemischer Initiator eingefuehrt.

Geplante Variante:

- `UpdateBackupOptions.SystemInitiatorUserId` liefert einen technischen Initiatorwert.
- Der Wert wird ausschliesslich fuer Audit-/Metadaten-/Logging-Zwecke genutzt.
- `IExportService.ExportAllAsync(...)` bleibt vorerst kompatibel und wird mit diesem technischen Initiator aufgerufen.

Falls die Exportlogik fachlich echte Admin-Benutzerrechte erzwingen soll, wird stattdessen eine neue exportfachliche Methode geplant, z. B. `ExportAllForSystemBackupAsync(...)`, die keinen UI-Benutzer simuliert, aber denselben Datenumfang erzeugt. Diese Entscheidung liegt innerhalb der Umsetzung, weil der aktuelle Code `adminUserId` nur als Initiator/Restore-Schutz sichtbar nutzt.

### 3. Vollstaendigkeit des vorhandenen Exports

Vor der Updater-Anbindung wird der Datenumfang von `ExportService.ExportAllAsync(...)` gegen das aktuelle EF-Datenmodell geprueft.

Explizit zu bewerten und gegebenenfalls zu ergaenzen:

- `CalendarEvents`
- `ShoppingListGroups`
- `ShoppingListItems`
- `UserSettings`
- `AppSettings`
- `PluginSettings`
- `PluginSources`
- `PluginSourceReleases`
- `AiRequestLogs`
- `RecipeSideDishes`
- Rezeptfelder wie `Uri` und `Portions`

Akzeptanz fuer diesen Plan:

- Wenn "vollstaendiger Datenexport" als echte technische Vollsicherung verstanden wird, muss `ExportAllAsync(...)` erweitert oder ein separater System-Backup-Export eingefuehrt werden.
- Wenn bewusst nur der vorhandene fachliche Rezept-/Benutzerexport gemeint ist, muss diese Einschraenkung im Code oder in der Dokumentation sichtbar gemacht werden. Fuer die Update-Anforderung wird die technische Vollsicherung bevorzugt, weil sie Datenverlust vor Updates verhindern soll.

### 4. UpdateBackupService

Neuer scoped Service, z. B. `IUpdateBackupService` / `UpdateBackupService` unter `Rezepte.Web/Services`.

Aufgaben:

- Konfiguration validieren.
- Backup-Zielverzeichnis erstellen.
- Zielpfad normalisieren und gegen Pfad-Traversal absichern.
- `IExportService.ExportAllAsync(systemInitiator, includeImages, includePdf, ct)` aufrufen.
- Exportstream zuerst in eine temporaere Datei im Backup-Verzeichnis schreiben.
- Nach erfolgreichem Schreiben atomar in den finalen Backup-Dateinamen verschieben.
- Dateiname eindeutig und sortierbar erzeugen, z. B. `update-backup-20260730-153000Z.zip`.
- Erfolg mit Zielpfad, Dateigroesse und Exportoptionen loggen.
- Fehler mit Exception loggen und weiterwerfen.
- Temp-Dateien best-effort bereinigen.
- Retention nach erfolgreichem finalen Backup anwenden.

Retention:

- Nur Dateien beruecksichtigen, die dem Update-Backup-Namensmuster entsprechen.
- Neueste `RetentionCount` Dateien behalten.
- Aeltere Dateien im konfigurierten Backup-Verzeichnis loeschen.
- Loeschungen loggen.
- Pfadvalidierung vor jeder Loeschung erzwingen.

### 5. Updater-Adapter-Grenze

Neue interne Adapter-Schicht, z. B. unter `Rezepte.Web/Services/Updates`:

- `IApplicationUpdater`
- `MsToolsApplicationUpdater`
- optional `NoopApplicationUpdater` fuer Tests oder solange die externe API nicht lokal verfuegbar ist
- `ApplicationUpdateHostedService` nur falls `msTools.Updater` keinen eigenen Hosted-Service registriert

Die Adapter-Grenze kapselt ausschliesslich die externe Updater-API. Die restliche Anwendung haengt nicht direkt an `msTools.Updater`-Typen.

Vorlaeufige interne Contract-Idee:

```csharp
public interface IApplicationUpdater
{
    Task RegisterPreInstallBackupAsync(Func<CancellationToken, Task> callback, CancellationToken ct);
    Task CheckAndInstallUpdatesAsync(CancellationToken ct);
}
```

Dieser Contract ist nur ein interner Platzhalter. Er wird bei Anbindung der echten `msTools.Updater`-API an deren Semantik angepasst.

### 6. Pre-Install-Anbindung

Der Pre-Install-Hook des Updaters muss so angebunden werden, dass scoped Services korrekt aufgeloest werden:

- `IServiceScopeFactory` verwenden.
- Im Callback einen Scope erzeugen.
- `IUpdateBackupService` aus dem Scope aufloesen.
- Backup awaiten.
- Bei Erfolg Installation fortsetzen lassen.
- Bei Fehler Exception oder Fehlerresultat an `msTools.Updater` zurueckgeben.

Wenn `msTools.Updater` keinen async Hook bietet, darf keine fire-and-forget-Sicherung eingebaut werden. Dann muss der Adapter die Installation blockierend verhindern oder die Integration abbrechen, bis eine belastbare synchrone/awaitbare API verfuegbar ist.

### 7. DI- und Projektintegration

Anpassungen:

- `Rezepte.Web/Rezepte.Web.csproj`: `PackageReference`, `ProjectReference` oder externe Binary-Integration fuer `msTools.Updater`, sobald verfuegbar.
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`: Options, Backup-Service und Updater-Adapter registrieren.
- `Rezepte.Web/appsettings.json`: neue `UpdateBackups`-Section.

Die Einbindung der externen Komponente erfolgt bevorzugt als NuGet-`PackageReference`. Falls kein Paket existiert, wird eine Projekt-/Git-Referenz oder separate Tool-Integration genutzt. Source-Einbindung ist nur letzte Option.

## Testplan

### Unit-Tests fuer UpdateBackupService

Neue Tests unter `Rezepte.Tests/Services`, z. B. `UpdateBackupServiceTests`.

Zu pruefen:

- Erstellt bei gueltiger Konfiguration ein ZIP im konfigurierten Backup-Verzeichnis.
- Nutzt `IExportService.ExportAllAsync(...)` mit dem systemischen Initiator.
- Schreibt zuerst temporaer und veroeffentlicht erst danach den finalen Dateinamen.
- Loescht Temp-Dateien nach Fehlern.
- Loggt Erfolg inklusive Zielpfad.
- Lehnt leeren Backup-Pfad ab.
- Loest relative Pfade gegen `ContentRootPath` auf.
- Verhindert Retention-Loeschungen ausserhalb des Backup-Verzeichnisses.

### Tests fuer Retention

Zu pruefen:

- Bei `RetentionCount = 3` bleiben die drei neuesten Update-Backups erhalten.
- Aeltere Dateien mit passendem Namensmuster werden geloescht.
- Nicht passende Dateien im selben Verzeichnis bleiben erhalten.
- `RetentionCount < 1` fuehrt zu einem Fehler vor Installation.
- Fehler beim Loeschen werden geloggt und als Fehler behandelt, wenn dadurch die Retention nicht verlaesslich angewendet wurde.

### Tests fuer Fehlerverhalten vor Installation

Zu pruefen:

- Wenn `IExportService.ExportAllAsync(...)` fehlschlaegt, wird keine finale Backup-Datei erzeugt.
- Wenn das Schreiben des Backup-ZIPs fehlschlaegt, wird der Fehler an den Pre-Install-Adapter weitergegeben.
- Wenn Retention fehlschlaegt, wird die Installation nicht stillschweigend fortgesetzt.
- Der Updater-Adapter uebersetzt Backup-Fehler in die von `msTools.Updater` erwartete Abbruchsemantik.

### Tests fuer Export-Vollstaendigkeit

Zu pruefen:

- `ExportAllAsync(...)` exportiert alle fuer die Vollsicherung definierten Tabellen/Felder.
- Mindestens die in der Bestandsaufnahme genannten moeglichen Luecken werden durch Tests abgedeckt.
- Falls ein separater System-Backup-Export entsteht, pruefen die Tests dessen Datenumfang statt des bisherigen fachlichen Exports.

### Integration-/Wiring-Tests

Zu pruefen:

- `AddRezepteServices(...)` registriert `UpdateBackupOptions`, `IUpdateBackupService` und den Updater-Adapter.
- Scoped Services werden im Pre-Install-Callback ueber Scope-Factories aufgeloest.
- Eine simulierte Pre-Install-Ausloesung wartet auf das Backup, bevor sie Erfolg meldet.

## Implementierungsreihenfolge

1. Externe `msTools.Updater`-Quelle lokal verfuegbar machen oder API-Dokumentation einsehen.
2. API-Details dokumentieren: Paket/Projekt, Registrierung, Optionen, Pre-Install-Signatur, Fehlersemantik.
3. `UpdateBackupOptions` und `appsettings.json`-Section ergaenzen.
4. Systemischen Export-Initiator festlegen und im Backup-Service nutzen.
5. Export-Vollstaendigkeit pruefen und `ExportAllAsync(...)` oder einen separaten System-Backup-Export erweitern.
6. `IUpdateBackupService` / `UpdateBackupService` implementieren.
7. Unit-Tests fuer Backup-Erstellung, Retention und Fehlerverhalten schreiben.
8. Updater-Adapter-Grenze implementieren.
9. Reale `msTools.Updater`-API hinter dem Adapter anbinden.
10. DI/Wiring in `ServiceCollectionExtensions` abschliessen.
11. Build und Tests ausfuehren: `dotnet build` und `dotnet test`.

## Risiken und Gegenmassnahmen

| Risiko | Gegenmassnahme |
|--------|----------------|
| `msTools.Updater` bietet keinen awaitbaren Pre-Install-Hook | Keine fire-and-forget-Integration; Adapter blockiert die Installation oder Umsetzung stoppt mit klarer Fehlermeldung |
| Backup laeuft im selben Prozess, den der Updater beendet | Hook-Zeitpunkt gegen echte API verifizieren; Backup muss vor Prozessbeendigung abgeschlossen sein |
| Export ist nicht vollstaendig | Datenmodell-Abgleich und Tests fuer alle definierten Exportdaten vor Updater-Aktivierung |
| Technischer Initiator ist fachlich unklar | Expliziten System-Initiator konfigurieren oder eigene System-Backup-Methode einfuehren |
| Grosser Export belastet Speicher | Bestehendes Risiko dokumentieren; falls noetig spaeter Streaming-Export planen |
| Retention loescht falsche Dateien | Namensmuster, Pfadnormalisierung und Verzeichnisvalidierung testen |
| Fehlerhafte Konfiguration erzeugt Scheinsicherheit | Konfigurationsfehler als harter Pre-Install-Fehler behandeln |

## Akzeptanzkriterien fuer die Umsetzung

- `msTools.Updater` ist eingebunden oder hinter einer dokumentierten Adapter-Grenze vorbereitet, falls die reale API lokal noch nicht verfuegbar ist.
- Die spaeter anzubindende externe API ist im Code/Plan klar benannt und lokal gekapselt.
- Vor der Installation wird ein vollstaendiger Systemexport gestartet und erfolgreich abgewartet.
- Der Export nutzt vorhandene Exportlogik oder eine daraus entwickelte System-Backup-Erweiterung ohne Duplizierung.
- Der systemische Export-Initiator ist explizit geloest.
- Die Vollstaendigkeit des Exports gegen das Datenmodell ist geprueft und getestet.
- Das Backup wird in das per `appsettings.json` konfigurierte Zielverzeichnis geschrieben.
- Retention behaelt nur die konfigurierte Anzahl neuester Update-Backups.
- Bei Backup-, Schreib-, Retention- oder Konfigurationsfehlern wird die Installation nicht fortgesetzt.
- Erfolgs- und Fehlerfaelle werden nachvollziehbar geloggt.

## Offene Punkte

Keine offenen Punkte fuer die Planung. Die unbekannte konkrete `msTools.Updater`-API ist als erster Implementierungsschritt und als Adapter-Anforderung eingeplant.
