# NuGet-Abhaengigkeiten

Quelle: `dotnet list Rezepte.sln package --outdated`, `dotnet list Rezepte.sln package --include-transitive` und `dotnet list Rezepte.sln package --vulnerable --include-transitive` am 2026-07-01.

Verwendete Paketquellen:

- `https://api.nuget.org/v3/index.json`
- `C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\`

## Direkte Paketverweise: Rezepte.Web

| Paket | Aktuell | Neueste laut NuGet | Hinweis |
|---|---:|---:|---|
| Google.Cloud.Vision.V1 | 3.7.0 | 3.8.0 | OCR/Google-Vision-Nutzung in Importlogik. |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.9 | 10.0.9 | Muss zum Ziel-Framework passen. |
| Microsoft.EntityFrameworkCore.Design | 9.0.9 | 10.0.9 | PrivateAssets/IncludeAssets gesetzt; Design-Time-Migrationen pruefen. |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.9 | 10.0.9 | Datenzugriff und SQLite-Provider; transitiver Vulnerability-Fix relevant. |
| QuestPDF | 2025.7.2 | 2026.6.1 | PDF-Generator nutzt API, bestehende Obsolete-Warnung moeglich. |
| Serilog.AspNetCore | 8.0.2 | 10.0.0 | Logging-Integration in `LoggingExtensions`. |
| Serilog.Sinks.Console | 5.0.1 | 6.1.1 | Console/systemd Logging. |
| Serilog.Sinks.File | 5.0.0 | 7.0.0 | Rolling file logging. |

## Direkte Paketverweise: Rezepte.Tests

| Paket | Aktuell | Neueste laut NuGet | Hinweis |
|---|---:|---:|---|
| Microsoft.NET.Test.Sdk | 17.11.1 | 18.7.0 | Testhost/Runner-Kompatibilitaet mit .NET 10 pruefen. |
| xunit | 2.9.0 | 2.9.3 | Kleines Update innerhalb xUnit v2. |
| xunit.runner.visualstudio | 2.8.2 | 3.1.5 | Major-Update; Runner-Verhalten pruefen. |
| FluentAssertions | 6.12.0 | 8.10.0 | Major-Update; Assertion-API/License-Hinweise pruefen. |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.9 | 10.0.9 | Sollte mit EF-Core-Hauptpaketen zusammen aktualisiert werden. |
| Microsoft.Extensions.Caching.Memory | 9.0.9 | 10.0.9 | Test- und Produktivnutzung ueber Services. |
| Moq | 4.20.72 | nicht als veraltet gemeldet | Beibehalten oder erneut pruefen. |
| coverlet.collector | 6.0.0 | 10.0.1 | Major-Update; Coverage-Verhalten pruefen. |

## Auffaellige transitive Pakete

Die transitive Liste ist umfangreich. Fuer die Planung besonders relevant:

- EF-Core-/SQLite-Kette: `Microsoft.EntityFrameworkCore*` `9.0.9`, `Microsoft.Data.Sqlite.Core` `9.0.9`, `SQLitePCLRaw.*` `2.1.10`
- ASP.NET/JWT-Kette: `Microsoft.IdentityModel.*` `8.0.1`, `System.IdentityModel.Tokens.Jwt` `8.0.1`
- Google/Grpc-Kette: `Google.Api.*`, `Google.Apis.*`, `Grpc.*`, `Google.Protobuf`
- Serilog-Kette: `Serilog` `3.1.1`, `Serilog.Extensions.*` `8.0.0`, `Serilog.Settings.Configuration` `8.0.2`
- Test-Kette: `Microsoft.TestPlatform.*` `17.11.1`, `xunit.*`, `Castle.Core` `5.1.1`

## Vulnerability-Befund

`dotnet list package --vulnerable --include-transitive` meldet in beiden Projekten:

| Paket | Version | Schweregrad | Advisory |
|---|---:|---|---|
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.10 | High | `https://github.com/advisories/GHSA-2m69-gcr7-jv3q` |

Upgrade-Relevanz:

- Der Befund kommt transitiv ueber die SQLite/EF-Core-Abhaengigkeiten.
- Bei Aktualisierung auf EF Core `10.0.9` ist zu pruefen, ob die transitive `SQLitePCLRaw`-Version automatisch aktualisiert wird.
- Falls nicht, sollte eine explizite Paketstrategie geplant werden.

## Paketnutzung im Code

Direkte Nutzungsschwerpunkte:

- `Microsoft.EntityFrameworkCore`: `Rezepte.Web/Data`, Services, Controller, Background Jobs, Migrations und Tests
- `Microsoft.AspNetCore.Authentication.JwtBearer`: mehrere Controller und `ServiceCollectionExtensions`
- `QuestPDF`: `Rezepte.Web/Services/PdfGenerator.cs`
- `Serilog`: `Rezepte.Web/Extensions/LoggingExtensions.cs`
- `Google.Cloud.Vision.V1`: `Rezepte.Web/Services/Import/AIFotoImportHandler.cs`
- `Microsoft.Extensions.Caching.Memory`: `TokenService`, `AIFotoImportHandler`, Tests
- `FluentAssertions`, `Moq`, `xunit`: Testprojekt
