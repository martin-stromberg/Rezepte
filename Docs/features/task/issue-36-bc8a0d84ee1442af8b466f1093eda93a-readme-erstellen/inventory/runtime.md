# Betrieb und Konfiguration

## Anwendung

- Framework: .NET 9.
- Web-Stack: ASP.NET Core, Blazor Server, API-Controller.
- Datenbank: SQLite ueber EF Core.
- Logging: Serilog mit Console- und File-Sink; Logdateien liegen unter `logs/app-.log` im App-Basisverzeichnis.

## Konfiguration

- `ConnectionStrings:Default`: optionaler SQLite-Connection-String, Fallback `Data Source=rezepte.db`.
- `Jwt:Key`: Signaturschluessel fuer JWT. In Produktion ersetzen.
- `Images`: Upload-Grenzen und erlaubte Bildtypen.
- `AI:Simulate`, `AI:EnableCache`, `AI:CacheDurationHours`: optionale Einstellungen fuer KI-Importe und Caching.

Hinweis: `appsettings.json` definiert `Jwt:Issuer` und `Jwt:Audience`; die aktuelle JWT-Validierung verwendet jedoch feste Werte in `ServiceCollectionExtensions.cs`.

## Externe Dateien

- `google.application-credentials.json`: optionaler Google-Service-Account fuer Vision/Gemini.
- `google.gemini.api-key.json`: optionaler Gemini-API-Key.
- Beide Dateien werden laut Projektdatei ins Build-Ausgabeverzeichnis kopiert, sollten aber nicht als Secrets ins Repository.

## Deployment-Hinweis

`Docs/install.md` beschreibt ein framework-abhaengiges Linux-x64-Publish in `/var/www/rezepte` und den Betrieb als systemd-Service.
