# Rezepte

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
- Optionale KI-Importe ueber Google Vision und Gemini.
- Exportfunktionen und Hintergrundjobs fuer laenger laufende Aufgaben, inklusive Fortschrittsanzeige fuer Datenexporte und Sicherungen.
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
Rezepte.Import.Plugins.*/
                    Import-Pluginprojekte fuer Backup- und Webseitenquellen
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

Beim Import werden nur aktivierte Plugins mit Status `Loaded` in gespeicherter Reihenfolge gefragt. Das erste passende Plugin verarbeitet die Datei oder URL; wenn kein Plugin passt, endet der Import mit einer fachlichen Fehlermeldung.

Das Chefkoch-Plugin unterstuetzt neben einzelnen Rezeptseiten auch Rezeptsammlungen. Bei einer Sammlungs-URL zeigt der Importdialog zuerst die gefundenen Rezepte an; ausgewaehlte Rezepte erhalten jeweils ein Zielkochbuch und werden erst nach dem Absenden abgerufen. Fuer Massenimporte lassen sich alle gefundenen Rezepte gesammelt auswaehlen oder abwaehlen und ein Zielkochbuch fuer alle ausgewaehlten Rezepte uebernehmen. Der Fortschritt wird pro Rezept angezeigt, Teilfehler stoppen die uebrigen ausgewaehlten Importe nicht.

Der aktuelle Stand enthaelt die gemeinsame Vertragsschicht `Rezepte.Import.Abstractions`, Host-seitige Pluginverwaltung, Admin-UI, Plugin-basierte Importauswahl und produktive Pluginprojekte fuer Backup sowie die klassischen Webseitenquellen. KI-Foto und KI-URL laufen bewusst als Hostadapter, liefern ihre Ergebnisse aber ebenfalls ueber neutrale Import-DTOs an den zentralen Persistenzpfad. Details stehen in `Docs/help/import-plugins.md`.

Build und Publish der Web-Anwendung bauen die produktiven externen Pluginprojekte mit und kopieren sie in das jeweilige `plugins`-Verzeichnis.

## Voraussetzungen

- .NET SDK 10 oder neuer
- Fuer den Standardbetrieb keine externe Datenbank; SQLite wird lokal verwendet
- Optional fuer KI-Funktionen:
  - `google.application-credentials.json`
  - `google.gemini.api-key.json`

Die Google-Dateien werden vom Webprojekt in das Build-Ausgabeverzeichnis kopiert, sollten aber als Secrets behandelt und nicht in das Repository eingecheckt werden.

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

## Daten und Sicherheit

- Passwoerter werden serverseitig gehasht gespeichert.
- Benutzernamen werden zentral serverseitig auf Laenge, erlaubte Zeichen, reservierte Namen sowie IP-/Domain- und offiziell wirkende Muster geprueft.
- Website-Zugriffe verwenden ein HTTP-only Auth-Cookie.
- API-Controller sind ueber JWT abgesichert; Admin-Endpunkte verlangen die Rolle `Admin`.
- Die Registrierung ist nur offen, solange noch kein Benutzer existiert.
- Rezept-, Kochbuch-, Kalender- und Einstellungsdaten sind benutzerbezogen modelliert.

## Deployment

`Docs/install.md` ist die verbindliche Schritt-fuer-Schritt-Anleitung fuer Publish, Runtime-Pruefung und systemd-Betrieb auf Linux, zum Beispiel aus `/var/www/rezepte`.

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
- Google-Credentials nur bei aktivierten KI-Funktionen
- Dateirechte fuer den systemd-Benutzer

## Weiterfuehrende Dokumentation

- `Docs/Anforderungskatalog.md`: fachlicher Status und geplante Erweiterungen.
- `Docs/dependencies.md`: Abhaengigkeits- und Sicherheitsstrategie, inklusive dokumentierter Behandlung verbleibender NuGet-Sicherheitswarnungen.
- `Docs/help/navigation.md`: Bedienhinweise zur Navigation, Einrichtung und zum Benutzermenue.
- `Docs/help/user-accounts.md`: Bedienhinweise zu Registrierung, Profil und Admin-Benutzerverwaltung.
- `Docs/help/exports.md`: Bedienhinweise zu Datenexporten, Sicherungen und Fortschrittsanzeige.
- `Docs/help/import-plugins.md`: Bedienhinweise und aktueller Umsetzungsstand des Import-Pluginsystems.
- `Docs/help/side-dishes.md`: Bedienhinweise zu Beilagen in Rezepten, Kalender und Einkaufsliste.
- `Docs/help/recipe-search.md`: Bedienhinweise zur Rezeptsuche, Trefferlogik und Kochbuchfilterung.
- `Docs/help/shopping-list.md`: Bedienhinweise zur Einkaufsliste und Rezeptuebernahme.
- `Docs/install.md`: manuelle Installationsnotizen fuer Linux/systemd.
