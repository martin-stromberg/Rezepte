# Bestandsaufnahme: Flaky-Test-Härtung (Issue #212)

Analysiert wurden die CI-Workflows `pr-staging-ci.yml`/`staging-ci.yml` (sowie der
angrenzende `release.yml`), die Testinfrastruktur von `Rezepte.Tests.Browser` und alle
Tests beider Testprojekte auf Umgebungssensitivität — bezogen auf die Anforderung
„Lane-Trennung, Deadline-/Cleanup-Härtung, Retry-/Quarantäne-Konvention" aus
`requirement.md`.

## Zusammenfassung

- **Lane-Trennung fehlt vollständig:** Beide Workflows führen exakt einen blockierenden
  `dotnet test Rezepte.sln`-Schritt für beide Testprojekte aus (pr-staging-ci.yml
  Z. 142–151, staging-ci.yml Z. 131–136). Kein `continue-on-error`, kein `--filter`,
  keine Traits, kein Retry — nirgends im Repo. Der Kommentar in `pr-staging-ci.yml`
  Z. 143–146 dokumentiert den Status quo explizit („both test projects are blocking").
- **Projekt-Split existiert bereits als Basis:** `Rezepte.Tests` (587 Methoden, überwiegend
  deterministisch) vs. `Rezepte.Tests.Browser` (34 `[SkippableFact]`-Methoden, per
  Konstruktion umgebungssensitiv: Playwright-Chromium + echter Kestrel-Kindprozess +
  `TcpListener`-Stub + SQLite-Temp-DBs).
- **Alle in der Anforderung genannten Fundstellen sind verifiziert:** `StartupTimeoutSeconds = 60`
  (RezepteAppFixture Z. 29), TOCTOU-`GetFreeTcpPort()` (Z. 263–275), best-effort-`Directory.Delete`
  ohne Retry (Z. 156–172), verworfene Kindprozess-Ausgabe (Z. 215–218), 5-s-Accept-Loop-Warte
  in `GitHubStubServer.Dispose` (Z. 102), 1-s-Default in `WaitUntilLoadingBarActiveAsync`
  (Z. 245), fester `WaitForTimeoutAsync(500)`-Sleep (LoadingBarFormNavigationBrowserTests
  Z. 70) und alle `Task.Delay`-Stellen in `Rezepte.Tests` an den genannten Zeilen.
- **Neue Audit-Funde über die Anforderung hinaus:** `ContractExportScriptTests` (pwsh- +
  `dotnet build`-Subprozesse inkl. NuGet-Restore) und `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…`
  (`dotnet publish -r linux-x64`-Subprozess) sind die stärksten `OsInterface`-Kandidaten
  innerhalb von `Rezepte.Tests`; `PluginManagerTests` enthält zwei Tests mit stillem
  No-op, wenn kein externer Plugin-Checkout vorliegt (`REZEPTE_EXTERNAL_PLUGINS_PATH`);
  `ExportCleanupHostedServiceTests` (100-ms-Festwarte) und `PluginUpdateServiceTests_Serialization`
  (250-ms-Negativwarten) sind echte Timing-Flake-Quellen; die AI-Plugin-Projekte nutzen
  `<Reference>`+`HintPath` statt `ProjectReference` auf `Rezepte.Web.dll` → dokumentierte
  Build-Reihenfolge-Falle (Baseline-Lauf 1).
- **Coverage-Wechselwirkung verifiziert:** Die Browser-Tests tragen **nicht** zur
  Zeilenabdeckung von `Rezepte.Web` bei (Kindprozess ohne Collector-Instrumentierung);
  das 70-%-Gate aggregiert `coverage.cobertura.xml` beider Projekte via ReportGenerator —
  Lane-Split verändert das Aggregat nur um den Minimalanteil der Testassembly.
- **Neue Artefakte fehlen alle:** `OsInterface`/`Flaky`-Traits (kein `[Trait]` im Repo),
  Meta-Test, `TempDirectoryCleanup`, `scripts/Retry-FlakyTests.ps1`,
  `scripts/Analyze-FlakyTestResults.ps1`, Doku-Pendant — alles zu erzeugen.
- **TRX-/Artefakt-Benennung uneindeutig:** beide Projekte schreiben `test-results.trx`;
  kein Run-Attempt-Bezug (`github.run_attempt` wird nirgends genutzt). Zusätzlicher
  Befund: Der Upload `playwright-traces/` greift auf das Repo-Root zu, die Traces landen
  aber (lokal verifiziert) im `bin/`-Ausgabeverzeichnis der Testassembly — möglicherweise
  leeres CI-Artefakt (Details in [konventionen.md](inventory/konventionen.md)).
- **Randbefund `release.yml`:** Das Release-Gate ruft die Tests bereits projektweise
  getrennt auf (beide blockierend, ohne TRX/Coverage) — außerhalb des benannten Scopes,
  aber konsistent zur beabsichtigten Lane-Trennung.
- **Test-Ausgangszustand:** `Rezepte.Tests` 764/764 grün; `Rezepte.Tests.Browser` 33/34
  mit **einem dokumentierten Flake** (`RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults`,
  30-s-`GotoAsync`-Timeout auf `/login`), der in der isolierten Wiederholung grün wurde —
  Nachweis in [tests.md](inventory/tests.md).

## Details

- [CI-Workflows und Coverage-/Artefakt-Mechanik](inventory/workflows.md)
- [Testinfrastruktur und Audit-Funde (Fixtures, Page Objects, Timeouts)](inventory/testinfrastruktur.md)
- [Vollständige Testliste beider Projekte mit Einstufung](inventory/testliste.md)
- [Konventionen: Traits, Skripte, Doku, Helfer, Env-Variablen, TRX-Benennung](inventory/konventionen.md)
- [Test-Ausgangszustand und bestehende Testklassen](inventory/tests.md) — Nachweisdateien unter [test-results/](inventory/test-results/)
