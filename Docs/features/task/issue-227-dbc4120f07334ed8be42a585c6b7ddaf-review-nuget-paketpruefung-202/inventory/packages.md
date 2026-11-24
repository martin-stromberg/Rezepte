# Paketinventar: NuGet-Referenzen der Solution `Rezepte.sln`

Stand der Aufnahme: 2026-11-24, Branch `task/issue-227-dbc4120f07334ed8be42a585c6b7ddaf-review-nuget-paketpruefung-202`, Commit `6f7982882b5ef3515491356c135394fa275718eb`.

Die Solution enthält 10 Projekte (Quelle: `Rezepte.sln`). Sechs Projekte haben direkte
`<PackageReference>`-Einträge; vier Projekte haben keine (`Rezepte.Import.Abstractions`,
`Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.Backup`,
`Rezepte.Tests.PluginFixture`). Insgesamt 37 direkte `PackageReference`-Einträge auf
28 verschiedene Pakete.

Zentrale Build-Vorgaben (`Directory.Build.props`): `TargetFramework` `net10.0`,
`Nullable` `enable`, `ImplicitUsings` `enable`, `ImportContractVersion` `0.3.0`.
Alle `*.csproj` setzen `GenerateDocumentationFile` = `true` und
`WarningsAsErrors` = `CS1591`. Kein `global.json` im Repository (SDK-Auswahl erfolgt
maschinenseitig; lokal installiert: `10.0.404`, `11.0.100`; CI pinnt `10.0.x`).

## Paketquellen

Datei: `NuGet.config`

| Quelle | Wert | Inhalt |
|--------|------|--------|
| `local-repo-packages` | `lib/nuget` | Lokaler Feed, enthält `msTools.Updater.0.10.4.nupkg` (einzige Datei im Verzeichnis) |
| `nuget.org` | `https://api.nuget.org/v3/index.json` | Öffentlicher Feed |

Hinweis aus dem Restore-/Scan-Log: Die dotnet-CLI nutzte zusätzlich den
maschinenseitigen Fallback-Ordner `C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\`
(Nachweis: [dotnet-list-package-outdated.log](test-results/dotnet-list-package-outdated.log)).
`msTools.Updater` wird ausschließlich über den lokalen Feed aufgelöst und ist auf
nuget.org nicht bewertbar.

## `Rezepte.Web` — `Rezepte.Web/Rezepte.Web.csproj`

Sdk: `Microsoft.NET.Sdk.Web`. Enthält eigene MSBuild-Targets
(`BuildImportPlugins`, `CopyImportPluginsToOutput`, `CopyImportPluginsToPublish`),
die die drei Import-Plugin-Projekte (`Backup`, `AIFoto`, `AIUrl`) über
`<ImportPluginProject>` einbinden und deren Ausgabe nach `plugins/` kopieren.

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Google.Cloud.Vision.V1` | 3.8.0 | Google-Vision-OCR für den KI-Foto-Import |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | JWT-Auth (Drift: 10.0.9) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | `PrivateAssets`=`all`, `IncludeAssets`=`runtime; build; native; contentfiles; analyzers; buildtransitive` |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | SQLite-Provider (Drift: 10.0.11) |
| `QuestPDF` | 2026.7.0 | PDF-Erzeugung |
| `Serilog.AspNetCore` | 10.0.0 | Logging-Integration (drittparty-versioniert, kein `Microsoft.*`) |
| `Serilog.Sinks.Console` | 6.1.1 | |
| `Serilog.Sinks.File` | 7.0.0 | |
| `msTools.Updater` | 0.10.4 | Nur lokaler Feed `lib/nuget` |

`ProjectReference`: `Rezepte.Import.Abstractions`.

## `Rezepte.Tests` — `Rezepte.Tests/Rezepte.Tests.csproj`

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.NET.Test.Sdk` | 18.7.0 | |
| `xunit` | 2.9.3 | Als `Legacy`/`deprecated` gemeldet, Alternative `xunit.v3` (Nachweis: [dotnet-list-package-deprecated.log](test-results/dotnet-list-package-deprecated.log)) |
| `xunit.runner.visualstudio` | 3.1.5 | `PrivateAssets`=`all`, `IncludeAssets`=`runtime; build; native; contentfiles; analyzers; buildtransitive` |
| `FluentAssertions` | 8.10.0 | |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.9 | Drift: 10.0.9 |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.11 | Drift: 10.0.11 |
| `Microsoft.Extensions.Caching.Memory` | 10.0.11 | Drift: 10.0.11 |
| `Moq` | 4.20.72 | |
| `msTools.Updater` | 0.10.4 | Nur lokaler Feed |
| `bunit` | 1.40.0 | |
| `AngleSharp` | 1.7.3 | Direkter Pin (Security-Fix gemäß `docs/dependencies.md`) |
| `coverlet.collector` | 10.0.1 | `PrivateAssets`=`all`, `IncludeAssets`=`runtime; build; native; contentfiles; analyzers; buildtransitive` |

`ProjectReference`: `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`,
`Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.AIFoto`,
`Rezepte.Import.Plugins.AIUrl`, `Rezepte.Tests.PluginFixture`, `Rezepte.Web`.

## `Rezepte.Tests.Browser` — `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.NET.Test.Sdk` | 18.7.0 | |
| `xunit` | 2.9.3 | `deprecated` (Legacy), siehe oben |
| `xunit.runner.visualstudio` | 3.1.5 | `PrivateAssets`/`IncludeAssets` wie in `Rezepte.Tests` |
| `FluentAssertions` | 8.10.0 | |
| `Microsoft.Playwright` | 1.61.0 | Browser-Automatisierung |
| `Xunit.SkippableFact` | 1.5.61 | |
| `coverlet.collector` | 10.0.1 | `PrivateAssets`/`IncludeAssets` wie in `Rezepte.Tests` |

`ProjectReference`: `Rezepte.Web`. Zusätzlich verlinkte `<Compile>`-Dateien aus
`Rezepte.Tests/TestHelpers` (`RepositoryPaths.cs`, `TestCategories.cs`,
`TempDirectoryCleanup.cs`, `FlakyConventionTests.cs`) und das Target
`PublishRezepteWebForBrowserTests` (publisht `Rezepte.Web` vor dem Testlauf).

## `Rezepte.Import.Plugins.AIFoto` — `Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj`

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Google.Cloud.Vision.V1` | 3.8.0 | |
| `Microsoft.Extensions.Caching.Abstractions` | 10.0.9 | Drift: 10.0.9 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 | Drift: 10.0.9 |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.9 | Drift: 10.0.9 |
| `Microsoft.Extensions.Options` | 10.0.9 | Drift: 10.0.9 |

Keine `ProjectReference` auf `Rezepte.Web`, sondern `<Reference Include="Rezepte.Web">`
mit `HintPath` (`$(RezepteWebReferencePath)` bzw.
`..\Rezepte.Web\bin\$(Configuration)\$(TargetFramework)\Rezepte.Web.dll`,
`Private`=`false`) — dokumentierte Build-Reihenfolgen-Falle, siehe
[tests.md](tests.md).

## `Rezepte.Import.Plugins.AIUrl` — `Rezepte.Import.Plugins.AIUrl/Rezepte.Import.Plugins.AIUrl.csproj`

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 | Drift: 10.0.9 |
| `Microsoft.Extensions.Logging` | 10.0.9 | Drift: 10.0.9 |

Gleiche `Rezepte.Web`-`Reference`/`HintPath`-Konstruktion wie `AIFoto`.

## `Rezepte.Updater.TestHost` — `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.Extensions.Hosting` | 10.0.0 | Drift: 10.0.0 (älteste `Microsoft.*`-Version im Repo) |
| `msTools.Updater` | 0.10.4 | Nur lokaler Feed |

`OutputType` `Exe`. Referenziert `..\..\..\..\Dashboard\Rezepte\appsettings.Production.json`
als `<Content>`-Link (Pfad außerhalb des Repos — Datei liegt lokal nicht vor, wird
aber nur als Content deklariert).

## Projekte ohne Paketreferenzen

| Projekt | Referenzen |
|---------|-----------|
| `Rezepte.Import.Abstractions` | Keine `PackageReference`, keine `ProjectReference`; `Version` aus `$(ImportContractVersion)` = 0.3.0 |
| `Rezepte.Import.PluginSdk` | `ProjectReference` → `Rezepte.Import.Abstractions`; `Version` 0.3.0 |
| `Rezepte.Import.Plugins.Backup` | `ProjectReference` → `Rezepte.Import.Abstractions` |
| `Rezepte.Tests.PluginFixture` | `ProjectReference` → `Rezepte.Import.Abstractions` |

## Versionsdrift `Microsoft.*` / `System.*`

Direkte `Microsoft.*`-Referenzen verteilen sich auf drei Stände der .NET-10-Linie:

| Version | Pakete |
|---------|--------|
| 10.0.0 | `Microsoft.Extensions.Hosting` (`Rezepte.Updater.TestHost`) |
| 10.0.9 | `Microsoft.AspNetCore.Authentication.JwtBearer` (`Rezepte.Web`), `Microsoft.AspNetCore.Mvc.Testing` (`Rezepte.Tests`), `Microsoft.Extensions.Caching.Abstractions`/`.DependencyInjection.Abstractions`/`.Logging.Abstractions`/`.Options` (`Rezepte.Import.Plugins.AIFoto`), `Microsoft.Extensions.DependencyInjection.Abstractions`/`.Logging` (`Rezepte.Import.Plugins.AIUrl`) |
| 10.0.11 | `Microsoft.EntityFrameworkCore.Design`/`.Sqlite` (`Rezepte.Web`), `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.Extensions.Caching.Memory` (`Rezepte.Tests`) |

`System.*`-Direktreferenzen gibt es nicht. `Serilog.AspNetCore` 10.0.0 folgt nur dem
.NET-Namensschema und ist kein `Microsoft.*`-Paket.

## Befund-Snapshot zum Stichtag (read-only `dotnet list`-Befehle)

Ausgeführt am 2026-11-24 (MEZ), Repo-Root, SDK `11.0.100`, Ausgaben unter
[test-results/](test-results/):

| Befehl | Exit | Ergebnis | Nachweis |
|--------|------|----------|----------|
| `dotnet restore Rezepte.sln` | 0 | Alle Projekte auf dem neuesten Stand | [dotnet-restore-sln.log](test-results/dotnet-restore-sln.log) |
| `dotnet list Rezepte.sln package --vulnerable --include-transitive` | 0 | Keine verwundbaren Pakete in allen 10 Projekten (entspricht dem CI-Gate aus `.github/actions/security-scan`) | [dotnet-list-package-vulnerable.log](test-results/dotnet-list-package-vulnerable.log) |
| `dotnet list Rezepte.sln package --deprecated` | 0 | `xunit` 2.9.3 in `Rezepte.Tests` + `Rezepte.Tests.Browser` als `Legacy` markiert, Alternative `xunit.v3` | [dotnet-list-package-deprecated.log](test-results/dotnet-list-package-deprecated.log) |
| `dotnet list Rezepte.sln package --outdated` | 0 | Update-Kandidaten je Projekt, siehe Tabelle unten | [dotnet-list-package-outdated.log](test-results/dotnet-list-package-outdated.log) |

### Update-Kandidaten laut `dotnet list --outdated` (Stichtag 2026-11-24)

Wichtig: Die Spalte „Neueste" der CLI gibt die absolute neueste Version auf
nuget.org wieder — für `Microsoft.*`/`AspNetCore`/`EFCore`/`Extensions` ist das
bereits `11.0.0`. Die Anforderung schließt `11.x` aus (nur .NET-10-Linie); die
jeweils höchste `10.0.x` ist aus dieser Ausgabe nicht direkt ablesbar und muss bei
der Umsetzung separat ermittelt werden.

| Projekt | Paket | Installiert | Neueste (CLI) | Update-Typ |
|---------|-------|------------|---------------|------------|
| Rezepte.Web | `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | 11.0.0 | Major (außerhalb .NET-10-Linie) |
| Rezepte.Web | `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Web | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Web | `QuestPDF` | 2026.7.0 | 2026.9.2 | Datumsbasiert (Minor-artig) |
| Rezepte.Web | `Serilog.AspNetCore` | 10.0.0 | 11.0.0 | Major |
| Rezepte.Tests | `AngleSharp` | 1.7.3 | 1.10.5 | Minor |
| Rezepte.Tests | `bunit` | 1.40.0 | 2.15.2 | Major |
| Rezepte.Tests | `coverlet.collector` | 10.0.1 | 10.1.1 | Minor |
| Rezepte.Tests | `FluentAssertions` | 8.10.0 | 8.11.0 | Minor |
| Rezepte.Tests | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Tests | `Microsoft.EntityFrameworkCore.InMemory` | 10.0.11 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Tests | `Microsoft.Extensions.Caching.Memory` | 10.0.11 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Tests | `Microsoft.NET.Test.Sdk` | 18.7.0 | 18.12.0 | Minor |
| Rezepte.Tests | `Moq` | 4.20.72 | 4.21.0 | Minor |
| Rezepte.Tests | `xunit.runner.visualstudio` | 3.1.5 | 4.0.1 | Major |
| Rezepte.Import.Plugins.AIFoto | `Microsoft.Extensions.Caching.Abstractions` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Import.Plugins.AIFoto | `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Import.Plugins.AIFoto | `Microsoft.Extensions.Logging.Abstractions` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Import.Plugins.AIFoto | `Microsoft.Extensions.Options` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Import.Plugins.AIUrl | `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Import.Plugins.AIUrl | `Microsoft.Extensions.Logging` | 10.0.9 | 11.0.0 | Major (außerhalb Linie) |
| Rezepte.Tests.Browser | `coverlet.collector` | 10.0.1 | 10.1.1 | Minor |
| Rezepte.Tests.Browser | `FluentAssertions` | 8.10.0 | 8.11.0 | Minor |
| Rezepte.Tests.Browser | `Microsoft.NET.Test.Sdk` | 18.7.0 | 18.12.0 | Minor |
| Rezepte.Tests.Browser | `Microsoft.Playwright` | 1.61.0 | 1.65.1 | Minor |
| Rezepte.Tests.Browser | `xunit.runner.visualstudio` | 3.1.5 | 4.0.1 | Major |
| Rezepte.Tests.Browser | `Xunit.SkippableFact` | 1.5.61 | 1.5.85 | Patch |
| Rezepte.Updater.TestHost | `Microsoft.Extensions.Hosting` | 10.0.0 | 11.0.0 | Major (außerhalb Linie) |

Nicht als veraltet gemeldet: `Google.Cloud.Vision.V1` 3.8.0, `Serilog.Sinks.Console`
6.1.1, `Serilog.Sinks.File` 7.0.0, `xunit` 2.9.3 (stattdessen `deprecated`/Legacy),
`msTools.Updater` 0.10.4 (lokaler Feed, für die CLI nicht bewertbar — offene Frage
Nr. 2 der Anforderung).

## Strategie-Dokument `docs/dependencies.md`

Bestandsstand (Dokumentdatum 2026-09-03): die früheren Security-Befunde
(`SQLitePCLRaw.lib.e_sqlite3` 2.1.11, `AngleSharp` 1.2.0 transitiv) wurden durch
Updates auf `Microsoft.EntityFrameworkCore.Sqlite` 10.0.11, `bunit` 1.40.0 und
direkten `AngleSharp`-Pin 1.7.3 behoben; `dotnet list --vulnerable --include-transitive`
meldet aktuell keinen Befund (verifiziert, siehe oben). Strategie-Vorgaben:
kein direkter `SQLitePCLRaw.lib.e_sqlite3`-Pin, `NU1903` nicht unterdrückt,
Security-Scan bleibt blockierendes CI-Gate, kein DB-/Providerwechsel.
