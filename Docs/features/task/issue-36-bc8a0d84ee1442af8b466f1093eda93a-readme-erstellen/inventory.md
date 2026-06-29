# Bestandsaufnahme

## Zusammenfassung

Das Repository enthaelt eine .NET-9-Solution fuer eine deutschsprachige Rezeptverwaltung. Die Webanwendung ist eine Blazor-Server-App mit integrierten API-Controllern, EF-Core/SQLite-Persistenz, Cookie- und JWT-Authentifizierung, Rezept- und Kochbuchverwaltung, Kalender, Import-/Export-Funktionen sowie optionalen KI-Importen ueber Google Vision/Gemini. Tests liegen in einem separaten xUnit-Projekt.

## Detaildokumente

- [Projektstruktur](inventory/project-structure.md)
- [Betrieb und Konfiguration](inventory/runtime.md)

## Wichtige Quellen

- `Rezepte.sln`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Web/Program.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Data/RezepteDbContext.cs`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Web/Properties/launchSettings.json`
- `Docs/Anforderungskatalog.md`
- `Docs/install.md`

## Dokumentationsrelevante Erkenntnisse

- Lokaler Start erfolgt ueber `dotnet run --project Rezepte.Web`; das Launch-Profil nutzt `http://localhost:5220`.
- Tests werden ueber `dotnet test` ausgefuehrt.
- Die Standarddatenbank ist `rezepte.db` im Arbeitsverzeichnis der Anwendung, sofern keine Connection String `Default` konfiguriert ist.
- EF-Core-Migrationen werden beim Anwendungsstart automatisch angewendet; ohne Migrationen wird die Datenbank erstellt.
- Authentifizierung nutzt Website-Cookie und JWT fuer API-Requests.
- Google-KI-Funktionen benoetigen optionale Credential-Dateien im Build-Ausgabeverzeichnis.
- Die JWT-Konfiguration in `appsettings.json` enthaelt Issuer/Audience, die Validierung in `ServiceCollectionExtensions.cs` nutzt aktuell jedoch feste Werte.
