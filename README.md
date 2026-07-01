# Rezepte

Rezepte ist eine deutschsprachige Webanwendung zur Verwaltung von Kochbuechern, Rezepten, Bildern und geplanten Mahlzeiten. Das Projekt kombiniert eine Blazor-Server-Oberflaeche mit JSON/API-Endpunkten, SQLite-Persistenz und optionalen KI-gestuetzten Importfunktionen.

## Funktionsumfang

- Benutzerregistrierung, Login und Logout mit Cookie-Authentifizierung fuer die Weboberflaeche.
- JWT-Authentifizierung fuer API-Aufrufe.
- Erster registrierter Benutzer wird automatisch Administrator.
- Admin-Bereich fuer Benutzerverwaltung und globale Einstellungen.
- Responsive Navigation mit kompaktem Benutzer- und Einstellungsbereich.
- Kochbuecher mit Sortierung, Detailseiten und Zuordnung mehrerer Rezepte.
- Rezeptverwaltung mit Zutaten, Zubereitungsschritten, Portionsangaben und Bildern.
- Bild-Upload mit Validierung, zugeschnittenen Thumbnails und Grossbildansicht.
- Suche nach Rezepten und Anzeige neuester bzw. zufaelliger Rezepte.
- Kalenderansicht fuer geplante Rezepte.
- Import von Rezepten aus Backups, Dateien, URLs und unterstuetzten Webseiten.
- Optionale KI-Importe ueber Google Vision und Gemini.
- Exportfunktionen und Hintergrundjobs fuer laenger laufende Aufgaben.
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
Rezepte.Tests/      Unit-Tests fuer zentrale Services
Docs/               Anforderungskatalog und Installationshinweise
```

Wichtige Bereiche in `Rezepte.Web`:

- `Components/Pages`: Blazor-Seiten wie Startseite, Login, Kochbuecher, Rezepte, Kalender und Einstellungen.
- `Components/Shared`: wiederverwendbare UI-Komponenten und Dialoge.
- `Controllers`: API-Endpunkte fuer Auth, Benutzer, Kochbuecher, Rezepte, Kalender, Jobs, Einstellungen und Exporte.
- `Services`: Fachlogik und Infrastruktur.
- `Services/Import`: Import-Orchestrierung und Handler fuer Backup-, URL-, Webseiten- und KI-Importe.
- `Data`, `Entities`, `Migrations`: EF-Core-Datenzugriff und Schemaentwicklung.
- `wwwroot`: statische Assets, CSS, JavaScript, Icons und Manifest.

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
- Website-Zugriffe verwenden ein HTTP-only Auth-Cookie.
- API-Controller sind ueber JWT abgesichert; Admin-Endpunkte verlangen die Rolle `Admin`.
- Die Registrierung ist nur offen, solange noch kein Benutzer existiert.
- Rezept-, Kochbuch-, Kalender- und Einstellungsdaten sind benutzerbezogen modelliert.

## Deployment

`Docs/install.md` beschreibt ein framework-abhaengiges Publish fuer `linux-x64` und den Betrieb als systemd-Service, zum Beispiel aus `/var/www/rezepte`.

Typischer Publish-Befehl:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

In Produktion sollten mindestens diese Punkte gesetzt bzw. geprueft werden:

- eigener `Jwt:Key`
- persistenter Speicherort fuer SQLite-Datenbank und Logs
- HTTPS/TLS vor der Anwendung
- Google-Credentials nur bei aktivierten KI-Funktionen
- Dateirechte fuer den systemd-Benutzer

## Weiterfuehrende Dokumentation

- `Docs/Anforderungskatalog.md`: fachlicher Status und geplante Erweiterungen.
- `Docs/dependencies.md`: Abhaengigkeits- und Sicherheitsstrategie, inklusive dokumentierter Behandlung verbleibender NuGet-Sicherheitswarnungen.
- `Docs/help/navigation.md`: Bedienhinweise zur Navigation, Einrichtung und zum Benutzermenue.
- `Docs/install.md`: manuelle Installationsnotizen fuer Linux/systemd.
