# Umsetzungsplan: Migration .NET 10 → .NET 11 (Issue #233, gebündelt mit #228 und #229)

## Übersicht

Die gesamte `Rezepte.sln` (zehn Projekte) wird konsistent von `net10.0`/.NET 10 auf `net11.0`/.NET 11 gehoben: Target-Frameworks in neun `.csproj` plus dem Fallback in `Directory.Build.props`, alle zwölf `Microsoft.*`-Direktreferenzen auf die gepinnte `11.0.0`-Linie, SDK-Pins in vier Workflows und der `build-and-package`-Composite-Action (inkl. zwei `--framework`-Publishes), zwei Skript-Pfade (`Export-ImportContract.ps1`, `generate-screenshots.js`), drei Teststellen sowie Betriebsdoku, `README.md` und `changes.log`. **Verbindlich im selben PR gebündelt** werden Issue #228 (Test-Stack-Migration `xunit` 2.9.3 → `xunit.v3` 4.0.3, `bunit` 1.40.0 → 2.15.2, `AngleSharp` 1.7.3 → 1.10.5, Entfernung von `Xunit.SkippableFact`) und Issue #229 (`QuestPDF` 2026.7.0 → 2026.9.2 mit expliziter Font-/Settings-Migration). Außer den für diese drei Teilumfänge erforderlichen Anpassungen gibt es keine fachlichen Code-Änderungen.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Vollständige Plattform-Migration der `Rezepte.sln` inkl. Toolchain (SDK-Pins, Paketstände, MSBuild-Targets, CI/CD, Release-Publish, Vertrags-/Tooling-Skripte, Betriebsdoku, README, `changes.log`) **plus** die zwei gekoppelten Library-Migrationen: Test-Stack (#228: `xunit.v3`/`bunit` 2.x/`AngleSharp`/`Xunit.SkippableFact`-Entfernung in `Rezepte.Tests` + `Rezepte.Tests.Browser`) und QuestPDF-Font-Migration (#229: `Rezepte.Web` + neue PDF-Tests in `Rezepte.Tests`). Kein Teilstand mit `net10.0`-Resten; beide Folge-Issues werden mit ihren vollständigen Akzeptanzkriterien umgesetzt und schließen mit diesem PR. |
| Auslösende Ereignisse | Auftragsbezogene Umsetzung auf `task/issue-233-…-net11`, PR gegen `staging`. Ausgangsbasis: `staging`-Head nach Merge von PR #230/#232 (`aa9263e`, verifiziert — damit ist die #228-Voraussetzung „Post-#230-Stand" erfüllt). Kein Laufzeit-Trigger, keine Nutzeraktion. |
| Erwartete Wirkung | `dotnet restore`/`build`/`test Rezepte.sln` mit .NET-11-SDK fehlerfrei, 809/809 Tests grün (bzw. dokumentierte Skips); CI (PR + Staging), Release-Publish (self-contained `linux-x64`/`win-x64`) und Security-Scan auf `net11.0`; Smoke-Test (Start, DB-Migration auf Bestands-SQLite, Plugin-Erkennung/-Ausführung, Exporte inkl. PDF) erfolgreich; keine unbegründeten `net10.0`-/`10.0.x`-/`10.0.15`-Referenzen; kein deprecated `xunit`-Paket mehr; PDF-Export mit Sonderzeichen erzeugt nachweisbar PDF-Einträge im Export-ZIP. |
| Umgang mit fehlenden Objekten | `global.json` → **erzeugen** (siehe Designentscheidungen). `11.0.x`-Zielversionen → **festgelegt**: `Microsoft.*` = `11.0.0`, SDK-Pin = `11.0.x`/`11.0.100` (nuget.org-Index + installierte SDKs verifiziert). `msTools.Updater` → **wiederverwenden** (0.10.4, `net10.0`-Asset für `net11.0` auflösbar — NuGet-konform, Build-/Smoke-Verifikation). EF-ModelSnapshot → **wiederverwenden** (`dotnet ef`-Verifikationsschritt). Externe `net10.0`-Plugin-Artefakte → **wiederverwenden** (Runtime-Roll-Forward). Produktivserver-Laufzeit → self-contained bleibt Referenzweg; framework-abhängiger Pfad in `install.md` beschreibt den offiziellen .NET-11-Installationsweg allgemein, **ohne** unverifizierte Distro-Feed-Behauptungen (verbindliche Entscheidung). Test-Stack-Pakete → `xunit`/`Xunit.SkippableFact` **verwerfen**, `xunit.v3`/`bunit`/`AngleSharp` **neu einführen**; übrige Test-Pakete **wiederverwenden**. PDF-Fonts → Basis-Schrift (gebündeltes Lato) **wiederverwenden**, Emoji-Fallback-Schrift (Noto Emoji, OFL) **erzeugen**/deployen, Font-/Settings-Konfiguration (`QuestPdfConfiguration`) **erzeugen**. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine fachlichen Funktionsänderungen (Domänenlogik, UI, neue Features) | Reine Plattform-/Toolchain-/Library-Migration; zulässig sind nur die dafür erforderlichen Code-Anpassungen (.NET-11-Breaking-Changes, xunit.v3-/bunit-2.x-API-Migration in Testcode, QuestPDF-Font-/Settings-Konfiguration inkl. `DefaultTextStyle`-Font-Chain). | bestätigt |
| 2 | Keine Änderung der Release-Topologie | Self-contained `linux-x64`/`win-x64` inkl. Plugin-Output, `release-metadata.json` und `update.json` bleiben bestehen — nur auf `net11.0`-Basis. | bestätigt |
| 3 | Kein Wechsel des Test-Ausführungsmodells auf Microsoft.Testing.Platform (MTP) | #228-Akzeptanzkriterium verlangt unveränderte CI-Aufrufe (`--filter "Category…"`, TRX-Logger, `XPlat Code Coverage`); der VSTest-Pfad bleibt (`xunit.runner.visualstudio` 4.0.1 unterstützt v3, verifiziert via Paketbeschreibung). `dotnet test` nutzt unter .NET-11-SDK ohne `test.runner`-Eintrag in `global.json` weiterhin VSTest. MTP als eigenes Folge-Thema möglich. | bestätigt |
| 4 | Export-Fehlerpfad bleibt Status quo (`PdfGenerator` → `null` + `LogError`; `ExportService` → `LogWarning` + Überspringen) | #229 lässt die Verschärfung des Fehlerpfads ausdrücklich als Option/Folge-Issue. Mit der geplanten Font-Konfiguration sind Font-/Glyph-Fehler keine erwartbaren Produktivfälle mehr; verbleibende Fehler sind Defekte, die über die vorhandene Log-Kette sichtbar sind. Dokumentierte Entscheidung, kein unbeobachtetes Verhalten. | bestätigt |
| 5 | Glyph-Abdeckung beschränkt auf Latin/Greek/Cyrillic (gebündeltes Lato) + Emoji (Noto Emoji); keine CJK-/Arabic-/Hebrew-Fonts | Rezeptinhalte sind deutsch/europäisch; Emoji wurden im Issue explizit als Beispiel genannt. Abdeckung weiterer Schriftsysteme ist eine spätere reine Asset-Ergänzung (weitere Noto-Datei + eine Familienangabe), keine Architekturentscheidung. Fehlende Glyphen außerhalb der Kette rendern als Tofu statt als fehlendes PDF (siehe `ThrowOnMissingTextGlyphs`-Entscheidung). | bestätigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| SDK-Pinning | `global.json` **anlegen**: `version: "11.0.100"`, `rollForward: "latestFeature"`, `allowPrerelease: false`; **kein** `test.runner`-Eintrag | Reproduzierbarer Build-Stand (Nicht-Ziel „kein Floating"). `latestFeature` fixiert Major.Minor `11.0` und toleriert neuere `11.0.x`-SDKs, die CI via `setup-dotnet` installiert. Bewusst kein `"test": { "runner": "Microsoft.Testing.Platform" }`: erst ab .NET-10-SDK schaltet dieser Eintrag `dotnet test` auf MTP um; ohne ihn bleibt der VSTest-Pfad mit `--filter`-Trait-Syntax, TRX-Logger und `XPlat Code Coverage` unverändert funktionsfähig (verifiziert via Microsoft-Learn-MTP-Doku). |
| `Microsoft.*`-Paketlinie | Einheitlich `11.0.0` (höchste stabile 11er-Version auf nuget.org, verifiziert u. a. für `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.AspNetCore.Mvc.Testing`) | Entspricht der bestehenden Konvention „einheitlicher Stand innerhalb der LTS-Linie" (aktuell `10.0.15`, `Docs/dependencies.md`) und dem installierten Runtime-Stand `11.0.0`. |
| `Serilog.AspNetCore` | Update `10.0.0` → `11.0.0` | Versioniert major-parallel zur .NET-Linie; `11.0.0` auf nuget.org verifiziert. `Serilog.Sinks.Console` `6.1.1` / `Serilog.Sinks.File` `7.0.0` sind jeweils höchste stabile Stände — unverändert. |
| `msTools.Updater` 0.10.4 | Behalten — kein neues Paket in `lib/nuget` | `.nupkg` enthält nur `lib/net10.0/` (verifiziert); NuGet löst `net10.0` als nächstniedrigeres kompatibles `netcoreapp`-Asset für `net11.0` auf. Verifikation über Restore/Build + Smoke-Test. Eskalationspfad: `net11.0`-Asset als neues `.nupkg` vom Paket-Eigentümer. |
| `Google.Cloud.Vision.V1` 3.8.0 | Behalten — kein Update möglich und keines nötig | `3.8.0` ist die höchste veröffentlichte Version auf nuget.org (Paketindex verifiziert — kein neuerer Stand existiert). Das Paket enthält nur `net462`- und `netstandard2.0`-Assets (lokaler NuGet-Cache verifiziert); NuGet löst `netstandard2.0` NuGet-konform für `net11.0` auf — analog zur `msTools.Updater`-Auflösung, aber ohne `netcoreapp`-Näherungsfrage. Bezüge in `Rezepte.Web` (Zeile 13) und `Rezepte.Import.Plugins.AIFoto` (Zeile 12); die transitive `Google.Api.Gax.Grpc`/`Grpc.*`-Kette ist ebenfalls `netstandard2.0`-kompatibel. Verifikation über Restore/Build (Ablauf 9.1); Fehlerfall würde sich als Restore-/Build-Fehler zeigen, nicht als stiller Laufzeitfehler. |
| Test-Stack (#228): Test-Runner | `xunit` 2.9.3 → **`xunit.v3` `4.0.3`** (höchste stabile, verifiziert); `xunit.runner.visualstudio` `4.0.1` und `Microsoft.NET.Test.Sdk` `18.12.0` **behalten**; `Xunit.SkippableFact` **entfernen** | `xunit.runner.visualstudio` 4.0.1 läuft v1/v2/v3 (Paketbeschreibung verifiziert) und hält den VSTest-Pfad (`dotnet test`, Test Explorer, CI-`--filter`, TRX, Coverage) intakt — Issue-Empfehlung übernommen, MTP nicht eingeführt (siehe Ausschluss 3). `Xunit.SkippableFact` ist v2-only; v3 bringt `Assert.Skip`/`SkipUnless`/`SkipWhen` eingebaut. |
| Test-Stack (#228): `bunit`/`AngleSharp` | `bunit` `1.40.0` → **`2.15.2`**; `AngleSharp` `1.7.3` → **`1.10.5`** (direkter Pin beibehalten) | `bunit` 2.15.2 ist die höchste stabile 2.x und hat expliziten `net11.0`-GA-Support (Release-Notes verifiziert); sie deklariert `AngleSharp >= 1.10.1` + `AngleSharp.Css >= 1.4.1` + `AngleSharp.Diffing >= 1.1.1` (nuspec verifiziert). `AngleSharp` wird auf `1.10.5` gepinnt — dieselbe Minor-Linie wie bunits Build-Basis (kein Sprung auf `1.11.0`, um Binärkompatibilität wie beim dokumentierten `TypeLoadException`-Vorfall sicherzustellen); direkte `AngleSharp.Dom`-Nutzung (`IElement` in `PluginSettingsComponentTests`) bleibt kompatibel. |
| Test-Stack (#228): Skip-Migration | `[SkippableFact]` → `[Fact]`; `Skip.IfNot(cond, msg)` → `Assert.SkipUnless(cond, msg)` (34 Attribute, 38 Aufrufe in 11 Dateien unter `Rezepte.Tests.Browser`) | Eins-zu-eins-API-Ersatz, identische Semantik (Runtime-Skip bei fehlendem Chromium/App). Aufrufe liegen ausschließlich in Testmethoden bzw. der während der Testausführung laufenden Hilfsmethode `LoadingBarBrowserSession.StartLoggedInSessionAsync` → `SkipException` wird vom Runner korrekt als `Skipped` gewertet. |
| Test-Stack (#228): `bunit`-API-Migration | `TestContext` → `BunitContext`, `RenderComponent<T>` → `Render<T>`, `AddTestAuthorization()` → `AddAuthorization()`, `TestContextBase` → `BunitContext` (nur in `PathBaseNavigationManager`) | Offizielle 1→2-Migrationsanleitung verifiziert: `RenderComponent` ist `[Obsolete(error:true)]` (Kompilierfehler, muss ersetzt werden); `TestContext` ist noch als `[Obsolete]`-Subklasse vorhanden, erzeugt aber Warnungen und kollidiert begrifflich mit `Xunit.TestContext`. `BunitContext` besitzt weiterhin `Services`, `JSInterop` (Typ `BunitJSInterop`), `Renderer` (Typ `BunitRenderer` mit `Dispatcher`) — `PathBaseNavigationManager` bleibt funktionsfähig. `IRenderedComponent<T>`, `WaitForAssertion`, `Find`/`FindAll`, `Click()`, `Change(v)` bleiben API-stabil. |
| QuestPDF (#229): Versionspin | `QuestPDF` `2026.7.0` → **`2026.9.2`** | Höchste stabile `2026.9.x` auf nuget.org (verifiziert); enthält die Breaking Changes des 2026.9.0-Release. Kein Floating auf `2026.x` (Breaking Changes möglich), kein Verbleib auf `2026.8.x` (hinter dem Issue-Ziel). |
| QuestPDF (#229): Font-Strategie | **Option B (variantenreich begründet):** `UseSystemFonts = false` (neuer Default, explizit gesetzt); Basisschrift = **gebündeltes Lato** (`QuestPDF.Fonts.Lato.br`, seit 2026.9 im Paket enthalten, deckt Latin/Greek/Cyrillic); Emoji-Fallback = deployte **Noto Emoji** (monochrome Variante, OFL-Lizenz) unter `Rezepte.Web/Assets/Fonts/`; `DefaultTextStyle` setzt `FontFamily("Lato", "Noto Emoji")` | Option A (`UseSystemFonts=true`) würde das bisherige Zufallsverhalten fortsetzen: PDFs hängen vom Font-Bestand des Deployment-Hosts ab — für self-contained Release-Artefakte auf schlanken Linux-Hosts nicht verlässlich und im CI verifizierbar nicht reproduzierbar (QuestPDF selbst warnt: „minimal environment such as an Alpine-based container may have none at all"). Option B mit gebündeltem Lato + explizitem Fallback ist deterministisch, deployment-unabhängig und deckt das Issue-Szenario (Emoji in Titeln/Schritten) ab. Monochrome Noto Emoji statt Color-Emoji, weil die SkiaSharp-Pipeline für Color-Fonts nicht verifiziert ist; monochrome Glyphen reichen für Lesbarkeit. |
| QuestPDF (#229): `ThrowOnMissing*`-Entscheidung | `ThrowOnMissingFontFamilies = true` (Default, explizit); `ThrowOnMissingTextGlyphs = false` | Fehlende Font-Familie = Konfigurations-/Programmierfehler → Fail-fast ist richtig und wird in Dev/CI sofort sichtbar (Test `PdfGeneratorTests` schlägt fehl). Fehlende Glyphen entstehen dagegen durch **Nutzerdaten**: Ein einziges exotisches Zeichen würde sonst das gesamte Rezept-PDF verwerfen — und `ExportService` überspringt `null`-PDFs mit nur `LogWarning` → genau der im Issue beschriebene stille Ausfall. Mit `false` rendern nicht abgedeckte Glyphen als Tofu-Zeichen über die Fallback-Kette; das PDF existiert → Akzeptanzkriterium „kein unbemerkt fehlendes PDF" erfüllt. |
| QuestPDF (#229): Settings-Ort | Neue `public static class QuestPdfConfiguration` (`Rezepte.Web/Configuration/`) mit idempotentem `Configure()`; Aufruf früh in `Program.cs` (vor `builder.Build()`); `Settings.License` wird aus dem `PdfGenerator`-Konstruktor dorthin verlagert | Issue-Vorgabe „zentral beim App-Start, nicht im Service-Konstruktor". Zentral statisch, weil `QuestPDF.Settings`/`FontManager` selbst globale Statik sind; idempotent aufrufbar, damit Tests (`PdfGeneratorTests`, künftige PDF-Tests) dieselbe Konfiguration ohne App-Host etablieren. `public`, da kein `InternalsVisibleTo` im Repo existiert (verifiziert). |
| `Export-ImportContract.ps1` Zeile 585 | TFM-unabhängig: DLL per `Get-ChildItem "$projectName/bin/Release/*/$projectName.dll"` auflösen | Versionsunabhängigkeit verhindert künftige TFM-Nacharbeit — gleiche Robustheit wie `RezepteAppFixture` (leitet TFM aus eigenem Output ab, verifiziert Zeilen 466–471). |
| `generate-screenshots.js` Zeile 10 | `net10.0`-Pfadsegment → Verzeichnisauflösung (`bin/Release/*/publish/Rezepte.Web.dll`) | Konsistent mit dem Skript-Ansatz; Skript kann `$(TargetFramework)` nicht auswerten. |
| EF Core 11 / ModelSnapshot | Keine neue Migration; `ProductVersion`-Annotationen (`10.0.9`/`10.0.11`) unverändert | Historisch generierte Tooling-Artefakte, keine produktiven Versionsreferenzen. EF Core 11 lädt bestehende Snapshots; Verifikation via `dotnet ef migrations has-pending-model-changes`. |
| Deployment-Doku (`install.md`) | Self-contained bleibt dokumentierter Referenzweg; framework-abhängiger Pfad beschreibt die offizielle .NET-11-Installation allgemein (Microsoft-Lernpfad/`dotnet-install`-Skript, `apt`-Paketnamen nur als „falls im Distro-Feed verfügbar" ohne konkrete Feed-Behauptungen) | Verbindliche Scope-Entscheidung; verhindert unverifizierte Behauptungen über STS-Verfügbarkeit in Ubuntu-/Debian-Feeds. |

## Programmabläufe

### 1. TFM- und Paket-Umstellung (Kernschritt)

1. `global.json` im Repo-Root anlegen (`11.0.100`, `latestFeature`, `allowPrerelease: false`, kein `test.runner`).
2. `Directory.Build.props` Zeile 3: Fallback `net10.0` → `net11.0` (versorgt `Rezepte.Import.PluginSdk`).
3. Neun `.csproj` `<TargetFramework>net10.0</TargetFramework>` → `net11.0`: `Rezepte.Web`, `Rezepte.Import.Abstractions`, `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost`.
4. Alle zwölf `Microsoft.*`-`PackageReference` `10.0.15` → `11.0.0` (Liste siehe `inventory/projektdateien.md` Zusammenfassung).
5. `Serilog.AspNetCore` `10.0.0` → `11.0.0` (`Rezepte.Web.csproj`).
6. `QuestPDF` `2026.7.0` → `2026.9.2` (`Rezepte.Web.csproj`).
7. `Google.Cloud.Vision.V1` `3.8.0` **unverändert** (`Rezepte.Web.csproj`, `Rezepte.Import.Plugins.AIFoto.csproj`) — höchste veröffentlichte Version; `netstandard2.0`-Asset löst NuGet-konform für `net11.0` auf (Begründung siehe Designentscheidungen).
8. Test-Stack-Pakete (`Rezepte.Tests.csproj`, `Rezepte.Tests.Browser.csproj`): `xunit` 2.9.3 → `xunit.v3` 4.0.3; `bunit` 1.40.0 → 2.15.2; `AngleSharp` 1.7.3 → 1.10.5; `Xunit.SkippableFact` entfernen; `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.12.0, `coverlet.collector` 10.1.1, `Microsoft.Playwright` 1.65.1, `Moq` 4.21.0, `FluentAssertions` 8.11.0 unverändert (alle auf nuget.org als jeweils höchste stabile Stände verifiziert).
9. `dotnet restore Rezepte.sln` + `dotnet build Rezepte.sln -c Release` — erwartete Kompilierfehler aus bunit/xunit.v3 werden in Ablauf 6 abgearbeitet.

Beteiligte Dateien: `global.json` (neu), `Directory.Build.props`, neun `.csproj`.

### 2. Plugin-Mechanik-Verifikation

1. Solution-Build: Plugins werden als Solution-Projekte gebaut (`BuildingSolutionFile=true`); Output landet in `bin/Release/net11.0/plugins/<PluginName>` via `CopyImportPluginsToOutput`.
2. Einzelprojekt-Build `dotnet build Rezepte.Web/Rezepte.Web.csproj`: `BuildImportPlugins` propagiert `TargetFramework=net11.0`; `RezepteWebReferencePath` zeigt auf `net11.0`-Output.
3. `dotnet publish Rezepte.Web` → `CopyImportPluginsToPublish` legt `bin/Release/net11.0/publish/plugins/` an.
4. Laufzeit: `PluginManager`/`PluginLoadContext` laden Plugins unverändert; externe `net10.0`-Artefakte bleiben ladbar (Runtime-Roll-Forward).

Beteiligte Komponenten: `Rezepte.Web.csproj`-Targets, `PluginManager`, `PluginLoadContext`, `PublishRezepteWebForBrowserTests` (TFM-agnostisch, keine Änderung).

### 3. EF-Core-11-Verifikation

1. `dotnet tool install -g dotnet-ef --version "11.0.*"` (ad hoc, kein Repo-Artefakt).
2. `dotnet ef migrations has-pending-model-changes --project Rezepte.Web`: kein Delta → keine Migration (erwartet); Delta → separat begründete Entscheidung.
3. Bestehende SQLite-Migrationstests + Smoke-Test gegen Bestands-DB decken das Anwenden der 15+ Migrationen ab.

Beteiligte Komponenten: `Rezepte.Web/Migrations/*`, `RezepteDbContextModelSnapshot`, `WebApplicationExtensions.ApplyDatabaseMigrationsAsync`.

### 4. Skripte und Tests anpassen

1. `scripts/Export-ImportContract.ps1` Zeile 585 → TFM-unabhängige DLL-Auflösung.
2. `scripts/screenshots/generate-screenshots.js` Zeile 10 → Verzeichnisauflösung.
3. `DeploymentDocumentationTests.cs`: Zeile 46 `.NET-10-Shared-Frameworks` → `.NET-11-…`; Zeile 68 `-f net10.0` → `-f net11.0`; Zeilen 96/99 `StartsWith("10.")` → `"11."`.
4. `ContractExportScriptTests.cs` Zeilen 269/271: Fake-Pfad `bin/Release/net10.0` → `net11.0`.

### 5. CI/CD und Release-Pipeline umstellen

1. `dotnet-version: '10.0.x'` → `'11.0.x'` an 8 Stellen: `pr-staging-ci.yml` (80, 116, 227), `staging-ci.yml` (75, 105, 210), `release.yml` (46), `security-scan.yml` (31).
2. `.github/actions/build-and-package/action.yml`: „.NET 10" → „.NET 11" (Zeilen 4, 25), `dotnet-version` Zeile 28, `--framework net10.0` → `net11.0` (Zeilen 48, 63).
3. Keine Workflow-Änderungen für den Test-Stack nötig: `dotnet test` bleibt VSTest (kein `test.runner` in `global.json`), Filter-/TRX-/Coverage-Argumente unverändert.

### 6. Test-Stack-Migration (#228)

1. **Paket-Umstellung** (in Ablauf 1.8 enthalten): danach `dotnet restore`; prüfen, dass kein Paket transitiv noch `xunit`/`xunit.core`/`xunit.extensibility.*` 2.x zieht (`dotnet list package --include-transitive` bzw. Restore-Ausgabe; sonst `CS0433`-Typkonflikte).
2. **`Xunit.SkippableFact`-Migration** (11 Dateien in `Rezepte.Tests.Browser`): `[SkippableFact]` → `[Fact]` (34×), `Skip.IfNot(cond, msg)` → `Assert.SkipUnless(cond, msg)` (38×). Dateien: 10 Testklassen (`LoadingBar*`, `PluginSettings*`, `RecipeSearch*`, `SecurityTxt*`, `ShareLink*`, `Auth/RegisterTests`) + Hilfsmethode `Infrastructure/LoadingBarBrowserSession.cs` (2 Aufrufe).
3. **`bunit`-API-Migration** (7 Dateien in `Rezepte.Tests/Components`): `TestContext` → `BunitContext` (Feld `_context`), `RenderComponent<T>` → `Render<T>` (identische Parameter-Builder-Überladung), `AddTestAuthorization()` → `AddAuthorization()` (2 Stellen in `RecipeSearchComponentTests`), `TestContextBase` → `BunitContext` (Parameter `PathBaseNavigationManager`, `Renderer`-Zugriff bleibt). `IDisposable.Dispose()` der Klassen bleibt; `_context.Dispose()` unverändert (`BunitContext` implementiert `IDisposable` + `IAsyncDisposable`).
4. **xunit.v3-Nebenmigration**: `using Xunit;` bleibt (111 Dateien, verifiziert — kein `using Xunit.Abstractions`/`Xunit.Sdk` im Bestand). `[Trait]`, `[Collection]`, `[CollectionDefinition]`, `IClassFixture`, `ICollectionFixture`, `IAsyncLifetime`, `[Theory]`/`[InlineData]`/`[MemberData]`/`TheoryData<>` sind v3-kompatibel (139× `InlineData`, 9× `MemberData`, 5× `TheoryData`, 2× `IClassFixture`, 2× `IAsyncLifetime`, 5× `Assert.Throws` gezählt). Neue `xunit.v3`-Analyzer-Warnungen im Build auswerten und beheben (`WarningsAsErrors`-Gate CS1591 bleibt bestehen; CI-Gate `TreatWarningsAsErrors`).
5. **Verifikation**: `dotnet test` beider Projekte; Lane-Filter `--filter "Category!=OsInterface"`/`"Category=OsInterface"` lokal ausführen und Trait-Auflösung prüfen; TRX-Logger und `--collect:"XPlat Code Coverage"` lokal verifizieren (CI-Argumente identisch); `dotnet list package --deprecated` → keine `xunit`-Meldung; `Skipped`-Status der Browser-Tests ohne Browser-Umgebung ggf. an einer Stichprobe prüfen.

Beteiligte Komponenten: `Rezepte.Tests.csproj`, `Rezepte.Tests.Browser.csproj`, alle 7 `Components/*Tests.cs`, alle 10 Browser-Testklassen + `LoadingBarBrowserSession`, Test-Infrastruktur (`BrowserTestCollection`, `RezepteAppFixture` u. a. — unverändert, v3-kompatibel).

### 7. QuestPDF-Font-/Settings-Migration (#229)

1. **Font-Asset deployen**: `NotoEmoji-Regular.ttf` (monochrome Noto-Emoji-Variante, OFL) + `OFL.txt` unter `Rezepte.Web/Assets/Fonts/` ins Repo; `Content`-Eintrag in `Rezepte.Web.csproj` mit `CopyToOutputDirectory=PreserveNewest` + `CopyToPublishDirectory=PreserveNewest` (Muster: `DemoData\demo-images.zip`-Eintrag).
2. **`QuestPdfConfiguration` anlegen** (`Rezepte.Web/Configuration/`, `public static`, XML-Docs wegen `WarningsAsErrors=CS1591`): idempotentes `Configure()` setzt `QuestPDF.Settings.License = LicenseType.Community`, `UseSystemFonts = false`, `ThrowOnMissingFontFamilies = true`, `ThrowOnMissingTextGlyphs = false` und ruft `FontManager.RegisterFontsFromDirectory(Path.Combine(AppContext.BaseDirectory, "Fonts"))` auf (nur wenn Verzeichnis existiert → testbar ohne App-Output).
3. **`Program.cs`**: `QuestPdfConfiguration.Configure()` vor `builder.Build()` aufrufen (Top-Level-Statements, z. B. direkt nach `ConfigureSerilog`).
4. **`PdfGenerator`**: `Settings.License`-Zeile aus dem Konstruktor entfernen; in `RecipePdfDocument.Compose` `page.DefaultTextStyle(x => x.FontSize(12))` → `x.FontSize(12).FontFamily("Lato", "Noto Emoji")` (Font-Fallback-Kette; „Lato" = gebündelte Schrift, „Noto Emoji" = deployte Datei — Familiennamen via `FontManager.GetRegisteredFonts()` verifizieren).
5. **Neue Tests** (`Rezepte.Tests`, siehe Tests-Abschnitt): `PdfGeneratorTests` mit Emoji-Rezept (`%PDF`-Header, nicht-leer) + `ExportService`-Test mit `includePdf: true` und echtem `PdfGenerator` (ZIP enthält `pdf/`-Einträge). Tests rufen `QuestPdfConfiguration.Configure()` selbst auf.
6. **Verifikation**: `dotnet test`; Publish-Probe prüft, dass `QuestPDF.Fonts.Lato.br` und `Fonts/NotoEmoji-Regular.ttf` im Output liegen; Smoke-Test exportiert Rezept mit Emoji → PDF öffnet, Emoji sichtbar bzw. vorhanden.

Beteiligte Komponenten: `Rezepte.Web.csproj`, `QuestPdfConfiguration` (neu), `Program.cs`, `PdfGenerator`/`RecipePdfDocument`, `ExportService` (unverändert — Status-quo-Fehlerpfad), `Rezepte.Web/Assets/Fonts/` (neu).

### 8. Dokumentation aktualisieren

1. `README.md`: Badge `.NET-10.0` → `11.0`, Tech-Stack `.NET 10` → `.NET 11`, Voraussetzung „SDK 10" → 11, `.NET-10-Shared-Frameworks` → `.NET-11-Shared-Frameworks` (Zeile 76 — von `DeploymentDocumentationTests` assertiert).
2. `Docs/install.md`: `net10.0` → `net11.0`, `.NET 10` → `.NET 11`, `-f net10.0`-Beispiele → `-f net11.0`; framework-abhängiger Abschnitt formuliert den **offiziellen .NET-11-Installationsweg allgemein** (Verweis auf Microsoft-Installationsdoku/`dotnet-install`-Skript; apt-Paketnamen wie `aspnetcore-runtime-11.0` nur mit „falls im Distro-Feed verfügbar"-Vorbehalt, **keine** Feed-Verfügbarkeitsbehauptungen) und stellt self-contained als Referenzweg heraus. Assertierte Strings (`Rezepte.Web.dll`, `dotnet --info`, `Microsoft.NETCore.App`, `Microsoft.AspNetCore.App`, `--self-contained false/true`, `chmod +x`) müssen erhalten bleiben bzw. angepasste Testassertionen decken.
3. `Docs/dependencies.md`: Versionsstände (`Microsoft.*` `11.0.0`, `Serilog.AspNetCore` `11.0.0`, `QuestPDF` `2026.9.2`, `xunit.v3` `4.0.3`, `bunit` `2.15.2`, `AngleSharp` `1.10.5`, `Microsoft.NET.Test.Sdk` `18.12.0`, `coverlet.collector` `10.1.1`); Zeile 31 (Aufschub-Hinweis #228/#229) durch Ergebnis ersetzen (Test-Stack migriert; QuestPDF-Font-Strategie kurz dokumentieren: gebündeltes Lato + Noto Emoji, `ThrowOnMissingTextGlyphs=false`); `msTools.Updater`-`net10.0`-Asset-Ausnahme dokumentieren.
4. `Docs/help/github-actions.md`: `10.0.x`/`net10.0`/„.NET-10-Testrunner" → 11er-Bezüge; veraltete Release-Fluss-Beschreibung an Ist-Zustand angleichen (self-contained, `release-{linux,win}-x64.zip`, `update.json`).
5. `Docs/help/os-interface-tests.md` (~Zeilen 28, 201–202): Quarantäne-Hinweis von `Skip.IfNot`/`Xunit.SkippableFact`-Nachrüstung auf eingebautes `Assert.Skip*` unter `xunit.v3` umformulieren.
6. `Docs/screenshot-generation.md` Zeile 7: „.NET SDK 10" → 11. `Docs/Anforderungskatalog.md` Zeile 1: „.NET 10" → „.NET 11".
7. `changes.log`: neuen Eintrag im bestehenden Format (Datum + Titel „Migration .NET 11 inkl. Test-Stack- und QuestPDF-Migration (#233/#228/#229)", Stichpunkte je Bereich).
8. `Docs/deployment-guide.md`, `Docs/development-guide.md`, `Docs/testing.md`: stichprobenartig gegenprüfen (Grep-verifiziert ohne .NET-10-Bezug).

### 9. Nachweis-Läufe (lokal + CI)

1. `dotnet restore`, `dotnet build Rezepte.sln -c Release`, `dotnet test Rezepte.sln` → 809/809 grün (bzw. begründete Skips).
2. `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`; `dotnet build -p:TreatWarningsAsErrors=true` (CI-Gate-Spiegel — fängt `xunit.v3`-Analyzer-Warnungen).
3. `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` + `--filter "Category=OsInterface"` (Lane-Verifikation unter v3/VSTest); `--collect:"XPlat Code Coverage"` und `--logger trx` stichprobenartig.
4. `dotnet list Rezepte.sln package --vulnerable --include-transitive` (keine neuen Befunde) + `dotnet list Rezepte.sln package --deprecated` (kein `xunit`-Befund).
5. `pwsh scripts/Export-ImportContract.ps1` → ZIP mit `net11.0`-DLLs, ApiCompat gegen `contract-baselines/import-contract/0.3.0` grün (`ImportContractVersion` bleibt `0.3.0`).
6. Publish-Probe: `dotnet publish Rezepte.Web/Rezepte.Web.csproj -c Release -f net11.0 -r win-x64 --self-contained true`; Output-Prüfung auf `QuestPDF.Fonts.Lato.br`, `Fonts/NotoEmoji-Regular.ttf`, `plugins/`; linux-x64 via CI.
7. Smoke-Test unter .NET 11: Start, `MigrateAsync` gegen Bestands-SQLite, Plugin-Erkennung/-Ausführung, Exporte (PDF mit Emoji-Rezept, JSON), Updater-Pfad.
8. Aufräumen: `bin/`/`obj/`-Ordner mit `net10.0`-Output löschen.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Rezepte.Web/Configuration/QuestPdfConfiguration.cs` | `public static class` | Zentrale, idempotente QuestPDF-Konfiguration (`License`, `UseSystemFonts`, `ThrowOnMissing*`, `FontManager.RegisterFontsFromDirectory`); aufgerufen aus `Program.cs` und PDF-Tests |
| `Rezepte.Tests/Services/PdfGeneratorTests.cs` | Testklasse | PDF-Erzeugung mit Sonderzeichen/Emoji: `%PDF`-Header, nicht-leere Bytes, Fallback-Rendering |

Neue Nicht-Klassen-Artefakte: `global.json`, `Rezepte.Web/Assets/Fonts/NotoEmoji-Regular.ttf` + `OFL.txt`.

## Änderungen an bestehenden Klassen

| Datei | Änderung |
|-------|----------|
| `Rezepte.Web/Program.cs` | `QuestPdfConfiguration.Configure()` vor `builder.Build()` aufrufen |
| `Rezepte.Web/Services/PdfGenerator.cs` | `Settings.License`-Zuweisung aus dem Konstruktor entfernen; `RecipePdfDocument.Compose`: `DefaultTextStyle` um `FontFamily("Lato", "Noto Emoji")` erweitern |
| `Rezepte.Web/Rezepte.Web.csproj` | `Content`-Eintrag für `Assets/Fonts/*` (Copy-to-Output+Publish); Paketbumps (siehe Ablauf 1) |
| `Rezepte.Tests/Components/*.cs` (7 Dateien) | `TestContext` → `BunitContext`; `RenderComponent<T>` → `Render<T>`; `AddTestAuthorization()` → `AddAuthorization()`; `TestContextBase` → `BunitContext` in `PathBaseNavigationManager` (`RecipeSearchComponentTests.cs` Zeilen 324–343) |
| `Rezepte.Tests.Browser/*.cs` + `Auth/RegisterTests.cs` + `Infrastructure/LoadingBarBrowserSession.cs` (11 Dateien) | `[SkippableFact]` → `[Fact]` (34×); `Skip.IfNot` → `Assert.SkipUnless` (38×) |
| `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` | Zeile 46: `.NET-10-Shared-Frameworks` → `.NET-11-Shared-Frameworks`; Zeile 68: `-f net10.0` → `-f net11.0`; Zeilen 96, 99: `Version.StartsWith("10.")` → `"11."` |
| `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` | Zeilen 269, 271: Fake-Pfad `bin/Release/net10.0` → `bin/Release/net11.0` |
| `Rezepte.Tests/Rezepte.Tests.csproj`, `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` | Paketänderungen siehe Ablauf 1.8; TFM `net11.0` |

`ExportService` bleibt **unverändert** (bewusste Entscheidung: Status-quo-Fehlerpfad, Ausschluss 4).

## Datenbankmigrationen

Keine. Datenmodell unverändert; `ProductVersion`-Annotationen bleiben (Tooling-Historie). Verifikation via `dotnet ef migrations has-pending-model-changes` (Ablauf 3).

## Validierungsregeln

Keine — keine neuen oder geänderten Eingaben, keine externen Schnittstellen mit Feldsemantik.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `global.json` → `sdk.version` | Toolchain-Pin | `11.0.100` | SDK-Untergrenze; reproduzierbarer Build-Stand |
| `global.json` → `sdk.rollForward` | Toolchain-Pin | `latestFeature` | Neuere `11.0.x`-SDKs akzeptieren, Major-Wechsel blockieren |
| `global.json` → `sdk.allowPrerelease` | Toolchain-Pin | `false` | Keine Preview-SDKs |
| `global.json` → `test.runner` | — | **nicht gesetzt** | Hält `dotnet test` bewusst im VSTest-Modus (MTP-Opt-in würde CI-Filter-Syntax brechen) |
| `actions/setup-dotnet` `dotnet-version` | CI-Pin | `11.0.x` (8 Stellen + `build-and-package`) | Aktuelles 11er-SDK in CI |
| `QuestPDF.Settings.*` via `QuestPdfConfiguration` | Laufzeit-Tooling | `License=Community`, `UseSystemFonts=false`, `ThrowOnMissingFontFamilies=true`, `ThrowOnMissingTextGlyphs=false` | Deterministische Font-Auflösung; Fail-fast bei Konfigurationsfehlern, robustes Rendering bei Nutzerdaten-Glyphen |
| `Rezepte.Web/Assets/Fonts/NotoEmoji-Regular.ttf` | Content-Asset | `CopyToOutput/PublishDirectory=PreserveNewest` | Emoji-Fallback-Schrift (OFL), deployment-unabhängig |

Keine Änderungen an `appsettings`.

## Seiteneffekte und Risiken

- **`msTools.Updater` (net10.0-Asset):** NuGet-konforme Auflösung für `net11.0`, muss im Build verifiziert werden; Laufzeitverhalten im Smoke-Test. Eskalation: `net11.0`-Asset vom Paket-Eigentümer.
- **Externe `net10.0`-Plugin-Artefakte:** laufen per Runtime-Roll-Forward auf .NET 11; Smoke-Test prüft Erkennung/Ausführung.
- **`xunit.v3`-Analyzer:** neue statische Regeln im `xunit.v3`-Paket können Warnungen erzeugen → unter `TreatWarningsAsErrors` Build-brüchig; im Build-Schritt einzeln beheben/begründen.
- **VSTest-vs.-MTP-Modus:** `dotnet test` bleibt VSTest nur solange `global.json` keinen `test.runner`-Eintrag trägt — explizit dokumentiert, damit spätere `global.json`-Änderungen die CI-Filter nicht unabsichtlich brechen.
- **`bunit`-2.x-Verhaltensänderungen** (Render-Engine, Event-Dispatcher): trotz API-kompatibler Migration können einzelne Komponententests Timing-/Render-Unterschiede zeigen → Fehlschläge werden einzeln analysiert, nicht pauschal angepasst.
- **`AngleSharp`-Pin `1.10.5`:** bewusst nicht `1.11.0` (bunits Build-Basis ist `1.10.1`); direkter Pin bleibt wegen `AngleSharp.Dom`-Nutzung erforderlich.
- **`xunit.v3` erzeugt ausführbare Testprojekte:** `dotnet test`/`dotnet build`-Auflösung und `PublishRezepteWebForBrowserTests` bleiben funktionsfähig; etwaige `dotnet test`-MTP-Preferenz wird über die Lane-Verifikation früh erkannt.
- **QuestPDF-PDF-Aussehen ändert sich:** von host-abhängiger Systemschrift auf gebündeltes Lato — gewollte Vereinheitlichung; Smoke-Test öffnet PDF visuell.
- **Font-Asset-Beschaffung:** `NotoEmoji-Regular.ttf` wird extern (Google Fonts, OFL) bezogen — Hash/Quelle im PR dokumentieren; `OFL.txt` liegt bei.
- **`DeploymentDocumentationTests`-Kopplung:** Tests assertieren Doku-Strings und führen echten `dotnet publish` aus — Doku- und Teständerung atomar committen.
- **`global.json`-Nebenwirkung:** Entwickler:innen ohne `11.0.x`-SDK erhalten klare SDK-Fehlermeldung — gewünscht, im PR vermerken.
- **CI-Global-Tools:** `dotnet-reportgenerator-globaltool` 5.5.11, `Microsoft.DotNet.ApiCompat.Tool` laufen unter 11er-SDK; bei Inkompatibilität Version anheben und begründen.
- **`Serilog.AspNetCore` 11.0.0:** Major-Update — Startverhalten/Logging im Smoke-Test verifizieren.

## Umsetzungsreihenfolge

1. **Baseline gegenprüfen**
   - Voraussetzungen: keine (Baseline aus Inventory: 809/809 grün auf `aa9263e`, SDK 11.0.100 installiert)
   - Beschreibung: `staging`-Stand verifiziert; ungetrackte Inventory-Artefakte stören Build nicht.

2. **`global.json` anlegen**
   - Voraussetzungen: SDK `11.0.100` lokal installiert (verifiziert)
   - Beschreibung: `sdk.version 11.0.100`, `rollForward latestFeature`, `allowPrerelease false`; bewusst kein `test.runner`.

3. **TFM-Umstellung (`Directory.Build.props` + 9 `.csproj`)**
   - Voraussetzungen: Schritt 2
   - Beschreibung: `net10.0` → `net11.0` überall; `Rezepte.Import.PluginSdk` erbt automatisch.

4. **Paketlinie anheben (Laufzeit + Test-Stack + QuestPDF gemeinsam)**
   - Voraussetzungen: Schritt 3; Verfügbarkeit aller Zielversionen auf nuget.org (verifiziert: `Microsoft.*` `11.0.0`, `Serilog.AspNetCore` `11.0.0`, `QuestPDF` `2026.9.2`, `xunit.v3` `4.0.3`, `bunit` `2.15.2`, `AngleSharp` `1.10.5`)
   - Beschreibung: zwölf `Microsoft.*`-Referenzen → `11.0.0`; `Serilog.AspNetCore` → `11.0.0`; `QuestPDF` → `2026.9.2`; `xunit` → `xunit.v3` `4.0.3`; `bunit` → `2.15.2`; `AngleSharp` → `1.10.5`; `Xunit.SkippableFact` entfernen; `Google.Cloud.Vision.V1` verbleibt `3.8.0` (verifiziert höchste Version, `netstandard2.0`-Asset).

5. **Restore + Build + Fehlerattribuierung**
   - Voraussetzungen: Schritte 3–4
   - Beschreibung: `dotnet restore`/`build`; `msTools.Updater`-Auflösung bestätigen; transitive `xunit`-2.x-Reste ausschließen; .NET-11-Breaking-Changes minimal beheben. Erwartete bunit-/xunit-Kompilierfehler steuern Schritt 6.

6. **Test-Stack-Codemigration (#228)**
   - Voraussetzungen: Schritt 5
   - Beschreibung: `SkippableFact`→`Fact`/`Skip.IfNot`→`Assert.SkipUnless` (11 Dateien); `TestContext`→`BunitContext`, `RenderComponent`→`Render`, `AddTestAuthorization`→`AddAuthorization`, `TestContextBase`→`BunitContext` (7 Dateien); `xunit.v3`-Analyzer-Warnungen beheben; Build + `dotnet test` beider Testprojekte grün.

7. **QuestPDF-Font-Migration (#229)**
   - Voraussetzungen: Schritt 5
   - Beschreibung: Font-Asset (`NotoEmoji-Regular.ttf` + `OFL.txt`) + `Content`-Eintrag; `QuestPdfConfiguration` neu; `Program.cs`-Aufruf; `PdfGenerator`-Konstruktor bereinigen; `DefaultTextStyle`-Font-Chain; neue Tests `PdfGeneratorTests` + `ExportService`-`includePdf`-Test.

8. **Skripte anpassen**
   - Voraussetzungen: Schritt 5
   - Beschreibung: `Export-ImportContract.ps1` TFM-unabhängig; `generate-screenshots.js` Pfadauflösung.

9. **Tests anpassen (`net10.0`-Referenzen)**
   - Voraussetzungen: Schritt 5
   - Beschreibung: `DeploymentDocumentationTests` (3 Stellen), `ContractExportScriptTests` (2 Stellen).

10. **CI/CD + Composite Action umstellen**
    - Voraussetzungen: Schritte 6–7 (lokaler Build grün)
    - Beschreibung: 8× `dotnet-version`, `build-and-package` (Pin + 2× `--framework` + Texte).

11. **Dokumentation + README + `changes.log` aktualisieren**
    - Voraussetzungen: Schritte 6–9 (Testassertionen hängen an README/install.md; Paketstände final)
    - Beschreibung: siehe Ablauf 8 — inkl. `dependencies.md`-Neufassung (neue Paketstände, Font-Strategie, `msTools.Updater`-Ausnahme) und `os-interface-tests.md` (`Assert.Skip*`-Konvention).

12. **EF-Core-Verifikation**
    - Voraussetzungen: Schritt 5; `dotnet-ef` 11.0.x ad hoc installiert
    - Beschreibung: `dotnet ef migrations has-pending-model-changes`; Ergebnis dokumentieren.

13. **Vollständige Nachweis-Läufe**
    - Voraussetzungen: Schritte 6–11
    - Beschreibung: `dotnet test Rezepte.sln` (809/809), Lane-Filter-Probe, Coverage/TRX-Probe, `dotnet format`, `TreatWarningsAsErrors`, Vulnerability- + Deprecated-Scan, `Export-ImportContract.ps1`, Publish-Probe `win-x64` (Output-Check: Lato-Archiv, Noto-Emoji, `plugins/`).

14. **Smoke-Test unter .NET 11**
    - Voraussetzungen: Schritt 13
    - Beschreibung: Start, `MigrateAsync` gegen Bestands-SQLite, Plugin-Erkennung/-Ausführung, Exporte (JSON + PDF mit Emoji-Rezept), Updater-Pfad.

15. **Grep-Audit + PR-Dokumentation**
    - Voraussetzungen: Schritte 11–14
    - Beschreibung: Repo-weiter Grep auf `net10.0`/`10.0.x`/`10.0.15`/`.NET 10`/`xunit 2.`/`SkippableFact` außerhalb historischer Docs und `bin`/`obj`; bewusste Ausnahmen (EF-`ProductVersion`, `msTools.Updater`-`lib/net10.0`-Asset, `lib/nuget`-Dateiname) dokumentieren; `Closes #228`-/`Closes #229`-/`Closes #233`-Bezug im PR vermerken (Bündelung ist verbindlich).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GenerateRecipePdf_WithEmojiTitle_ReturnsNonEmptyPdf` | `Rezepte.Tests/Services/PdfGeneratorTests.cs` | `QuestPdfConfiguration.Configure()` + `PdfGenerator.GenerateRecipePdfAsync` mit Emoji im Titel/Schritttext → nicht-`null` Bytes, `%PDF`-Header (Fallback-Rendering funktioniert, kein stiller `null`-Pfad) |
| `GenerateRecipePdf_WithCjkGlyphs_ReturnsNonEmptyPdf` | `Rezepte.Tests/Services/PdfGeneratorTests.cs` | Rezept mit Glyphen außerhalb der Lato-/Noto-Emoji-Kette (CJK-Zeichen im Titel, z. B. „中文") → nicht-`null`/nicht-leere Bytes, `%PDF`-Header — `ThrowOnMissingTextGlyphs=false` rendert Tofu-Glyphen statt stillem `null`-Ausfall (Randfall-Nachweis für Szenario 4) |
| `ExportUser_WithPdfIncluded_ContainsPdfEntriesInZip` | `Rezepte.Tests/Services/ExportServiceExportTests.cs` (Erweiterung) | `ExportService` mit echtem `PdfGenerator` (Sqlite-`:memory:`-Muster wie Bestandstests), `includePdf: true` → ZIP enthält `pdf/`-Einträge (Akzeptanzkriterium #229: erwartete PDF-Einträge im Datenexport) |
| `Configure_IsIdempotent` | `PdfGeneratorTests` | `QuestPdfConfiguration.Configure()` mehrfach aufrufbar ohne Nebenwirkung (Test-Pattern-Absicherung) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| 7 `Rezepte.Tests/Components/*Tests.cs` | `bunit`-2.x-API-Migration (`BunitContext`, `Render<T>`, `AddAuthorization`, `TestContextBase`→`BunitContext`) |
| 10 `Rezepte.Tests.Browser/*Tests.cs` + `Auth/RegisterTests.cs` + `Infrastructure/LoadingBarBrowserSession.cs` | `Xunit.SkippableFact`-Entfernung: `[SkippableFact]`→`[Fact]`, `Skip.IfNot`→`Assert.SkipUnless` |
| `DeploymentDocumentationTests` (Zeilen 46, 68, 96, 99) | `.NET-11-`-Strings, `-f net11.0`, Versionspräfix `11.` |
| `ContractExportScriptTests` (Zeilen 269, 271) | Fake-Pfad `bin/Release/net11.0` |
| `FlakyConventionTests` (per Compile-Link in beiden Testprojekten) | Konventionstest bleibt unverändert, muss unter xunit.v3 weiterlaufen — Verifikationsobjekt |

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung führt **keine neuen Benutzerflüsse** ein (keine neue/geänderte UI-Interaktion, kein Dialog). Als E2E-Nachweis dienen die **35 bestehenden Playwright-Browser-Tests** (`Rezepte.Tests.Browser`), die auf der `net11.0`-Plattform unter `xunit.v3` gegen den `PublishRezepteWebForBrowserTests`-Output Start, Auth, Plugin-Settings und zentrale UI-Pfade real ausüben — unverändert in Auslöser und Ergebnis, aber unter der migrierten Test-Infrastruktur. Der PDF-Export-Pfad wird durch die neuen Integrationstests (`ExportService` + `includePdf`, echtes `PdfGenerator`) und den Smoke-Test abgedeckt — ein reiner UI-Playwright-Test würde den Font-/Glyph-Nachweis nicht zusätzlich absichern (ZIP-Inhaltsprüfung ist Services-Ebene). `RezepteAppFixture` leitet TFM/Pfad aus eigenem Output ab (TFM-agnostisch, verifiziert) — keine Anpassung nötig.

Bestehende E2E-Tests müssen angepasst werden, **ausschließlich** im Rahmen der Test-Framework-Migration (`[SkippableFact]` → `[Fact]` + `Assert.SkipUnless`) — keine inhaltlichen Änderungen.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Solution auf `net11.0` | `dotnet build`/`test` (Dev/CI) | Repo umgestellt, `global.json` vorhanden, SDK `11.0.x` | `dotnet restore` → `build` → `test Rezepte.sln` | Build fehlerfrei, 809/809 Tests grün (oder dokumentierte Skips) | Build-/Test-Logs, CI-Lauf |
| 2 | Test-Stack unter `xunit.v3`/VSTest (#228) | `dotnet test` mit Lane-Filtern | Testprojekte auf `xunit.v3` 4.0.3 + `bunit` 2.15.2 | `--filter "Category!=OsInterface"` (blockierend) + `"Category=OsInterface"` (best-effort), TRX + `XPlat Code Coverage` | Beide Lanes laufen, Trait-Filter greift, keine Deprecated-Warnung, `Skipped` wird bei fehlendem Chromium korrekt gemeldet | Test-Logs, `dotnet list package --deprecated`, CI-Lauf |
| 3 | PDF-Export mit Sonderzeichen (#229) | Nutzer startet Datenexport mit `includePdf` (oder Testaufruf) | App gestartet (`QuestPdfConfiguration` konfiguriert), Rezept mit Emoji im Titel | `ExportService.ExportUserAsync(includePdf: true)` → `PdfGenerator.GenerateRecipePdfAsync` → ZIP | ZIP enthält `pdf/<autor> - <titel>.pdf`; PDF nicht leer, Emoji gerendert (Fallback-Kette), kein still fehlendes PDF | `PdfGeneratorTests` + `ExportService`-ZIP-Test + Smoke-Test |
| 4 | Fehlende Glyph-Abdeckung (außerhalb Lato/Noto Emoji) | Rezept enthält exotische Glyphen (z. B. CJK) | wie #3 | PDF-Generierung | PDF entsteht trotzdem (Tofu-Glyphen statt Ausfall); `ThrowOnMissingFontFamilies` würde nur bei falscher Font-Konfiguration (Dev-Fehler) werfen | `PdfGeneratorTests.GenerateRecipePdf_WithCjkGlyphs_ReturnsNonEmptyPdf` + dokumentierte Entscheidung |
| 5 | Dev-Maschine ohne 11er-SDK | `dotnet build` | `global.json` vorhanden, nur SDK 10.x | Build starten | Klare SDK-Fehlermeldung (kein stiller 10er-Fallback) | Manuelle Beobachtung |
| 6 | CI PR + Staging | PR gegen `staging`/Merge | Workflows auf `11.0.x` | `static-checks`, `build-and-test`, `browser-tests`, `security-scan`, `version`, `prerelease` | Alle Jobs grün inkl. Coverage-Gate 70 %, Playwright-Pfad, ApiCompat-Export | GitHub-Actions-Läufe |
| 7 | Release-Pfad | `staging-ci` prerelease/`release.yml` | `build-and-package` auf `net11.0` | Self-contained Publishes `linux-x64`+`win-x64` | Artefakte mit `net11.0`-Publish + `plugins/` + `QuestPDF.Fonts.Lato.br` + `Fonts/NotoEmoji-Regular.ttf` + Update-Metadaten | CI-Artefakte, Publish-Probe |
| 8 | Vertragsskript | `pwsh scripts/Export-ImportContract.ps1` | `net11.0`-Build | Staged Build → TFM-unabhängige DLL-Auflösung → ApiCompat 0.3.0 | `rezepte-import-contract-0.3.0.zip`, kein Vertragsbruch | Skript-Exit-Code |
| 9 | Smoke-Test unter .NET 11 | Manueller App-Start | Bestehende SQLite-DB aus `net10.0`-Betrieb, `plugins/` | Start → `MigrateAsync` → Plugin-Discovery → Import/Export (PDF) → Updater-Check | App läuft, DB migriert, Plugins `Loaded`, PDF-Export sichtbar, `msTools.Updater` lädt | Manueller Nachweis + Logs |
| 10 | Externes `net10.0`-Plugin | App-Start mit externem Artefakt (falls vorhanden) | `net10.0`-Plugin-DLL im Plugin-Root | `PluginManager`-Discovery | Plugin `Loaded` statt `Incompatible`/`LoadFailed` | Smoke-Test/Plugin-Status |
| 11 | EF-Modell-Delta | `dotnet ef migrations has-pending-model-changes` | EF Core 11, Snapshot `ProductVersion 10.0.11` | Check ausführen | Erwartet „kein Delta" → keine Migration | Kommando-Output, PR-Notiz |

## Offene Punkte

Keine.

Alle Punkte aus `requirement.md`, #228 und #229 sind durch Faktenprüfung bzw. die verbindlichen Scope-Antworten geklärt und im Plan festgelegt:

- **Bündelung #228/#229:** verbindlich im selben Umsetzungs-PR (verbindliche Antwort 1) → als eigene Programmabläufe 6/7 eingearbeitet; beide Issues schließen mit dem PR (`Closes`-Keywords).
- **Deployment-Doku:** self-contained = Referenzweg; framework-abhängige Doku ohne unverifizierte Distro-Feed-Behauptungen, offizieller Installationsweg allgemein (verbindliche Antwort 2).
- **Zielversionen:** `Microsoft.*` `11.0.0`, SDK `11.0.100`/`11.0.x`, `Serilog.AspNetCore` `11.0.0`, `QuestPDF` `2026.9.2`, `xunit.v3` `4.0.3`, `bunit` `2.15.2`, `AngleSharp` `1.10.5`, übrige Test-Pakete unverändert, `Google.Cloud.Vision.V1` `3.8.0` verbleibt (höchste veröffentlichte Version; `netstandard2.0`-Asset für `net11.0`, NuGet-Cache verifiziert).
- **`global.json`:** wird angelegt (inkl. dokumentierter Entscheidung gegen `test.runner`/MTP).
- **#228-Ausführungsmodell:** VSTest beibehalten (Issue-Empfehlung + CI-Unverändertheit), Trait-Filter-Verifikation eingeplant.
- **#229-Font-Entscheidung:** Option B — `UseSystemFonts=false`, gebündeltes Lato + deployte Noto Emoji, `ThrowOnMissingFontFamilies=true`, `ThrowOnMissingTextGlyphs=false` (Begründung: self-contained-Determinismus + Nutzerdaten-Robustheit gegen stillen PDF-Ausfall; Nachweis über `PdfGeneratorTests`, `ExportService`-ZIP-Test, Smoke-Test).
- **#229-Fehlerpfad:** Status quo bewusst beibehalten (Log-Kette) — dokumentiert als Ausschluss 4.
- **EF Core 11:** kein Migrationszwang erwartet — Verifikationsschritt 12.
- **Externe Plugins / `msTools.Updater`:** `net10.0`-Artefakte wiederverwendbar (Runtime-Roll-Forward bzw. NuGet-Auflösung) — Build-/Smoke-Verifikation.
