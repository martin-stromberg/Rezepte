# Projektdateien: TFM und Paketstände

Bestandsaufnahme aller zehn Projekte der `Rezepte.sln` sowie der solution-weiten Build-Grundlagen, bezogen auf die Migration `net10.0` → `net11.0`.

## `Directory.Build.props`

Datei: `Directory.Build.props`

| Eigenschaft | Wert | Zeile | Zweck |
|-------------|------|-------|-------|
| `TargetFramework` (Condition: nur wenn nicht gesetzt) | `net10.0` | 3 | Fallback-TFM für Projekte ohne eigenen TFM |
| `Nullable` | `enable` | 4 | |
| `ImplicitUsings` | `enable` | 5 | |
| `ImportContractVersion` | `0.3.0` | 6 | Versionierung der Vertrags-Assemblies (`Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`) und des Contract-Exports |

`global.json` existiert nicht (verifiziert: `ls global.json` → nicht gefunden). SDK-Auflösung erfolgt über das höchste installierte SDK.

## `NuGet.config`

Datei: `NuGet.config`

- Quelle `local-repo-packages` → `lib/nuget` (lokale Pakete)
- Quelle `nuget.org` → `https://api.nuget.org/v3/index.json`
- `lib/nuget` enthält ausschließlich `msTools.Updater.0.10.4.nupkg`. Das Paket enthält nur ein `lib/net10.0/`-Asset (`lib/net10.0/msTools.Updater.dll` + `.xml`) — kein `netstandard`- oder `net11.0`-Ziel.

## `Rezepte.Web`

Datei: `Rezepte.Web/Rezepte.Web.csproj` — SDK `Microsoft.NET.Sdk.Web`

- `TargetFramework`: `net10.0` (Zeile 4)
- `ExternalImportPluginsPath`: `$(MSBuildProjectDirectory)\..\external\rezepte-import-plugins-private\artifacts\plugins` (Zeile 9) — optionale externe Plugin-Quelle, nur kopiert wenn vorhanden
- Projektreferenz: `Rezepte.Import.Abstractions` (Zeile 50)
- `ImportPluginProject`-Items (Zeilen 54–62): `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`
- MSBuild-Targets `BuildImportPlugins`, `CopyImportPluginsToOutput`, `CopyImportPluginsToPublish` (Zeilen 65–91), siehe [build-und-plugin-mechanik.md](build-und-plugin-mechanik.md)

Paketreferenzen:

| Paket | Version | Anmerkung |
|-------|---------|-----------|
| `Google.Cloud.Vision.V1` | `3.8.0` | Drittpaket |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.15` | `Microsoft.*` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.15` | `PrivateAssets=all` |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.15` | `Microsoft.*` |
| `QuestPDF` | `2026.7.0` | Drittpaket; Update ≥ 2026.9.x ist Gegenstand von Issue #229 |
| `Serilog.AspNetCore` | `10.0.0` | Drittpaket |
| `Serilog.Sinks.Console` | `6.1.1` | Drittpaket |
| `Serilog.Sinks.File` | `7.0.0` | Drittpaket |
| `msTools.Updater` | `0.10.4` | lokales `.nupkg` aus `lib/nuget` (nur `net10.0`-Asset) |

## `Rezepte.Import.Abstractions`

Datei: `Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj` — SDK `Microsoft.NET.Sdk`

- `TargetFramework`: `net10.0` (Zeile 4)
- `Version`/`AssemblyVersion`/`FileVersion`: `$(ImportContractVersion)` = `0.3.0` (Zeilen 7–9)
- Keine Paketreferenzen.
- Öffentliche Vertragsfläche: `IImportPlugin`, `IImportHandler`, `ICollectionImportHandler`, `IImportInteraction`, `IInteractiveImportHandler` sowie DTOs (`ImportResult`, `ImportedRecipe` u. a.), siehe [interfaces.md](interfaces.md)

## `Rezepte.Import.PluginSdk`

Datei: `Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj` — SDK `Microsoft.NET.Sdk`

- **Kein eigener `TargetFramework`** — erbt `net10.0` aus `Directory.Build.props` Zeile 3 (verifiziert: kein `<TargetFramework>`-Element in der Datei)
- `Version`/`AssemblyVersion`/`FileVersion`: `$(ImportContractVersion)` = `0.3.0` (Zeilen 5–7)
- Projektreferenz: `Rezepte.Import.Abstractions` (Zeile 13)
- Keine Paketreferenzen.
- Enthält `ImportParserBase`, `ParsedIngredient`, `UrlHelpers`.

## `Rezepte.Import.Plugins.Backup`

Datei: `Rezepte.Import.Plugins.Backup/Rezepte.Import.Plugins.Backup.csproj`

- `TargetFramework`: `net10.0` (Zeile 3)
- Projektreferenz: `Rezepte.Import.Abstractions`; keine Paketreferenzen.

## `Rezepte.Import.Plugins.AIFoto`

Datei: `Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj`

- `TargetFramework`: `net10.0` (Zeile 3)
- Projektreferenz: `Rezepte.Import.Abstractions` (Zeile 11)
- Bedingte `Reference` auf `Rezepte.Web.dll` per `HintPath`: `$(RezepteWebReferencePath)` falls gesetzt, sonst `..\Rezepte.Web\bin\$(Configuration)\$(TargetFramework)\Rezepte.Web.dll` (Zeilen 18–29)

| Paket | Version |
|-------|---------|
| `Google.Cloud.Vision.V1` | `3.8.0` |
| `Microsoft.Extensions.Caching.Abstractions` | `10.0.15` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.15` |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.15` |
| `Microsoft.Extensions.Options` | `10.0.15` |

## `Rezepte.Import.Plugins.AIUrl`

Datei: `Rezepte.Import.Plugins.AIUrl/Rezepte.Import.Plugins.AIUrl.csproj`

- `TargetFramework`: `net10.0` (Zeile 3)
- Projektreferenz: `Rezepte.Import.Abstractions` (Zeile 15)
- Bedingte `Reference` auf `Rezepte.Web.dll` per `HintPath` wie bei AIFoto (Zeilen 17–28)

| Paket | Version |
|-------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.15` |
| `Microsoft.Extensions.Logging` | `10.0.15` |

## `Rezepte.Tests`

Datei: `Rezepte.Tests/Rezepte.Tests.csproj`

- `TargetFramework`: `net10.0` (Zeile 3), `IsPackable=false`
- Projektreferenzen (Zeilen 31–37): `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, alle drei Plugin-Projekte, `Rezepte.Tests.PluginFixture`, `Rezepte.Web`

| Paket | Version | Anmerkung |
|-------|---------|-----------|
| `Microsoft.NET.Test.Sdk` | `18.12.0` | |
| `xunit` | `2.9.3` | xunit.v3-Migration ist Issue #228 |
| `xunit.runner.visualstudio` | `4.0.1` | `PrivateAssets=all` |
| `FluentAssertions` | `8.11.0` | |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.15` | `Microsoft.*` |
| `Microsoft.EntityFrameworkCore.InMemory` | `10.0.15` | `Microsoft.*` |
| `Microsoft.Extensions.Caching.Memory` | `10.0.15` | `Microsoft.*` |
| `Moq` | `4.21.0` | |
| `msTools.Updater` | `0.10.4` | lokales `.nupkg` (nur `net10.0`-Asset) |
| `bunit` | `1.40.0` | bunit-2.x-Migration ist Issue #228 |
| `AngleSharp` | `1.7.3` | direkter Pin, an bunit gekoppelt (siehe `Docs/dependencies.md`) |
| `coverlet.collector` | `10.1.1` | `PrivateAssets=all` |

Hart codierte `net10.0`-Pfade in Tests:

- `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` Zeilen 269/271: legt Fake-Verzeichnis `Rezepte.Import.PluginSdk/bin/Release/net10.0` an (Test des Aufräumverhaltens von `Export-ImportContract.ps1`)
- `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` Zeile 68: erwartete Dokumentationszeichenfolge `publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false`

## `Rezepte.Tests.Browser`

Datei: `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`

- `TargetFramework`: `net10.0` (Zeile 3), `IsPackable=false`
- Projektreferenz: `Rezepte.Web` (Zeile 26); verlinkte Compile-Items aus `Rezepte.Tests/TestHelpers` (Zeilen 29–32)
- Target `PublishRezepteWebForBrowserTests` (Zeilen 35–37): publiziert `Rezepte.Web` via `$(TargetFramework)`, `AfterTargets="ResolveProjectReferences"`, mit `Inputs`/`Outputs` auf `net10.0`-Pfaden (über `$(TargetFramework)` aufgelöst)

| Paket | Version |
|-------|---------|
| `Microsoft.NET.Test.Sdk` | `18.12.0` |
| `xunit` | `2.9.3` |
| `xunit.runner.visualstudio` | `4.0.1` |
| `FluentAssertions` | `8.11.0` |
| `Microsoft.Playwright` | `1.65.1` |
| `Xunit.SkippableFact` | `1.5.85` |
| `coverlet.collector` | `10.1.1` |

## `Rezepte.Tests.PluginFixture`

Datei: `Rezepte.Tests.PluginFixture/Rezepte.Tests.PluginFixture.csproj`

- `TargetFramework`: `net10.0` (Zeile 4)
- Projektreferenz: `Rezepte.Import.Abstractions`; keine Paketreferenzen.

## `Rezepte.Updater.TestHost`

Datei: `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`

- `TargetFramework`: `net10.0` (Zeile 5), `OutputType=Exe`
- `Content Include="..\..\..\..\Dashboard\Rezepte\appsettings.Production.json"` (Zeile 15) — externer Pfad außerhalb des Repos

| Paket | Version |
|-------|---------|
| `Microsoft.Extensions.Hosting` | `10.0.15` |
| `msTools.Updater` | `0.10.4` |

## Zusammenfassung der `Microsoft.*`-Direktreferenzen

Alle auf `10.0.15` gepinnt:

- `Microsoft.AspNetCore.Authentication.JwtBearer` (`Rezepte.Web`)
- `Microsoft.EntityFrameworkCore.Design` (`Rezepte.Web`)
- `Microsoft.EntityFrameworkCore.Sqlite` (`Rezepte.Web`)
- `Microsoft.AspNetCore.Mvc.Testing` (`Rezepte.Tests`)
- `Microsoft.EntityFrameworkCore.InMemory` (`Rezepte.Tests`)
- `Microsoft.Extensions.Caching.Abstractions` (`AIFoto`)
- `Microsoft.Extensions.Caching.Memory` (`Rezepte.Tests`)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (`AIFoto`, `AIUrl`)
- `Microsoft.Extensions.Hosting` (`Updater.TestHost`)
- `Microsoft.Extensions.Logging` (`AIUrl`)
- `Microsoft.Extensions.Logging.Abstractions` (`AIFoto`)
- `Microsoft.Extensions.Options` (`AIFoto`)
