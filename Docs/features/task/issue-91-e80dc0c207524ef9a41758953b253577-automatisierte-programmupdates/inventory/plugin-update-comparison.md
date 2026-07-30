# Plugin-Update-Services als Vergleich

## Abgrenzung

Die vorhandenen Plugin-Update-Services aktualisieren Import-Plugins, nicht die Webanwendung. Sie duerfen nur als Architekturvergleich dienen. Fachlich sollten Web-App-Updates nicht mit Plugin-Quellen, Plugin-Releases, Plugin-Settings oder Plugin-Verzeichnissen vermischt werden.

## Relevante Klassen

- `PluginUpdateOptions`
- `PluginUpdateHostedService`
- `IPluginUpdateService`
- `PluginUpdateService`
- `GitHubReleaseClient`
- `PluginPackageValidator`
- `PluginPackageInstaller`
- `PluginManager`
- `PluginStartupService`

## Ablauf des Plugin-Updaters

`PluginUpdateHostedService` laeuft beim Start, erstellt einen Scope, loest `IPluginUpdateService` auf und ruft `CheckForUpdatesAsync` auf. Fehler werden geloggt, blockieren aber den App-Start nicht.

`PluginUpdateService`:

- liest aktivierte und bestaetigte Plugin-Quellen aus der Datenbank
- fragt GitHub-Releases ab
- findet ZIP-Assets
- legt/aktualisiert Release-Statusdatensaetze
- laedt ZIP-Dateien in ein temp-Verzeichnis
- validiert Pakete
- installiert ueber `PluginPackageInstaller`
- persistiert Status, Fehler, Timestamps und Reload-Status
- loescht temporaere Verzeichnisse best-effort

`PluginPackageInstaller`:

- nutzt einen statischen `SemaphoreSlim` als Installationslock
- schreibt nach `<ContentRoot>/plugins`
- erstellt temporaere Backups vorhandener Plugin-Verzeichnisse
- ersetzt Plugin-Verzeichnisse unter `PluginManager.CoordinateReloadAsync`
- rollt bei Fehlern zurueck

## Wiederverwendbare Muster

- Hosted-Service-Registrierung in `AddRezepteServices`
- scoped Service-Aufloesung aus Hosted-Service/Callback
- Status- und Fehlerlogging pro Phase
- temp-Verzeichnis fuer Downloads/Arbeitsdaten
- expliziter Installationslock gegen parallele Installationen
- Rollback/Backup vor ersetzenden Dateioperationen
- Best-effort Cleanup in `finally`
- klare Trennung von Download, Validierung, Installation und Reload

## Nicht direkt uebertragbar

- PluginSource/PluginSourceRelease-Entities gehoeren zum Import-Plugin-System.
- GitHubReleaseClient ist auf GitHub-Releases fuer Plugin-ZIPs zugeschnitten.
- PluginPackageValidator validiert Import-Plugin-Strukturen, nicht Web-App-Releases.
- PluginPackageInstaller ersetzt Plugin-Verzeichnisse, nicht Web-App-Binaries.
- PluginManager-Lifecycle-Lock schuetzt Import-Handler, nicht die laufende Web-App.

## Konsequenz fuer Web-App-Updates

Falls `msTools.Updater` eigene Download-/Installationsmechanismen bereitstellt, sollte die Anwendung diese nicht durch den Plugin-Updater nachbauen. Die Anwendung sollte nur:

- die externe Komponente registrieren/konfigurieren
- das Pre-Install-Event anbinden
- im Event ein Backup erzeugen
- Fehler so signalisieren, dass die Installation abgebrochen wird

Alles, was Release-Ermittlung und Web-App-Binary-Austausch betrifft, sollte aus `msTools.Updater` kommen.
