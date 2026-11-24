# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. `dotnet restore`/`build`/`test Rezepte.sln` mit .NET-11-SDK fehlerfrei, Testbestand grün bzw. `Skipped` erkennbar | Ablauf 1 (TFM/Pakete), `global.json` (Ablauf 1.1), Reihenfolge-Schritte 2–5 | Ablauf 9.1–9.3 (`dotnet test Rezepte.sln`, Lane-Filter, Skipped-Stichprobe), Szenario 1 | Abgedeckt |
| 2. Keine unbegründeten `net10.0`-/`10.0.x`-/`10.0.15`-Referenzen; Ausnahmen dokumentiert | Abläufe 4, 5, 8 (Skripte, Workflows, Doku, README, `changes.log`) | Reihenfolge-Schritt 15: repo-weiter Grep-Audit + dokumentierte Ausnahmen (EF-`ProductVersion`, `msTools.Updater`-`net10.0`-Asset, `lib/nuget`-Dateiname) | Abgedeckt |
| 3. CI für PR und Staging mit .NET 11 inkl. Testfilterung, Coverage, Playwright-Pfad, Security-Scan | Ablauf 5 (8× `dotnet-version`, Composite Action), Entscheidung VSTest-Verbleib | Ablauf 9.3 (Lane-/TRX-/Coverage-Probe lokal mit CI-Argumenten), Szenario 6 (CI-Jobs inkl. Coverage-Gate 70 %, browser-tests, security-scan) | Abgedeckt |
| 4. Release-Pfad erzeugt self-contained `linux-x64`/`win-x64` auf `net11.0` inkl. Plugin-Output und Update-/Release-Metadaten | Ablauf 5.2 (`build-and-package`: Pin + 2× `--framework net11.0`), Scope-Ausschluss 2 (Topologie unverändert) | Ablauf 9.6 Publish-Probe `win-x64` mit Output-Check (`plugins/`, Font-Assets); `linux-x64` via CI, Szenario 7 | Abgedeckt |
| 5. Smoke-Test unter .NET 11: Start, DB-Migration Bestands-SQLite, Plugin-Erkennung/-Ausführung, Exportfunktionen | Ablauf 9.7, Reihenfolge-Schritt 14 | Szenario 9 (manueller Nachweis + Logs), Szenario 10 (externes `net10.0`-Plugin) | Abgedeckt |
| 6. `dotnet list Rezepte.sln package --vulnerable --include-transitive` ohne neue Befunde | Ablauf 9.4 | Vulnerability-Scan in Reihenfolge-Schritt 13; zusätzlich `--deprecated`-Scan für `xunit` | Abgedeckt |
| 7. Umgang mit #228/#229 im PR dokumentiert (umgesetzt+nachgewiesen oder bewusst offen) | Verbindliche Bündelung: eigene Abläufe 6 (#228) und 7 (#229) mit vollständigen Teil-Akzeptanzkriterien | Neue Tests (`PdfGeneratorTests`, `ExportService`-`includePdf`-ZIP-Test, `Configure_IsIdempotent`), Lane-/Trait-/Deprecated-Nachweise, `Closes #228`/`#229`/`#233` im PR (Reihenfolge-Schritt 15) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine fehlende Testanforderung festgestellt.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Bestehende UI-Flüsse (Start, Auth, Plugin-Settings, LoadingBar, RecipeSearch, ShareLink, SecurityTxt) unter `net11.0`/`xunit.v3` | Die 35 bestehenden Playwright-Tests in `Rezepte.Tests.Browser` laufen unverändert inhaltlich gegen den `PublishRezepteWebForBrowserTests`-Output der migrierten Plattform (E2E-Abschnitt plan.md Zeile 281); Anpassung ausschließlich `[SkippableFact]` → `[Fact]`/`Assert.SkipUnless` | Abgedeckt |
| Datenexport mit PDF (`includePdf`) — #229-Akzeptanzkriterium „PDF-Einträge im Export-ZIP" | Kein dedizierter Playwright-Test; Nachweis über Integrationstest `ExportUser_WithPdfIncluded_ContainsPdfEntriesInZip` (echter `PdfGenerator`) + `PdfGeneratorTests` (Emoji, `%PDF`-Header) + manueller Smoke-Test mit Emoji-Rezept | Nicht erforderlich mit Begründung — Plan begründet explizit: Fluss unverändert, Font-/Glyph-Nachweis ist binärer ZIP-Inhalt auf Service-Ebene; ein UI-Test würde keine zusätzliche Absicherung liefern |
| Dialoge/Overlays/Panels mit Geometrie-Anforderung | — | Nicht erforderlich mit Begründung — keine neuen oder geänderten Dialoge/Overlays; vorhandene Dialog-Tests laufen unverändert weiter |
| Fehlsituation „SDK ohne 11.0.x" (sichtbare Blockade) | Szenario 5: klare SDK-Fehlermeldung via `global.json`, manuelle Beobachtung | Abgedeckt (kein UI-Flow; Toolchain-Verhalten) |

## Fehlende oder unvollständige Planbestandteile

- [ ] Drittpaket `Google.Cloud.Vision.V1` `3.8.0` ist in der Anforderung explizit als auf `net11.0`-Kompatibilität zu prüfendes Paket genannt (requirement.md „Betroffene Klassen und Komponenten") und in zwei Projekten referenziert (`Rezepte.Web/Rezepte.Web.csproj` Zeile 13, `Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj` Zeile 12). Der Plan enthält zu diesem Paket **keinerlei** Festlegung — weder ein begründetes Update noch eine begründete Verbleib-Entscheidung, wie sie für alle anderen Drittpakete dokumentiert ist. Designentscheidungen/Ablauf 1 um eine Zeile ergänzen (z. B. „Verbleib auf `3.8.0`, kompatibel via `netstandard2.0`-Asset — verifiziert" oder begründetes Update).

## Hinweise

- Akzeptanzszenario 4 (Glyphen außerhalb der Font-Kette, z. B. CJK → Tofu statt PDF-Ausfall) nennt als Nachweis „`PdfGeneratorTests` (Randfall)", die Tabelle „Neue Tests" enthält aber keinen benannten Testfall für nicht abgedeckte Glyphen. Bei der Testdefinition einen konkreten Randfall-Test (z. B. Rezept mit CJK-Zeichen → PDF-Bytes nicht leer/`%PDF`-Header) in `PdfGeneratorTests` einplanen — kein Planbruch, aber Konkretisierungsbedarf.
- Stichproben-Verifikation der Plan-Zahlen gegen den Repo-Bestand: 7 der 8 `Rezepte.Tests/Components/*Tests.cs` enthalten bunit-API-Bezüge (`MainLayoutSearchTests.cs` hat keinen — korrekt nicht migriert); 34× `[SkippableFact]` und 38× `Skip.If*`-Aufrufe in 11 `Rezepte.Tests.Browser`-Dateien stimmen exakt mit dem Plan überein.
- `FlakyConventionTests.cs` enthält keinen `SkippableFact`-/`Skip.*`-Bezug — die Entfernung des Pakets erzeugt hier keinen versteckten Bruch; der Plan führt die Klasse zutreffend nur als Verifikationsobjekt.
- Die begründete Entscheidung gegen `test.runner`/MTP in `global.json` sowie der dokumentierte VSTest-Verbleib schützen die CI-`--filter`-Trait-Syntax — Risiko korrekt benannt.
