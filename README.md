# Rezepte

[![Pull Request](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Rezepte/pr.yml?label=Pull%20Request)](https://github.com/martin-stromberg/Rezepte/actions)
[![License](https://img.shields.io/github/license/martin-stromberg/Rezepte)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Rezepte ist eine deutschsprachige Webanwendung zur Verwaltung von Kochbuechern, Rezepten, Bildern und geplanten Mahlzeiten. Das Projekt kombiniert eine Blazor-Server-Oberflaeche mit JSON/API-Endpunkten, SQLite-Persistenz und optionalen KI-gestuetzten Importfunktionen.

## Funktionsumfang

- Benutzerregistrierung, Login und Logout mit Cookie-Authentifizierung fuer die Weboberflaeche.
- Serverseitige Username-Validierung fuer Registrierung, Profil und Admin-Benutzerverwaltung.
- JWT-Authentifizierung fuer API-Aufrufe.
- Erster registrierter Benutzer wird automatisch Administrator.
- Admin-Bereich fuer Benutzerverwaltung und globale Einstellungen.
- Responsive Navigation mit kompaktem Benutzer- und Einstellungsbereich.
- Kochbuecher mit Sortierung, Detailseiten und Zuordnung mehrerer Rezepte.
- Rezeptverwaltung mit Zutaten, Zubereitungsschritten, Portionsangaben, Bildern und verlinkten Beilagen.
- Einkaufsliste mit Gruppen, abhakbaren Zutaten und Uebernahme von Rezeptzutaten inklusive gruppierter Beilagenzutaten.
- Bild-Upload mit Validierung, zugeschnittenen Thumbnails und Grossbildansicht.
- Suche nach Rezepten und Anzeige neuester bzw. zufaelliger Rezepte.
- Kalenderansicht fuer geplante Rezepte mit optionaler Uebernahme hinterlegter Beilagen.
- Import von Rezepten aus Backups, Dateien, URLs und unterstuetzten Webseiten, inklusive Chefkoch-Rezeptsammlungen mit Zwischenauswahl.
- Plugin-Framework fuer Rezeptimporte mit aktivierbarer Reihenfolge in den Admin-Einstellungen.
- Globale GitHub-Pluginquellen in den Admin-Einstellungen mit automatischer Pruefung beim Anwendungsstart.
- Optionale KI-Importe ueber Gemini fuer URL-Quellen sowie Google Vision und Gemini fuer Fotoimporte.
- Exportfunktionen und Hintergrundjobs fuer laenger laufende Aufgaben, inklusive Fortschrittsanzeige fuer Datenexporte und Sicherungen.
- GitHub Actions fuer Pull-Request-Pruefungen und automatisierte Release-Artefakte.
- Nutzungs- und KI-Limits ueber Einstellungen und Protokollierung.

## Tech-Stack

- .NET 10
- ASP.NET Core / Blazor Server mit Interactive Server Components
- Entity Framework Core mit SQLite
- xUnit, FluentAssertions, Moq und EF-Core InMemory fuer Tests
- Serilog fuer Console- und Datei-Logging
- QuestPDF fuer PDF-Erzeugung
- Google Cloud Vision und Gemini fuer optionale KI-Funktionen

## Projektstruktur

```text
Rezepte.sln
Rezepte.Web/        Webanwendung, API, Services, Datenmodell und Migrationen
Rezepte.Import.Abstractions/
                    Gemeinsame Vertrage und DTOs fuer Import-Plugins
Rezepte.Import.PluginSdk/
                    Isoliert baubare Parser- und URL-Hilfen fuer externe Import-Plugins
Rezepte.Import.Plugins.Backup/
                    Backup-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIFoto/
                    KI-Foto-Import-Plugin im Hauptrepository
Rezepte.Import.Plugins.AIUrl/
                    KI-Webseiten-Import-Plugin im Hauptrepository
external/rezepte-import-plugins-private/
                    Lokale Struktur des privaten Repositories fuer Webseitenquellen
Rezepte.Tests/      Unit-Tests fuer zentrale Services
Docs/               Anforderungskatalog und Installationshinweise
```

Wichtige Bereiche in `Rezepte.Web`:

- `Components/Pages`: Blazor-Seiten wie Startseite, Login, Kochbuecher, Rezepte, Kalender und Einstellungen.
- `Components/Shared`: wiederverwendbare UI-Komponenten und Dialoge.
- `Controllers`: API-Endpunkte fuer Auth, Benutzer, Kochbuecher, Rezepte, Kalender, Jobs, Einstellungen und Exporte.
- `Services`: Fachlogik und Infrastruktur.
- `Services/Import`: Import-Orchestrierung, PluginManager, hostseitige Persistenz neutraler Importdaten und KI-Hostadapter.
- `Data`, `Entities`, `Migrations`: EF-Core-Datenzugriff und Schemaentwicklung.
- `wwwroot`: statische Assets, CSS, JavaScript, Icons und Manifest.

## Import-Plugins

Rezeptimporte laufen ueber einen `PluginManager`. Beim Programmstart erkennt die Anwendung Import-Plugin-DLLs im Ausgabeverzeichnis unter `plugins` sowie in direkten Unterordnern von `plugins`. Gefundene Plugins werden in der Datenbank mit Aktivierungsstatus, Reihenfolge und Ladezustand persistiert. Die initiale Reihenfolge beruecksichtigt die Standard-Prioritaet der Plugins, sodass KI-Plugins hinter Plugins mit fester Quellenstruktur starten. Administratoren koennen diese Liste in den Einstellungen unter `Plugins` aktivieren, deaktivieren und sortieren.

Administratoren koennen unter `Plugins` ausserdem globale GitHub-Pluginquellen verwalten. Beim Hinzufuegen werden Repository-URL, Sichtbarkeit, Aktivierung und eine Vertrauensbestaetigung erfasst. Aktivierte Quellen werden einmalig beim Anwendungsstart auf das neueste veroeffentlichte Release geprueft. Ein geeignetes ZIP-Asset wird serverseitig heruntergeladen, in einem temporaeren Verzeichnis geprueft und bei erfolgreicher Plugin-Erkennung in die Plugin-Unterordner von `plugins` uebernommen. GitHub-Rate-Limits werden kontrolliert behandelt. Austausch, Rollback und Reload laufen koordiniert; der bestehende Pluginbestand bleibt bei einem Fehler aktiv und Reloadfehler werden separat historisiert.

Fuer private GitHub-Repositories kann ein Personal Access Token (PAT) in den Plugin-Einstellungen hinterlegt oder aktualisiert werden. Der PAT wird ausschliesslich ueber den geschuetzten Secret-Speicher des Backends verwaltet und weder im Frontend angezeigt noch an den Browser uebertragen. Aenderungen an Quellen oder Tokens werden beim naechsten Anwendungsstart wirksam.

Beim Import werden nur aktivierte Plugins mit Status `Loaded` in gespeicherter Reihenfolge gefragt. Das erste passende Plugin verarbeitet die Datei oder URL; wenn kein Plugin passt, endet der Import mit einer fachlichen Fehlermeldung.

Das Chefkoch-Plugin unterstuetzt neben einzelnen Rezeptseiten auch Rezeptsammlungen. Bei einer Sammlungs-URL zeigt der Importdialog zuerst die gefundenen Rezepte an; ausgewaehlte Rezepte erhalten jeweils ein Zielkochbuch und werden erst nach dem Absenden abgerufen. Fuer Massenimporte lassen sich alle gefundenen Rezepte gesammelt auswaehlen oder abwaehlen und ein Zielkochbuch fuer alle ausgewaehlten Rezepte uebernehmen. Der Fortschritt wird pro Rezept angezeigt, Teilfehler stoppen die uebrigen ausgewaehlten Importe nicht.

Der aktuelle Stand enthaelt die gemeinsame Vertragsschicht `Rezepte.Import.Abstractions`, das SDK-Projekt `Rezepte.Import.PluginSdk`, Host-seitige Pluginverwaltung, Admin-UI, Plugin-basierte Importauswahl sowie die Backup-, KI-Foto- und KI-URL-Plugins im Hauptrepository. Die klassischen Webseitenquellen liegen in der privaten Plugin-Repository-Struktur `external/rezepte-import-plugins-private` und werden als externe Artefakte an den Host uebergeben. Alle drei Hauptrepository-Plugins liefern neutrale Import-DTOs an den zentralen Persistenzpfad. Details stehen in `Docs/help/import-plugins.md`.

Das Hauptrepository kann den oeffentlichen Import-Plugin-Vertrag mit `scripts/Export-ImportContract.ps1` als separates Contract-ZIP exportieren. Das ZIP enthaelt Manifest, Dateihashes, ApiCompat-Baseline-DLLs und nur die freigegebenen Contract-Dateien. GitHub-Releases dokumentieren eine konkrete credential-frei abrufbare Contract-ZIP-URL; Actions-Artefakte bleiben CI-Artefakte. Normale Plugin-Builds laden keinen neuen Vertragsstand automatisch herunter; externe Plugin-Repositories importieren einen Exportstand manuell anhand Artefakt-URL und erwartetem ZIP-SHA-256.

Build und Publish der Web-Anwendung bauen die drei Hauptrepository-Plugins mit. Vorhandene Artefakte aus `external/rezepte-import-plugins-private/artifacts/plugins` werden zusaetzlich in das jeweilige `plugins`-Verzeichnis uebernommen. Das private Plugin-Repository stellt dafuer `publish-plugins.ps1` bereit.

## Voraussetzungen

- .NET SDK 10 oder neuer
- Fuer den Standardbetrieb keine externe Datenbank; SQLite wird lokal verwendet
- Optional fuer KI-Funktionen: Gemini API-Key und bei Fotoimporten zusaetzlich ein Google-Service-Account fuer Vision

Die Google-Credentials werden nicht als Datei im Projekt abgelegt und nicht in Build-Ausgaben kopiert. Sie werden zur Laufzeit bevorzugt ueber die Umgebungsvariablen `GOOGLE_GEMINI_API_KEY` und `GOOGLE_APPLICATION_CREDENTIALS` oder alternativ ueber die Konfigurationssektion `GoogleCredentials` bereitgestellt. URL-basierte KI-Importe koennen allein mit Gemini-API-Key arbeiten; Fotoimporte benoetigen fuer Google Vision eine lesbare Service-Account-Datei und zusaetzlich Gemini-Authentifizierung. Details zur lokalen Einrichtung stehen in `Docs/development-guide.md`, Details zum Produktionsbetrieb in `Docs/deployment-guide.md`.

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

Das Testprojekt `Rezepte.Tests` deckt zentrale Services wie Benutzer, Kochbuecher, Rezepte, Einstellungen und KI-Nutzung ab.

## Konfiguration

Die wichtigsten Einstellungen liegen in `Rezepte.Web/appsettings.json` und koennen wie ueblich per User Secrets, Umgebungsvariablen oder Deployment-Konfiguration ueberschrieben werden.

| Einstellung | Bedeutung |
|-------------|-----------|
| `ConnectionStrings:Default` | SQLite-Connection-String. Fallback: `Data Source=rezepte.db`. |
| `Jwt:Key` | Signaturschluessel fuer API-Tokens. In Produktion ersetzen. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:LifetimeMinutes` | JWT-Basiskonfiguration. Hinweis: Die Validierung verwendet im aktuellen Code feste Issuer-/Audience-Werte. |
| `Images:MaxSizeBytes` | Maximale Upload-Groesse fuer Bilder. |
| `Images:AllowedContentTypes` | Erlaubte Bildformate. |
| `AI:Simulate`, `AI:EnableCache`, `AI:CacheDurationHours` | Optionale Einstellungen fuer KI-Importe und Caching. |
| `GoogleCredentials:ServiceAccountFilePath`, `GoogleCredentials:GeminiApiKey` | Fallback fuer Google-Credentials, wenn die Umgebungsvariablen `GOOGLE_APPLICATION_CREDENTIALS` bzw. `GOOGLE_GEMINI_API_KEY` nicht gesetzt sind. |
| `PluginUpdates:GitHubApiBaseUrl`, `PluginUpdates:TimeoutSeconds`, `PluginUpdates:UserAgent` | Serverseitige GitHub-Kommunikation fuer die Startpruefung konfigurierter Pluginquellen. |

## Daten und Sicherheit

- Passwoerter werden serverseitig gehasht gespeichert.
- Benutzernamen werden zentral serverseitig auf Laenge, erlaubte Zeichen, reservierte Namen sowie IP-/Domain- und offiziell wirkende Muster geprueft.
- Website-Zugriffe verwenden ein HTTP-only Auth-Cookie.
- API-Controller sind ueber JWT abgesichert; Admin-Endpunkte verlangen die Rolle `Admin`.
- Die Registrierung ist nur offen, solange noch kein Benutzer existiert.
- Rezept-, Kochbuch-, Kalender- und Einstellungsdaten sind benutzerbezogen modelliert.
- Session-basierte Importablaeufe sind an den initiierenden authentifizierten Benutzer gebunden; fremde oder ungueltige Session-IDs legen keine Sessiondetails offen.
- PATs fuer private GitHub-Pluginquellen verbleiben im geschuetzten Secret-Speicher des Backends und werden nicht an das Frontend ausgegeben oder protokolliert.

## Deployment

`Docs/install.md` ist die verbindliche Schritt-fuer-Schritt-Anleitung fuer Publish, Runtime-Pruefung und systemd-Betrieb auf Linux, zum Beispiel aus `/var/www/rezepte`.

GitHub Actions pruefen Pull Requests gegen `main` automatisch mit Restore, Build, Tests, Contract-Export, optionalem ApiCompat-Vergleich gegen die neueste passende gespeicherte Contract-Baseline und Format-Check. Nach einem gemergten Pull Request erstellt der Release-Workflow ein `release.zip` sowie ein separates Import-Contract-ZIP als Actions-Artefakte und bei SemVer-relevanten Conventional Commits zusaetzlich einen GitHub Release. Details stehen in `Docs/help/github-actions.md`.

Typischer framework-abhaengiger Publish-Befehl:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

Bei framework-abhaengigem Publish muessen auf dem Server passende .NET-10-Shared-Frameworks fuer `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App` installiert sein. Wenn die Server-Runtime nicht verlaesslich bereitsteht, sollte stattdessen die in `Docs/install.md` dokumentierte self-contained Alternative verwendet werden.

In Produktion sollten mindestens diese Punkte gesetzt bzw. geprueft werden:

- eigener `Jwt:Key`
- passende .NET-10-Shared-Frameworks oder self-contained Publish
- persistenter Speicherort fuer SQLite-Datenbank und Logs
- HTTPS/TLS vor der Anwendung
- `GOOGLE_GEMINI_API_KEY` fuer Gemini-basierte KI-Importe
- `GOOGLE_APPLICATION_CREDENTIALS` mit lesbarer Service-Account-Datei fuer Google-Vision-/Fotoimporte
- Dateirechte fuer den systemd-Benutzer

## Changelog und Releases

Aenderungen sind in `changes.log` dokumentiert. Beim Merge zu `main` werden automatisch GitHub Releases erstellt (mit SemVer-Versionierung bei relevanten Conventional Commits). Details zum Release-Prozess stehen in `Docs/help/github-actions.md`.

## Weiterfuehrende Dokumentation

- `Docs/Anforderungskatalog.md`: fachlicher Status und geplante Erweiterungen.
- `Docs/dependencies.md`: Abhaengigkeits- und Sicherheitsstrategie, inklusive dokumentierter Behandlung verbleibender NuGet-Sicherheitswarnungen.
- `Docs/help/navigation.md`: Bedienhinweise zur Navigation, Einrichtung und zum Benutzermenue.
- `Docs/help/user-accounts.md`: Bedienhinweise zu Registrierung, Profil und Admin-Benutzerverwaltung.
- `Docs/help/exports.md`: Bedienhinweise zu Datenexporten, Sicherungen und Fortschrittsanzeige.
- `Docs/help/import-plugins.md`: Bedienhinweise und aktueller Umsetzungsstand des Import-Pluginsystems.
- `Docs/help/github-actions.md`: Hinweise zu Pull-Request-Pruefungen, Release-Artefakten und SemVer-Versionierung.
- `Docs/help/side-dishes.md`: Bedienhinweise zu Beilagen in Rezepten, Kalender und Einkaufsliste.
- `Docs/help/recipe-search.md`: Bedienhinweise zur Rezeptsuche, Trefferlogik und Kochbuchfilterung.
- `Docs/help/shopping-list.md`: Bedienhinweise zur Einkaufsliste und Rezeptuebernahme.
- `Docs/install.md`: manuelle Installationsnotizen fuer Linux/systemd.
- `Docs/development-guide.md`: lokale Einrichtung von Google-Credentials ueber Umgebungsvariablen/User Secrets.
- `Docs/deployment-guide.md`: Secret-Store- und Umgebungsvariablen-Setup fuer Google-Credentials in Production.
