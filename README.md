# Rezepte

[![Pull Request](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Rezepte/pr.yml?label=Pull%20Request)](https://github.com/martin-stromberg/Rezepte/actions)
[![License](https://img.shields.io/github/license/martin-stromberg/Rezepte)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Rezepte ist eine deutschsprachige Webanwendung zur Verwaltung von Kochbüchern, Rezepten, Bildern und geplanten Mahlzeiten. Das Projekt kombiniert eine Blazor-Server-Oberflaeche mit JSON/API-Endpunkten, SQLite-Persistenz und optionalen KI-gestuetzten Importfunktionen.

## Funktionsumfang

- Benutzerregistrierung, Login und Logout mit Cookie-Authentifizierung für die Weboberflaeche.
- Serverseitige Username-Validierung für Registrierung, Profil und Admin-Benutzerverwaltung.
- JWT-Authentifizierung für API-Aufrufe.
- Erster registrierter Benutzer wird automatisch Administrator.
- Admin-Bereich für Benutzerverwaltung und globale Einstellungen.
- Responsive Navigation mit kompaktem Benutzer- und Einstellungsbereich sowie visuellem Ladebalken bei Navigation.
- Kochbücher mit Sortierung, Detailseiten und Zuordnung mehrerer Rezepte.
- Rezeptverwaltung mit Zutaten, Zubereitungsschritten, Portionsangaben, Bildern und verlinkten Beilagen.
- Einkaufsliste mit Gruppen, abhakbaren Zutaten und Übernahme von Rezeptzutaten inklusive gruppierter Beilagenzutaten.
- Bild-Upload mit Validierung, zugeschnittenen Thumbnails und Grossbildansicht.
- Suche nach Rezepten und Anzeige neuester bzw. zufälliger Rezepte.
- Kalenderansicht für geplante Rezepte mit optionaler Übernahme hinterlegter Beilagen.
- Import von Rezepten aus Backups, Dateien, URLs und unterstuetzten Webseiten.
- Plugin-Framework für Rezeptimporte mit aktivierbarer Reihenfolge in den Admin-Einstellungen.
- Globale GitHub-Pluginquellen in den Admin-Einstellungen mit automatischer Prüfung beim Anwendungsstart.
- Optionale KI-Importe über Gemini für URL-Quellen sowie Google Vision und Gemini für Fotoimporte.
- Export- und Sicherungsfunktionen inklusive Fortschrittsanzeige für Datenexporte sowie validierte Wiederherstellung aus ZIP-Archiven mit Ressourcenlimits.
- Programmupdates über `msTools.Updater` mit Status, Prüfung, Download und Installation in den Admin-Einstellungen sowie Pre-Install-Update-Backups.
- GitHub Actions für Pull-Request-Prüfungen auf `staging`, automatische Promotion- und Sync-PRs sowie automatisierte Release-Artefakte.
- Nutzungs- und KI-Limits über Einstellungen und Protokollierung.
- `security.txt` gemäss RFC 9116 unter `/security.txt` und `/.well-known/security.txt` mit optionalen Zusatzformaten (`/.well-known/security.md`, `/.well-known/security.html`); Konfiguration durch Administratoren im Einstellungsbereich (Canonical wird serverseitig je Ausgabeformat bestimmt); alle Endpunkte öffentlich erreichbar ohne Authentifizierung.

## Tech-Stack

- .NET 10
- ASP.NET Core / Blazor Server mit Interactive Server Components
- Entity Framework Core mit SQLite
- xUnit, FluentAssertions, Moq und EF-Core InMemory für Tests
- Serilog für Console- und Datei-Logging
- QuestPDF für PDF-Erzeugung
- Google Cloud Vision und Gemini für optionale KI-Funktionen

## Projektstruktur

```text
Rezepte.sln
Rezepte.Web/        Webanwendung, API, Services, Datenmodell und Migrationen
Rezepte.Import.Abstractions/
                    Gemeinsame Vertrage und DTOs für Import-Plugins
Rezepte.Import.PluginSdk/
                    Isoliert baubare Parser- und URL-Hilfen für externe Import-Plugins
Rezepte.Import.Plugins.Backup/
                    Backup-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIFoto/
                    KI-Foto-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIUrl/
                    KI-Webseiten-Import-Plugin im Hauptrepository
Rezepte.Tests/      Unit-Tests für zentrale Services
Rezepte.Tests.Browser/
                    Browser-/E2E-Tests mit Playwright
Rezepte.Tests.PluginFixture/
                    Testfixture für Plugin-bezogene Tests
Docs/               Anforderungskatalog und Installationshinweise
```

Wichtige Bereiche in `Rezepte.Web`:

- `Components/Pages`: Blazor-Seiten wie Startseite, Login, Kochbücher, Rezepte, Kalender und Einstellungen.
- `Components/Shared`: wiederverwendbare UI-Komponenten und Dialoge.
- `Controllers`: API-Endpunkte für Auth, Benutzer, Kochbücher, Rezepte, Kalender, Jobs, Einstellungen und Exporte.
- `Services`: Fachlogik und Infrastruktur.
- `Services/Import`: Import-Orchestrierung, PluginManager, hostseitige Persistenz neutraler Importdaten und KI-Hostadapter.
- `Data`, `Entities`, `Migrations`: EF-Core-Datenzugriff und Schemaentwicklung.
- `wwwroot`: statische Assets, CSS, JavaScript, Icons und Manifest.

## Import-Plugins

Rezeptimporte laufen über einen `PluginManager`. Beim Programmstart erkennt die Anwendung Import-Plugin-DLLs im Ausgabeverzeichnis unter `plugins` sowie in direkten Unterordnern von `plugins`. Gefundene Plugins werden in der Datenbank mit Aktivierungsstatus, Reihenfolge und Ladezustand persistiert. Die initiale Reihenfolge berücksichtigt die Standard-Prioritaet der Plugins, sodass KI-Plugins hinter Plugins mit fester Quellenstruktur starten. Administratoren können diese Liste in den Einstellungen unter `Plugins` aktivieren, deaktivieren und sortieren.

Administratoren können unter `Plugins` ausserdem globale GitHub-Pluginquellen verwalten. Beim Hinzufügen werden Repository-URL, Sichtbarkeit, Aktivierung und eine Vertrauensbestätigung erfasst. Aktivierte Quellen werden einmalig beim Anwendungsstart auf das neueste veröffentlichte Release geprüft. Ein geeignetes ZIP-Asset wird serverseitig heruntergeladen, in einem temporaeren Verzeichnis geprüft und bei erfolgreicher Plugin-Erkennung in die Plugin-Unterordner von `plugins` übernommen. GitHub-Rate-Limits werden kontrolliert behandelt. Austausch, Rollback und Reload laufen koordiniert; der bestehende Pluginbestand bleibt bei einem Fehler aktiv und Reloadfehler werden separat historisiert.

Für private GitHub-Repositories kann ein Personal Access Token (PAT) in den Plugin-Einstellungen hinterlegt oder aktualisiert werden. Der PAT wird ausschliesslich über den geschuetzten Secret-Speicher des Backends verwaltet und weder im Frontend angezeigt noch an den Browser übertragen. Änderungen an Quellen oder Tokens werden beim naechsten Anwendungsstart wirksam.

Beim Import werden nur aktivierte Plugins mit Status `Loaded` in gespeicherter Reihenfolge gefragt. Das erste passende Plugin verarbeitet die Datei oder URL; wenn kein Plugin passt, endet der Import mit einer fachlichen Fehlermeldung.

Der aktuelle Stand enthält die gemeinsame Vertragsschicht `Rezepte.Import.Abstractions`, das SDK-Projekt `Rezepte.Import.PluginSdk`, Host-seitige Pluginverwaltung, Admin-UI, Plugin-basierte Importauswahl sowie die Backup-, KI-Foto- und KI-URL-Plugins im Hauptrepository. Alle drei Hauptrepository-Plugins liefern neutrale Import-DTOs an den zentralen Persistenzpfad. Details stehen in `Docs/help/import-plugins.md`.

Das Hauptrepository kann den öffentlichen Import-Plugin-Vertrag mit `scripts/Export-ImportContract.ps1` als separates Contract-ZIP exportieren. Das ZIP enthält Manifest, Dateihashes, ApiCompat-Baseline-DLLs und nur die freigegebenen Contract-Dateien. GitHub-Releases dokumentieren eine konkrete credential-frei abrufbare Contract-ZIP-URL; Actions-Artefakte bleiben CI-Artefakte. Normale Plugin-Builds laden keinen neuen Vertragsstand automatisch herunter; externe Plugin-Repositories importieren einen Exportstand manuell anhand Artefakt-URL und erwartetem ZIP-SHA-256.

Build und Publish der Web-Anwendung bauen die drei Hauptrepository-Plugins mit.

## Voraussetzungen

- .NET SDK 10 oder neuer
- Für den Standardbetrieb keine externe Datenbank; SQLite wird lokal verwendet
- Optional für KI-Funktionen: Gemini API-Key und bei Fotoimporten zusätzlich ein Google-Service-Account für Vision

Die Google-Credentials werden nicht als Datei im Projekt abgelegt und nicht in Build-Ausgaben kopiert. Sie werden zur Laufzeit bevorzugt über die Umgebungsvariablen `GOOGLE_GEMINI_API_KEY` und `GOOGLE_APPLICATION_CREDENTIALS` oder alternativ über die Konfigurationssektion `GoogleCredentials` bereitgestellt. URL-basierte KI-Importe können allein mit Gemini-API-Key arbeiten; Fotoimporte benötigen für Google Vision eine lesbare Service-Account-Datei und zusätzlich Gemini-Authentifizierung. Details zur lokalen Einrichtung stehen in `Docs/development-guide.md`, Details zum Produktionsbetrieb in `Docs/deployment-guide.md`.

## Lokaler Start

```powershell
dotnet restore
dotnet run --project Rezepte.Web
```

Das vorhandene Launch-Profil startet die Anwendung unter:

```text
http://localhost:5220
```

Beim ersten Start wird die SQLite-Datenbank automatisch vorbereitet. Sind EF-Core-Migrationen vorhanden, werden sie angewendet; andernfalls wird die Datenbank erstellt.

## Tests

```powershell
dotnet test
```

`Rezepte.Tests` deckt zentrale Services wie Benutzer, Kochbücher, Rezepte, Einstellungen und KI-Nutzung ab. `Rezepte.Tests.Browser` enthält Browser-/E2E-Tests (u. a. für den security.txt-Flow inklusive Admin-UI und öffentlichen Endpunkten).

Für Browser-Tests muss zusätzlich Playwright-Chromium installiert sein:

```powershell
dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release
pwsh (Get-ChildItem Rezepte.Tests.Browser/bin/Release -Recurse -Filter playwright.ps1 | Select-Object -First 1 -ExpandProperty FullName) install --with-deps chromium
dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release --no-build
```

## Konfiguration

Die wichtigsten Einstellungen liegen in `Rezepte.Web/appsettings.json` und können wie ueblich per User Secrets, Umgebungsvariablen oder Deployment-Konfiguration überschrieben werden.

| Einstellung | Bedeutung |
|-------------|-----------|
| `ConnectionStrings:Default` | SQLite-Connection-String. Fallback: `Data Source=rezepte.db`. |
| `Jwt:Key` | Signaturschlüssel für API-Tokens. Außerhalb der Entwicklungsumgebung zwingend erforderlich (mindestens 32 Zeichen); ohne gültigen Wert startet die Anwendung nicht. In der Entwicklungsumgebung wird ohne Konfiguration ein Zufallsschlüssel pro Prozess erzeugt. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:LifetimeMinutes` | JWT-Basiskonfiguration. Issuer und Audience werden zum Ausstellen und Validieren der Tokens verwendet (Standard: `rezepte` bzw. `rezepte.api`). |
| `Images:MaxSizeBytes` | Maximale Upload-Groesse für Bilder. |
| `Images:AllowedContentTypes` | Erlaubte Bildformate. |
| `AI:Simulate`, `AI:EnableCache`, `AI:CacheDurationHours` | Optionale Einstellungen für KI-Importe und Caching. |
| `GoogleCredentials:ServiceAccountFilePath`, `GoogleCredentials:GeminiApiKey` | Fallback für Google-Credentials, wenn die Umgebungsvariablen `GOOGLE_APPLICATION_CREDENTIALS` bzw. `GOOGLE_GEMINI_API_KEY` nicht gesetzt sind. |
| `PluginUpdates:GitHubApiBaseUrl`, `PluginUpdates:TimeoutSeconds`, `PluginUpdates:UserAgent` | Serverseitige GitHub-Kommunikation für die Startprüfung konfigurierter Pluginquellen. |
| `UpdateBackups:Directory`, `UpdateBackups:RetentionCount` | Zielverzeichnis und Aufbewahrungsanzahl für automatische Backups vor Programmupdates. |
| `UpdateBackups:IncludeImages`, `UpdateBackups:IncludePdf`, `UpdateBackups:SystemInitiatorUserId` | Umfang und technischer Initiator der Update-Backups. |
| `ApplicationUpdates:*` | Steuerung der eingebundenen `msTools.Updater`-Programmupdates, inklusive Quelle, Downloadpfad, automatischem Download und automatischer Installation. |
| `ApplicationUpdates:ServiceName` | Windows: Name des Dienstes, der während der Installation gestoppt und neu gestartet wird. |
| `ApplicationUpdates:ExecutablePath` | Windows: Pfad zur Executable, falls kein Dienst verwendet wird. |
| `ApplicationUpdates:AppPoolName` | Windows (ab `msTools.Updater` `0.7.0-rc.10`): Name des IIS-Application-Pools, der gestoppt und neu gestartet wird. |
| `ApplicationUpdates:SiteName` | Windows (ab `msTools.Updater` `0.7.0-rc.10`): Optionale IIS-Site für das Logging bei `AppPoolName`. |
| `ApplicationUpdates:UpdateUnitName` | Linux: Name der systemd-Unit, über die das Installationsskript ausgeführt wird. |
| `ApplicationUpdates:StopHostAfterScriptStart` | Linux: Muss `true` sein, damit der Hostprozess stoppt und das Skript Dateien ersetzen kann. |
| `LoadingBar:Enabled` | Aktiviert oder deaktiviert den Ladebalken bei Navigation global (Standard: `true`). |
| `LoadingBar:Height` | Hoehe des Ladebalkens als CSS-Laenge, z. B. `"3px"` oder `"0.25rem"` (Standard: `"3px"`). |
| `LoadingBar:AnimationDuration` | Dauer eines vollständigen Sweeps von rechts nach links als CSS-Zeit, z. B. `"2s"` (Standard: `"2s"`). |
| `LoadingBar:HideDelay` | Wartezeit nach Navigationabschluss bis zum Ausblenden des Balkens, z. B. `"300ms"` (Standard: `"300ms"`). |
| `LoadingBar:MaxVisibleDuration` | Sicherheitsgrenze, nach der der Balken auch ohne Abschlusssignal ausgeblendet wird, z. B. `"15s"` (Standard: `"15s"`). |
| `LoadingBar:Colors` | Liste von Hexfarben in der Form `["#RGB", "#RRGGBB", ...]`, aus denen pro Navigationsinteraktion eine zufällige Farbe gewählt wird. |
| `SecurityTxt.Enabled` | Schaltet die security.txt-Auslieferung ein (`true`) oder aus (`false`). Standard: `false`. |
| `SecurityTxt.Contact` | RFC-9116-Direktive `Contact` — URI oder E-Mail, ein Wert pro Zeile. Pflichtfeld bei `Enabled = true`. |
| `SecurityTxt.Expires` | RFC-9116-Direktive `Expires` — Ablaufzeitpunkt als ISO-8601-Datum. Pflichtfeld bei `Enabled = true`; muss in der Zukunft liegen. |
| `SecurityTxt.Encryption` | RFC-9116-Direktive `Encryption` — URL zum öffentlichen Schlüssel. Optional. |
| `SecurityTxt.Acknowledgments` | RFC-9116-Direktive `Acknowledgments` — URL zur Danksagungsseite. Optional. |
| `SecurityTxt.PreferredLanguages` | RFC-9116-Direktive `Preferred-Languages` — kommagetrennte Sprachcodes. Optional. |
| `SecurityTxt.Policy` | RFC-9116-Direktive `Policy` — URL zur Sicherheitsrichtlinie. Optional. |
| `SecurityTxt.Hiring` | RFC-9116-Direktive `Hiring` — URL zu Sicherheitsstellen-Ausschreibungen. Optional. |

Hinweis: `Canonical` ist nicht admin-konfigurierbar. Die Direktive wird vom Server je Ausgabeformat (Plain-Text/Markdown/HTML) automatisch aus Request-Schema, Host, PathBase und Zielpfad erzeugt.

### Ladebalken und visuelles Feedback

Bei der Navigation (Klicks auf Navigationslinks oder Absenden von Formularen) wird ein schmaler, farbiger Ladebalken unterhalb der Navigationsleiste angezeigt. Der Balken erscheint sofort bei Benutzerinteraktionen und bietet Feedback auf langsamen Servern, nutzt eine zufällig gewählte Farbe aus der konfigurierten Farbpalette und animiert sich mit einer linearen Bewegung von rechts nach links (Sweep-Effekt). Er wird ausgeblendet, sobald die Navigation abgeschlossen ist oder ein Sicherheits-Timeout ausläuft.

Das Feature ist standardmaessig aktiviert (`LoadingBar:Enabled: true`), kann aber global deaktiviert werden. Bei aktiviertem `prefers-reduced-motion` (Systemeinstellung) wird die Bewegung durch einen statischen, farbigen Balken ersetzt. Details zu allen `LoadingBar:*`-Parametern stehen in `Docs/help/loading-bar-configuration.md`.

### Programmupdates und Update-Backups

Die Anwendung bindet `msTools.Updater` als externe Update-Komponente ein. Administratoren sehen den Update-Status in den Einstellungen und können dort Prüfung, Download und Installation auslösen. Vor einer Installation erstellt das `BeforeInstall`-Event ein Update-Backup im konfigurierten Zielverzeichnis und wendet die konfigurierte Aufbewahrungsanzahl an. Schlägt das Backup fehl, wird die Installation abgebrochen. Details stehen in `Docs/help/application-updates.md`.

## Daten und Sicherheit

- Passwoerter werden serverseitig gehasht gespeichert.
- Benutzernamen werden zentral serverseitig auf Laenge, erlaubte Zeichen, reservierte Namen sowie IP-/Domain- und offiziell wirkende Muster geprüft.
- Website-Zugriffe verwenden ein HTTP-only Auth-Cookie.
- Login und Registrierung sind pro Client-IP auf 10 Anfragen pro Minute begrenzt (HTTP 429 bei Überschreitung).
- Das Passwort bei der Registrierung muss mindestens 6 Zeichen lang sein.
- Rezeptbilder werden nur an den Eigentümer des Rezepts ausgeliefert und mit `Cache-Control: private` gekennzeichnet.
- Serverseitige Abrufe benutzergelieferter URLs (URL-Import) sind auf öffentliche http(s)-Adressen und die Standardports beschränkt; Loopback-, Link-Local- und private Netzbereiche werden abgelehnt.
- API-Controller sind über JWT abgesichert; Admin-Endpunkte verlangen die Rolle `Admin`.
- Die Registrierung ist nur offen, solange noch kein Benutzer existiert.
- Rezept-, Kochbuch-, Kalender- und Einstellungsdaten sind benutzerbezogen modelliert.
- Session-basierte Importabläufe sind an den initiierenden authentifizierten Benutzer gebunden; fremde oder ungültige Session-IDs legen keine Sessiondetails offen.
- PATs für private GitHub-Pluginquellen verbleiben im geschuetzten Secret-Speicher des Backends und werden nicht an das Frontend ausgegeben oder protokolliert.
- Die Pfade `/security.txt`, `/.well-known/security.txt`, `/.well-known/security.md` und `/.well-known/security.html` sind explizit von der Authentifizierungspflicht ausgenommen und öffentlich erreichbar; bei deaktivierter Funktion (`SecurityTxt.Enabled = false`) antworten alle vier Endpunkte mit HTTP 404.

## Deployment

`Docs/install.md` ist die verbindliche Schritt-für-Schritt-Anleitung für Publish, Runtime-Prüfung und systemd-Betrieb auf Linux, zum Beispiel aus `/var/www/rezepte`.

GitHub Actions prüfen Pull Requests gegen `main` automatisch mit Restore, Build, Tests, Contract-Export, optionalem ApiCompat-Vergleich gegen die neueste passende gespeicherte Contract-Baseline und Format-Check. Nach einem gemergten Pull Request baut der Release-Workflow die Anwendung sowie beide Testprojekte, bereitet Playwright für die Browser-Tests vor, fuehrt die Release-Tests projektweise aus und erstellt anschliessend ein `release.zip` sowie ein separates Import-Contract-ZIP als Actions-Artefakte. Bei SemVer-relevanten Conventional Commits wird zusätzlich ein GitHub Release erstellt. Details stehen in `Docs/help/github-actions.md`.

Typischer framework-abhängiger Publish-Befehl:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

Bei framework-abhängigem Publish müssen auf dem Server passende .NET-10-Shared-Frameworks für `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App` installiert sein. Wenn die Server-Runtime nicht verlaesslich bereitsteht, sollte stattdessen die in `Docs/install.md` dokumentierte self-contained Alternative verwendet werden.

In Produktion sollten mindestens diese Punkte gesetzt bzw. geprüft werden:

- eigener `Jwt:Key` mit mindestens 32 Zeichen (z. B. per Umgebungsvariable `Jwt__Key`)
- passende .NET-10-Shared-Frameworks oder self-contained Publish
- persistenter Speicherort für SQLite-Datenbank und Logs
- HTTPS/TLS vor der Anwendung
- `GOOGLE_GEMINI_API_KEY` für Gemini-basierte KI-Importe
- `GOOGLE_APPLICATION_CREDENTIALS` mit lesbarer Service-Account-Datei für Google-Vision-/Fotoimporte
- Dateirechte für den systemd-Benutzer

## Changelog und Releases

Änderungen sind in `changes.log` dokumentiert. Beim Merge zu `main` werden automatisch GitHub Releases erstellt (mit SemVer-Versionierung bei relevanten Conventional Commits). Details zum Release-Prozess stehen in `Docs/help/github-actions.md`.

## Weiterfuehrende Dokumentation

- `Docs/Anforderungskatalog.md`: fachlicher Status und geplante Erweiterungen.
- `Docs/dependencies.md`: Abhängigkeits- und Sicherheitsstrategie, inklusive dokumentierter Behandlung verbleibender NuGet-Sicherheitswarnungen.
- `Docs/help/navigation.md`: Bedienhinweise zur Navigation, Einrichtung und zum Benutzermenü.
- `Docs/help/user-accounts.md`: Bedienhinweise zu Registrierung, Profil und Admin-Benutzerverwaltung.
- `Docs/help/exports.md`: Bedienhinweise zu Datenexporten, Sicherungen und Fortschrittsanzeige.
- `Docs/help/application-updates.md`: Hinweise zu vorbereiteten Programmupdates, Update-Backups, Retention und Aktivierungsvoraussetzungen.
- `Docs/help/import-plugins.md`: Bedienhinweise und aktueller Umsetzungsstand des Import-Pluginsystems.
- `Docs/help/github-actions.md`: Hinweise zu Pull-Request-Prüfungen, Release-Artefakten und SemVer-Versionierung.
- `Docs/help/side-dishes.md`: Bedienhinweise zu Beilagen in Rezepten, Kalender und Einkaufsliste.
- `Docs/help/recipe-search.md`: Bedienhinweise zur Rezeptsuche, Trefferlogik und Kochbuchfilterung.
- `Docs/help/shopping-list.md`: Bedienhinweise zur Einkaufsliste und Rezeptübernahme.
- `Docs/help/security-txt/index.md`: Konfiguration und Betrieb der security.txt-Funktion gemäss RFC 9116.
- `Docs/install.md`: manuelle Installationsnotizen für Linux/systemd.
- `Docs/development-guide.md`: lokale Einrichtung von Google-Credentials über Umgebungsvariablen/User Secrets.
- `Docs/deployment-guide.md`: Secret-Store- und Umgebungsvariablen-Setup für Google-Credentials in Production.
