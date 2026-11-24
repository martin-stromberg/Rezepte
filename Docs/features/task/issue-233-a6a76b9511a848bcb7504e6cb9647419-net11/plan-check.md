# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. `dotnet restore`/`build`/`test Rezepte.sln` mit .NET-11-SDK fehlerfrei, Testbestand grün bzw. `Skipped` erkennbar | Ablauf 1 (TFM/Pakete, `global.json`), Umsetzungsreihenfolge Schritte 2–5 | Ablauf 9.1–9.3 (`dotnet test Rezepte.sln`, Lane-Filter, Skipped-Stichprobe), Szenario 1 | Abgedeckt |
| 2. Keine unbegründeten `net10.0`-/`10.0.x`-/`10.0.15`-Referenzen; Ausnahmen dokumentiert | Abläufe 4, 5, 8 (Skripte, Workflows, Doku, README, `changes.log`) | Reihenfolge-Schritt 15: repo-weiter Grep-Audit + dokumentierte Ausnahmen (EF-`ProductVersion`, `msTools.Updater`-`net10.0`-Asset, `lib/nuget`-Dateiname) | Abgedeckt |
| 3. CI für PR und Staging mit .NET 11 inkl. Testfilterung, Coverage, Playwright-Pfad, Security-Scan | Ablauf 5 (8× `dotnet-version`, Composite Action), begründeter VSTest-Verbleib | Ablauf 9.3 (Lane-/TRX-/Coverage-Probe lokal mit CI-Argumenten), Szenario 6 (CI-Jobs inkl. Coverage-Gate 70 %, browser-tests, security-scan) | Abgedeckt |
| 4. Release-Pfad erzeugt self-contained `linux-x64`/`win-x64` auf `net11.0` inkl. Plugin-Output und Update-/Release-Metadaten | Ablauf 5.2 (`build-and-package`: Pin + 2× `--framework net11.0`), Scope-Ausschluss 2 (Topologie unverändert) | Ablauf 9.6 Publish-Probe `win-x64` mit Output-Check (`plugins/`, Font-Assets); `linux-x64` via CI, Szenario 7 | Abgedeckt |
| 5. Smoke-Test unter .NET 11: Start, DB-Migration Bestands-SQLite, Plugin-Erkennung/-Ausführung, Exportfunktionen | Ablauf 9.7, Reihenfolge-Schritt 14 | Szenario 9 (manueller Nachweis + Logs), Szenario 10 (externes `net10.0`-Plugin via Roll-Forward) | Abgedeckt |
| 6. `dotnet list Rezepte.sln package --vulnerable --include-transitive` ohne neue Befunde | Ablauf 9.4, Reihenfolge-Schritt 13 | Vulnerability-Scan + `--deprecated`-Scan (kein `xunit`-Befund) | Abgedeckt |
| 7. Umgang mit #228/#229 im PR dokumentiert (umgesetzt+nachgewiesen oder bewusst offen) | Verbindliche Bündelung: eigene Abläufe 6 (#228) und 7 (#229) mit vollständigen Teil-Akzeptanzkriterien | Neue Tests (`PdfGeneratorTests` inkl. Emoji + CJK-Randfall, `ExportService`-`includePdf`-ZIP-Test, `Configure_IsIdempotent`), Lane-/Trait-/Deprecated-Nachweise, `Closes #228`/`#229`/`#233` im PR (Reihenfolge-Schritt 15) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Bestehende UI-Flüsse (Start, Auth, Plugin-Settings, LoadingBar, RecipeSearch, ShareLink, SecurityTxt) unter `net11.0`/`xunit.v3`/`bunit` 2.x | Die 35 bestehenden Playwright-Tests in `Rezepte.Tests.Browser` laufen inhaltlich unverändert gegen den `PublishRezepteWebForBrowserTests`-Output der migrierten Plattform (E2E-Abschnitt plan.md); Anpassung ausschließlich `[SkippableFact]` → `[Fact]`/`Assert.SkipUnless` | Abgedeckt |
| Datenexport mit PDF (`includePdf`) — #229-Akzeptanzkriterium „PDF-Einträge im Export-ZIP" | Kein dedizierter Playwright-Test; Nachweis über Integrationstest `ExportUser_WithPdfIncluded_ContainsPdfEntriesInZip` (echter `PdfGenerator`) + `PdfGeneratorTests` (Emoji, CJK-Randfall, `%PDF`-Header) + manueller Smoke-Test | Nicht erforderlich mit Begründung — Plan begründet explizit: Fluss unverändert, Font-/Glyph-Nachweis ist binärer ZIP-Inhalt auf Service-Ebene |
| Dialoge/Overlays/Panels mit Geometrie-Anforderung | — | Nicht erforderlich mit Begründung — keine neuen oder geänderten Dialoge/Overlays in dieser Plattform-Migration; vorhandene Dialog-Tests laufen unverändert weiter |
| Fehlsituation „kein 11er-SDK installiert" (sichtbare Blockade) | Szenario 5: klare SDK-Fehlermeldung via `global.json`, manuelle Beobachtung | Abgedeckt (kein UI-Flow; Toolchain-Verhalten, nicht über UI auslösbar) |
| Externe Plugin-Artefakte (`net10.0`-DLLs) im Laufzeitkontext | Szenario 10: `PluginManager`-Discovery → `Loaded` statt `Incompatible`/`LoadFailed` (Smoke-Test/Plugin-Status) | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- Die in `plan-check.1.md` beanstandete Lücke ist geschlossen: `Google.Cloud.Vision.V1` `3.8.0` hat nun eine begründete Verbleib-Entscheidung (Designentscheidungen, Ablauf 1.7, Reihenfolge-Schritt 4, Offene Punkte) — höchste veröffentlichte Version, `netstandard2.0`-Asset NuGet-konform für `net11.0`, Fehlerfall-Analyse (Restore-/Build-Fehler statt stiller Laufzeitfehler). Stichprobe verifiziert: Bezug in `Rezepte.Web.csproj` Zeile 13 und `Rezepte.Import.Plugins.AIFoto.csproj` Zeile 12.
- Der Hinweis aus `plan-check.1.md` zum fehlenden benannten Randfall-Test ist erledigt: `GenerateRecipePdf_WithCjkGlyphs_ReturnsNonEmptyPdf` deckt Akzeptanzszenario 4 konkret ab.
- Die requirement-seitige „Breaking-Changes-Review"-Empfehlung (Implementierungsansatz: gezielte Prüfung von ASP.NET Core/Blazor, EF Core/SQLite, Auth, DI, JSON, statische Assets, Publish) hat keinen eigenen expliziten Review-Schritt; die Abdeckung erfolgt faktisch über Build-Fehlerattribuierung (Schritt 5), die vollständige Testsuite (809 Tests), Lane-/Publish-Proben und den Smoke-Test. Bei der Umsetzung ist sinnvoll, die Breaking-Changes-Liste einmalig gegen die genannten Bereiche abzugleichen — kein Planbruch.
- Stichproben-Verifikation der Plan-Referenzen gegen den Repo-Bestand (alle korrekt): `DeploymentDocumentationTests.cs` Zeilen 46/68/96/99, `ContractExportScriptTests.cs` Zeilen 269/271, 8× `dotnet-version: '10.0.x'` an den benannten Zeilen, `build-and-package`-Zeilen 4/25/28/48/63, `Program.cs` (`ConfigureSerilog` Zeile 18, `builder.Build()` Zeile 63), `PdfGenerator.cs` (`Settings.License` Zeile 33, `DefaultTextStyle` Zeile 103), `DemoData\demo-images.zip`-Content-Muster (Zeile 36), README-.NET-10-Stellen (Zeilen 5/33/43/76), `os-interface-tests.md` SkippableFact-Bezug (Zeilen 201–202), SkippableFact-Nutzung in exakt 11 Browser-Dateien (34 Attribute + 38 Aufrufe).
- `ContractExportScriptTests` legt bewusst einen `net11.0`-Fake-Pfad an, obwohl das Skript nach Umstellung TFM-agnostisch auflöst — erforderlich wegen Akzeptanzkriterium 2 (keine unbegründeten `net10.0`-Referenzen), konsistent.
- `dotnet ef --version "11.0.*"` (Ablauf 3.1) ist ein Floating-Pin für ein ad-hoc-Tool, kein Repo-Artefakt — vereinbar mit dem Nicht-Ziel „kein Floating bei SDK-/Paketversionen", da `global.json` und `PackageReference` gepinnt sind.
