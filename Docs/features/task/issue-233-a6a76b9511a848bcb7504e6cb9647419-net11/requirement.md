# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `a6a76b95-11a8-48bc-b750-4e6cb9647419`
- Branch: `task/issue-233-a6a76b9511a848bcb7504e6cb9647419-net11`
- Verknüpftes Issue: `martin-stromberg/Rezepte` #233 — „.NET11"
- Kundenanforderung: Das Projekt wird von .NET 10 auf .NET 11 migriert. Ziel ist ein konsistenter Stand aus Target Framework, SDK-Pins, `Microsoft.*`-Paketen, CI/CD, Release-Publish, Plugin-Mechanismus, Vertrags-/Tooling-Skripten und Dokumentation auf `net11.0` bzw. .NET 11. Der PR läuft gegen `staging`; als Folge-Beziehung ist der Umgang mit den Issues #228 (xunit.v3/bunit-Test-Stack-Migration) und #229 (QuestPDF-Font-/Settings-Migration) zu entscheiden und zu dokumentieren.

## Fachliche Zusammenfassung

Die gesamte Solution (zehn C#-Projekte) wird vom Target Framework `net10.0` auf `net11.0` gehoben — explizite TFM-Angaben in den Projektdateien ebenso wie der Fallback in `Directory.Build.props`, von dem `Rezepte.Import.PluginSdk` seinen TFM erbt. Alle `Microsoft.*`-Direktreferenzen werden einheitlich auf eine gepinnte `11.0.x`-Linie angehoben; Drittpakete (u. a. `Serilog.AspNetCore`, `Google.Cloud.Vision.V1`, `QuestPDF`, `msTools.Updater`, `Microsoft.Playwright`, Test-/Coverage-Pakete) werden auf `net11.0`-Kompatibilität geprüft und erforderlichenfalls begründet aktualisiert. Gleichzeitig werden SDK-Pins in allen GitHub-Workflows und der Composite Action `.github/actions/build-and-package` (inklusive der beiden self-contained Publishes mit `--framework net10.0`) umgestellt, das Vertragsskript `scripts/Export-ImportContract.ps1` auf den neuen Ausgabepfad angepasst und die Betriebsdokumentation (`Docs/install.md`, `Docs/dependencies.md`) auf den .NET-11-Stand gebracht — als ein konsistenter fachlicher Stand ohne Mischzustand.

## Fachlicher Scope

- **Verarbeitungseinheit:** Eine vollständige Plattform-Migration der Solution `Rezepte.sln` — alle zehn Projekte (`Rezepte.Web`, `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost`) samt zugehöriger Toolchain (SDK-Pins, Paketstände, MSBuild-Targets, CI/CD-Workflows, Release-Publishes, Vertragsskripte, Betriebsdoku). Die Migration ist nur als Ganzes abgeschlossen; ein Teilstand mit `net10.0`-Resten gilt nicht als erfüllt.
- **Auslösende Ereignisse:** Auftragsbezogene Umsetzung auf einem Task-Branch mit PR gegen `staging` (Repo-Workflow: Task-Branch → `staging` → `main`). Ausgangsbasis ist der `staging`-Stand nach Merge von PR #230. Keine Nutzeraktion, kein Laufzeit-Trigger.
- **Erwartete Wirkung:**
  - `dotnet restore Rezepte.sln`, `dotnet build Rezepte.sln` und `dotnet test Rezepte.sln` laufen mit dem .NET-11-SDK fehlerfrei; bestehender Testbestand bleibt grün bzw. zulässige Laufzeit-Skips bleiben als `Skipped` erkennbar.
  - CI für PR und Staging läuft mit .NET 11 inklusive Testfilterung, Coverage, Playwright-/Browser-Test-Pfad und Security-Scan erfolgreich.
  - Der Release-Pfad erzeugt weiterhin self-contained Artefakte für `linux-x64` und `win-x64` auf Basis von `net11.0` inklusive Plugin-Output und Update-/Release-Metadaten.
  - Smoke-Test unter .NET 11 erfolgreich: Start, Datenbankmigration gegen bestehende SQLite-Datenbank, Import-Plugin-Erkennung/-Ausführung, zentrale Exportfunktionen.
  - Keine produktive Projektdatei, Workflow-Definition, Composite Action, Release-Pipeline oder aktuelle Betriebsdoku verweist unbegründet auf `net10.0`, `10.0.x` oder `10.0.15`; bewusste Ausnahmen sind dokumentiert.
- **Umgang mit fehlenden Objekten:**
  - `global.json` (SDK-Pinning): existiert derzeit nicht im Repository (verifiziert) — Entscheidung offen, ob sie ergänzt wird; lokal ist bereits .NET SDK `11.0.100` verfügbar.
  - Kompatible `11.0.x`-Zielversionen für `Microsoft.*`-Pakete: zum Umsetzungszeitpunkt konkret festzulegen und zu pinnen (kein Floating).
  - Drittpaket-Kompatibilität: `msTools.Updater` wird als lokales Paket über `lib/nuget/msTools.Updater.0.10.4.nupkg` eingebunden — bei fehlender `net11.0`-Kompatibilität ist der Aktualisierungsweg (neue Paketversion in `lib/nuget`) gesondert zu klären. Übrige Drittpakete prüfen und erforderliche Updates begründet pinnen.
  - EF-Core-Migration/ModelSnapshot: prüfen, ob EF Core 11 eine Migration oder Änderungen am ModelSnapshot erfordert; falls ja, Auswirkung und Migration separat begründen.
  - Externe/bestehende Plugin-Releases: Kompatibilität externer Import-Plugin-Quellen und bereits veröffentlichter Plugin-Artefakte mit .NET 11 klären (aktivieren/wiederverwenden vs. Neuübersetzung erforderlich).
  - Produktivserver-Laufzeit: prüfen, ob Server für framework-abhängige Deployments .NET 11 bereitstellen; andernfalls self-contained als Referenzweg dokumentieren.
- **Scope-Ausschlüsse:**
  1. Keine automatische Erledigung von #228 und #229 — beide enthalten eigenständige Breaking Changes (xunit.v3/`bunit` 2.x bzw. QuestPDF-Font-/Settings-Migration), die ein reiner TFM-/SDK-Wechsel nicht löst. Ob sie im selben PR gebündelt oder als Folge-PRs umgesetzt werden, ist vor Umsetzungsbeginn festzulegen; keines der beiden Issues darf allein aufgrund der `net11.0`-Umstellung geschlossen werden.
  2. Keine fachlichen Funktionsänderungen an der Anwendung — es handelt sich um eine Plattform-/Toolchain-Migration; nur für die Migration erforderliche Code-Anpassungen (z. B. durch .NET-11-Breaking-Changes) sind zulässig.
  3. Keine Änderung der Release-Topologie — self-contained Artefakte für `linux-x64` und `win-x64` inklusive Plugin-Output und Update-/Release-Metadaten bleiben bestehen, lediglich auf `net11.0`-Basis.

## Betroffene Klassen und Komponenten

### Projektdateien und Build-Grundlagen

- `Directory.Build.props` — Fallback-`<TargetFramework>` (Zeile 3: `net10.0` per Condition) auf `net11.0`; versorgt u. a. `Rezepte.Import.PluginSdk`, das keinen eigenen TFM setzt (verifiziert).
- Explizit `net10.0` gesetzt in: `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj`, `Rezepte.Import.Plugins.Backup/…csproj`, `Rezepte.Import.Plugins.AIFoto/…csproj`, `Rezepte.Import.Plugins.AIUrl/…csproj`, `Rezepte.Tests/Rezepte.Tests.csproj`, `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`, `Rezepte.Tests.PluginFixture/Rezepte.Tests.PluginFixture.csproj`, `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`.
- `Microsoft.*`-Direktreferenzen einheitlich `10.0.15` → gepinnte `11.0.x`-Linie: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.Extensions.Caching.Abstractions`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`.
- Drittpakete auf `net11.0`-Kompatibilität prüfen: `Serilog.AspNetCore` `10.0.0` (plus `Serilog.Sinks.Console`, `Serilog.Sinks.File`), `Google.Cloud.Vision.V1` `3.8.0`, `QuestPDF` `2026.7.0`, `msTools.Updater` `0.10.4` (lokal in `lib/nuget`, Bezug in drei Projekten), `Microsoft.Playwright` `1.65.1`, `Microsoft.NET.Test.Sdk` `18.12.0`, `xunit` `2.9.3`, `xunit.runner.visualstudio`, `Xunit.SkippableFact`, `bunit` `1.40.0`, `AngleSharp`, `Moq`, `FluentAssertions`, `coverlet.collector`.
- EF Core: `Migrations`/ModelSnapshot in `Rezepte.Web` auf EF-Core-11-Anforderungen prüfen.

### Plugin- und Build-Mechanik

- `Rezepte.Web/Rezepte.Web.csproj` — `ImportPluginProject`-Items (Zeilen 54–62) sowie MSBuild-Targets `BuildImportPlugins`, `CopyImportPluginsToOutput`, `CopyImportPluginsToPublish` (Zeilen 65–84): bauen/kopieren Plugins über `$(TargetFramework)` — Funktion mit `net11.0`-Ausgabepfaden verifizieren; Plugin-Laden unter .NET 11 prüfen.
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` — Target `PublishRezepteWebForBrowserTests` (Zeilen 35–36) publiziert `Rezepte.Web` über `$(TargetFramework)`.
- `Rezepte.Import.Abstractions` / `Rezepte.Import.PluginSdk` — Vertrags-Assemblies (`$(ImportContractVersion)`); Kompatibilität bestehender Plugin-Releases gegen den neuen Laufzeitkontext bewerten (siehe `contract-baselines/import-contract`).

### CI/CD und Release

- `.github/workflows/pr-staging-ci.yml` — `actions/setup-dotnet` Pins `10.0.x` (ca. Zeilen 80, 116, 227).
- `.github/workflows/staging-ci.yml` — Pins `10.0.x` (ca. Zeilen 75, 105, 210).
- `.github/workflows/release.yml` — Pin `10.0.x` (ca. Zeile 46).
- `.github/workflows/security-scan.yml` — Pin `10.0.x` (ca. Zeile 31).
- `.github/actions/build-and-package/action.yml` — `dotnet-version: '10.0.x'` (Zeile 28) sowie beide self-contained Publishes mit `--framework net10.0` (Zeilen 48, 63).
- Übrige Workflows (`pages.yml`, Review-/Promotion-/Sync-Workflows) auf implizite .NET-10-Abhängigkeiten prüfen.

### Skripte und Dokumentation

- `scripts/Export-ImportContract.ps1` — Zeile 585: Vertrags-Assemblies unter `bin/Release/net10.0` erwartet; auf `net11.0` anpassen oder versionsunabhängig machen.
- `Docs/install.md` — .NET-10-Laufzeit-/Paket-Hinweise (`aspnetcore-runtime-10.0`, `dotnet10`, Publish-Beispiele `-f net10.0` für `linux-x64`).
- `Docs/dependencies.md` — .NET-10-Paketstand (`10.0.15`-Linie) und Update-Politik.
- Weitere aktuelle Betriebsdoku (`Docs/deployment-guide.md`, `Docs/development-guide.md`, `Docs/testing.md`, `Docs/help/github-actions.md`) auf .NET-10-Bezüge prüfen.

## Implementierungsansatz

- **Baseline zuerst:** Auf dem `staging`-Stand nach PR #230 kurze .NET-10-Baseline sicherstellen (`restore`, `build`, `test`), danach TFM/SDK/Pakete gemeinsam umstellen, damit Restore- und Kompilierfehler eindeutig der Migration zugeordnet werden können.
- **TFM-Umstellung:** Alle expliziten `<TargetFramework>net10.0</TargetFramework>` auf `net11.0` sowie den Fallback in `Directory.Build.props` anheben; `Rezepte.Import.PluginSdk` erbt dann automatisch `net11.0`.
- **Paketlinie:** `Microsoft.*`-Referenzen einheitlich auf eine konkret gepinnte `11.0.x`-Version heben; Drittpakete nur bei nachgewiesenem Bedarf aktualisieren und Updates begründen. `msTools.Updater` ist ein lokales `.nupkg` — dessen Aktualisierung erfordert ggf. eine neue Paketdatei in `lib/nuget` statt eines Versions-Bumps am Feed.
- **Breaking-Changes-Review:** .NET-11-Breaking-Changes gezielt gegen die verwendeten Bereiche prüfen: ASP.NET Core/Blazor, EF Core/SQLite, Authentication (Cookie + JWT), Dependency Injection, JSON/Serialisierung, statische Assets, Publish/self-contained Deployment sowie Test-/MSBuild-Integration.
- **Mechanik-Verifikation statt Blind-Umstellung:** Plugin-Build/-Kopie (`BuildImportPlugins`, `CopyImportPlugins*`), Plugin-Laden zur Laufzeit und `PublishRezepteWebForBrowserTests` unter `net11.0` verifizieren; `Export-ImportContract.ps1`-Pfad anpassen bzw. TFM-unabhängig formulieren.
- **Ein konsistenter Stand:** Änderungen an Workflows, Release-Publishes, Skripten und Doku gehören in denselben Umsetzungs-PR wie die Projektumstellung — kein Mischzustand aus `net10.0`- und `net11.0`-Referenzen.
- **Abhängigkeit #228/#229:** Vor Umsetzungsbeginn festlegen, ob die beiden Issues gebündelt werden; Entscheidung und Nachweis (oder bewusstes Offenlassen) im PR dokumentieren.

## Konfiguration

Keine neue Laufzeit-Konfiguration der Anwendung erforderlich. Steuerung erfolgt über die Toolchain: SDK-Pinning in `actions/setup-dotnet` (`11.0.x`), optional `global.json` im Repository (Entscheidung offen), gepinnte Paketversionen in den `.csproj`-Dateien sowie der `--framework net11.0`-Parameter im Release-Publish. Deployment-Referenzweg (self-contained vs. framework-abhängig mit .NET-11-Runtime auf dem Server) ist zu klären und in `Docs/install.md` zu dokumentieren.

## Akzeptanzkriterien

1. `dotnet restore Rezepte.sln`, `dotnet build Rezepte.sln` und `dotnet test Rezepte.sln` laufen mit dem .NET-11-SDK fehlerfrei; der bestehende Testbestand bleibt grün bzw. zulässige Laufzeit-Skips bleiben als `Skipped` erkennbar.
2. Keine produktive Projektdatei, Workflow-Definition, Composite Action, Release-Pipeline oder aktuelle Betriebsdoku verweist noch unbegründet auf `net10.0`, `10.0.x` oder `10.0.15`; bewusste Ausnahmen sind dokumentiert.
3. CI für PR und Staging läuft mit .NET 11 inklusive Testfilterung, Coverage, Playwright-/Browser-Test-Pfad und Security-Scan erfolgreich.
4. Der Release-Pfad erzeugt weiterhin self-contained Artefakte für `linux-x64` und `win-x64`, jetzt auf Basis von `net11.0`, inklusive Plugin-Output und Update-/Release-Metadaten.
5. Anwendungs-Smoke-Test unter .NET 11 erfolgreich: Start, Datenbankmigration gegen eine bestehende SQLite-Datenbank, Import-Plugin-Erkennung/-Ausführung und zentrale Exportfunktionen.
6. `dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet keine neuen bekannten verwundbaren Pakete.
7. Der Umgang mit #228 und #229 ist im PR dokumentiert: entweder wurden beide vollständig mit umgesetzt und nachgewiesen, oder sie bleiben bewusst als separate Folge-Issues offen.

## Explizite Nicht-Ziele

- Keine implizite Lösung von #228 (xunit.v3/`bunit` 2.x/AngleSharp) oder #229 (QuestPDF-Font-/Settings-Migration); bei Nicht-Umsetzung bleiben beide Issues mit unveränderten Akzeptanzkriterien geöffnet.
- Keine fachlichen Änderungen an Anwendungsfunktionen, Domänenlogik oder UI.
- Keine Änderung des Release-Modells (self-contained `linux-x64`/`win-x64` bleibt Referenz, sofern nicht anders dokumentiert entschieden).
- Kein Floating bei SDK- oder Paketversionen — Ziel sind reproduzierbar gepinnte Stände.

## Offene Fragen

1. Konkrete Zielversionen für SDK und `Microsoft.*`-Pakete zum Umsetzungszeitpunkt festlegen (z. B. `11.0.x` statt Floating).
2. Soll eine `global.json` zur expliziten SDK-Pinning-Strategie ergänzt werden?
3. Werden #228 und #229 im selben Umsetzungs-PR gebündelt oder als eigenständige Folge-PRs nach dem Upgrade umgesetzt?
4. Erfordert EF Core 11 eine Migration bzw. Änderungen am ModelSnapshot; falls ja, wie sind Auswirkung und Migration zu begründen?
5. Sind externe/optionale Import-Plugin-Quellen und bestehende Plugin-Releases mit .NET 11 kompatibel, oder müssen Plugins neu gegen `net11.0` gebaut werden?
6. Stellen Produktivserver für framework-abhängige Deployments bereits .NET 11 bereit; andernfalls self-contained als Referenzweg dokumentieren.
7. Ist `msTools.Updater` `0.10.4` (lokales Paket in `lib/nuget`) `net11.0`-kompatibel; falls nicht, wie wird ein aktualisiertes Paket bereitgestellt?
