# Bestandsaufnahme: Migration .NET 10 → .NET 11 (Issue #233)

Analysiert wurde die komplette `Rezepte.sln`-Toolchain — zehn C#-Projekte, Build-Grundlagen, Plugin-Mechanik, CI/CD-Workflows, Release-Publish, Skripte und Betriebsdokumentation — bezogen auf die Anforderung, den gesamten Stand konsistent von `net10.0`/.NET 10 auf `net11.0`/.NET 11 zu heben.

## Zusammenfassung

- **TFM-Ist-Zustand:** Neun von zehn Projekten setzen explizit `<TargetFramework>net10.0</TargetFramework>`; `Rezepte.Import.PluginSdk` erbt `net10.0` über den Conditional-Fallback in `Directory.Build.props` (Zeile 3). `global.json` existiert nicht.
- **Paketstände:** Alle zwölf `Microsoft.*`-Direktreferenzen sind einheitlich auf `10.0.15` gepinnt. Drittpakete u. a. `Serilog.AspNetCore` 10.0.0, `Google.Cloud.Vision.V1` 3.8.0, `QuestPDF` 2026.7.0, `Microsoft.Playwright` 1.65.1, Test-Stack `xunit` 2.9.3 + `bunit` 1.40.0 + `AngleSharp` 1.7.3 (die Updates xunit.v3/bunit 2.x bzw. QuestPDF ≥ 2026.9.x sind explizit Gegenstand der Folge-Issues #228/#229).
- **Kritischer Befund `msTools.Updater`:** Das lokale Paket `lib/nuget/msTools.Updater.0.10.4.nupkg` enthält ausschließlich ein `lib/net10.0`-Asset — es hat kein `net11.0`- oder `netstandard`-Ziel. Bezug in drei Projekten (`Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Updater.TestHost`).
- **SDK-Pins in CI/CD:** `dotnet-version: '10.0.x'` an 8 Stellen (je 3× `pr-staging-ci.yml`, 3× `staging-ci.yml`, 1× `release.yml`, 1× `security-scan.yml`) plus `build-and-package`-Action (Zeile 28) mit zwei hart codierten `--framework net10.0`-Publishes (Zeilen 48, 63).
- **Skripte:** `scripts/Export-ImportContract.ps1` Zeile 585 erwartet Vertrags-Assemblies unter `bin/Release/net10.0`; `scripts/screenshots/generate-screenshots.js` Zeile 10 hat einen hart codierten `net10.0`-Publish-Pfad (in der Anforderung nicht genannt).
- **Tests mit `net10.0`-Bezug:** `ContractExportScriptTests.cs` (Fake-Pfad `bin/Release/net10.0`, Zeilen 269/271) und `DeploymentDocumentationTests.cs` (assertiert `-f net10.0` in `install.md`, Zeile 68).
- **Dokumentation:** .NET-10-Bezüge in `install.md` (9 Stellen inkl. `aspnetcore-runtime-10.0`/`dotnet10`/`-f net10.0`), `dependencies.md` (3 Stellen), `help/github-actions.md` (3 Stellen), `screenshot-generation.md` (1), `Anforderungskatalog.md` (1).
- **Plugin-Mechanik:** MSBuild-Targets (`BuildImportPlugins`, `CopyImportPlugins*`, `PublishRezepteWebForBrowserTests`) sind TFM-agnostisch über `$(TargetFramework)` formuliert; Laufzeit-Laden erfolgt über `AssemblyLoadContext`-Subklasse `PluginLoadContext` in `PluginManager`. Externe Plugins können aus `external/rezepte-import-plugins-private/artifacts/plugins` (optionaler Pfad) oder zur Laufzeit aus `plugins/`-Verzeichnissen kommen.
- **EF Core:** 15+ Migrationsdateien; `RezepteDbContextModelSnapshot` trägt `ProductVersion`-Annotation `10.0.11` (Snapshot-Tooling-Stand), Paketstand `10.0.15`.

**Test-Ausgangszustand:** Alle Läufe auf Baseline-Commit `aa9263e` (Staging-Head) mit SDK 11.0.100 gegen `net10.0`/Runtime 10.0.15: `restore` und `build` (Release, 0 Warnungen/0 Fehler) erfolgreich; `dotnet test Rezepte.sln` = **809/809 Tests bestanden, 0 fehlgeschlagen, 0 übersprungen** (davon `Rezepte.Tests` 774: 759 deterministische Lane + 15 `OsInterface`; `Rezepte.Tests.Browser` 35); Vulnerability-Scan ohne Befund. Details und Nachweise: [inventory/tests.md](inventory/tests.md).

## Details

- [Projektdateien: TFM und Paketstände](inventory/projektdateien.md) — alle zehn `.csproj`, `Directory.Build.props`, `NuGet.config`, `msTools.Updater`-Asset-Struktur
- [Build- und Plugin-Mechanik](inventory/build-und-plugin-mechanik.md) — MSBuild-Targets, `PluginManager`/`PluginLoadContext`, EF-Migrationsstand, Contract-Export/Baselines
- [CI/CD, Composite Actions und Skripte](inventory/ci-cd-und-skripte.md) — SDK-Pins, `--framework net10.0`-Publishes, TFM-Bezüge in Skripten, projektübliche CLI-Befehle
- [Vertrags-Interfaces](inventory/interfaces.md) — `IImportPlugin` & Co. (`ImportContractVersion` 0.3.0), PluginSdk, Typidentität Host↔Plugin
- [Betriebsdokumentation](inventory/dokumentation.md) — .NET-10-Bezüge in `Docs/` (ohne historische Feature-/Projekt-Aufzeichnungen)
- [Tests](inventory/tests.md) — Ausgangszustand, Lauf-Nachweise, Testklassen- und Hilfsmethoden-Bestand
