# Projektstruktur

## Root

- `Rezepte.sln`: Solution mit Web- und Testprojekt.
- `README.md`: Projektuebersicht, wird in diesem Lauf nachgezogen.
- `LICENSE`: Lizenzdatei.
- `Docs/`: bestehende Installationshinweise und Anforderungskatalog.

## `Rezepte.Web`

- `Program.cs`: Anwendungseinstieg, Middleware, Komponenten- und Controller-Mapping.
- `Components/Pages`: Blazor-Seiten fuer Home, Login, Registrierung, Kochbuecher, Rezepte, Kalender und Einstellungen.
- `Components/Shared`: wiederverwendbare UI-Komponenten wie Dialoge, Bild-Overlay, Zufallsrezepte und Rezeptauswahl.
- `Controllers`: API-Endpunkte fuer Authentifizierung, Benutzer, Kochbuecher, Rezepte, Kalender, Einstellungen, Jobs und Exporte.
- `Services`: Fach- und Infrastrukturdienste.
- `Services/Import`: Import-Orchestrierung und Handler fuer Backups, Webseiten und KI-Quellen.
- `Services/BackgroundJobs`: einfache Hintergrundjob-Queue fuer asynchrone Arbeiten.
- `Data` und `Entities`: EF-Core-DbContext und Persistenzmodell.
- `Migrations`: EF-Core-Migrationen.
- `wwwroot`: statische Assets, CSS, JavaScript und PWA-Manifest.

## `Rezepte.Tests`

- xUnit-Testprojekt fuer Service-Logik.
- Verwendet FluentAssertions, Moq, EF-Core InMemory und Testhilfen fuer Gemini-Isolation.
