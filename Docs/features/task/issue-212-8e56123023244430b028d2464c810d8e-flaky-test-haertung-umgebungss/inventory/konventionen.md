# Konventionen, Skripte, Doku und Artefakt-Mechanik — Bestand

Was im Repo bereits an Konventionen/Mechaniken existiert, auf die die neuen Artefakte
(`OsInterface`/`Flaky`-Traits, `TempDirectoryCleanup`, `Retry-FlakyTests.ps1`,
`Analyze-FlakyTestResults.ps1`, Doku-Pendant) aufsetzen müssen.

## Traits und Test-Markierung

- **`[Trait]`-Attribute kommen im gesamten Repo nicht vor** (verifiziert per
  Volltextsuche; Treffer nur in der Anforderung selbst und einem Kommentar in
  `pr-staging-ci.yml` Z. 145). Es gibt weder `OsInterface`- noch `Flaky`-Traits noch
  einen Meta-Test, der Trait-Kombinationen prüft.
- xUnit 2.9.3 in beiden Projekten → `[Trait("Name", "Value")]` steht ohne Zusatzpaket
  zur Verfügung; `dotnet test --filter`-Ausdrücke wie `Category=…`/`Trait=…` sind damit
  nutzbar. Es gibt aktuell **keinen** `--filter`-Aufruf in Workflows/Doku.
- `Xunit.SkippableFact` 1.5.61 ist nur in `Rezepte.Tests.Browser.csproj` (Z. 19)
  referenziert. `Rezepte.Tests.csproj` hat kein Skip-Paket — dynamische Skips sind dort
  aktuell nicht möglich (relevant für den offenen Quarantäne-Übergangsmechanismus).
- Namenskonventionen der Tests: `Methode_Szenario_Erwartung` bzw.
  `X_ShouldY_WhenZ`; Browser-Tests enden auf `…BrowserTests`; `[SkippableFact]` überall
  im Browser-Projekt mit `Skip.IfNot`-Vorbedingungen statt Attribut-Parameter.

## Skripte unter `scripts/`

| Datei | Zweck | Konvention |
|-------|-------|------------|
| `Build-Site.ps1` | Assembliert die Pages-Site | pwsh-Skript, im `pages.yml`-Workflow aufgerufen |
| `Export-ImportContract.ps1` | Contract-Export + optionale ApiCompat-Prüfung | pwsh; wird aus Workflows mit `shell: pwsh` aufgerufen; hat eigene Prozess-Tests (`ContractExportScriptTests`) |
| `check-site.mjs`, `resolve-release-version.mjs` | Node-Helfer | mjs |
| `screenshots/` | Screenshot-Generierung | eigene package.json |

**Nicht vorhanden** (verifiziert): `Retry-FlakyTests.ps1`,
`Analyze-FlakyTestResults.ps1` oder sonstige TRX-Auswertung/Retry-Skripte. Die CI ruft
PowerShell-Skripte über `shell: pwsh` auf (Bash ist Default-Shell auf ubuntu-Runnern).

## Dokumentations-Konventionen

- `Docs/testing.md` — Test-Ausführungs-Doku; beschreibt aktuell nur `dotnet test` und
  die Browser-Test-Voraussetzungen (Playwright-Install + Publish). Muss für die
  Lane-Trennung angepasst werden.
- `Docs/help/` — featurebezogene Hilfeseiten (flache Liste + Unterverzeichnisse wie
  `git-hooks/`, `security-txt/` mit `index.md` + `beschreibung.md`/`ablauf-technisch.md`/
  `business-rules.md`-Struktur). Ein Pendant zur Vorlagen-Datei
  `docs/help/stabilitaet/os-interface-tests.md` existiert **nicht**; naheliegend wäre
  z. B. `Docs/help/test-stabilitaet.md` oder ein Unterverzeichnis.
- `Docs/features.md` Abschnitt „CI/CD" (Z. 57–62) — verweist auf
  `Docs/help/github-actions.md`.
- `Docs/help/github-actions.md` — beschreibt den aktuellen Ablauf wortwörtlich
  (`dotnet test Rezepte.sln`, Z. 27) und muss bei Lane-Trennung synchronisiert werden;
  dokumentiert bereits die projektweisen Release-Testaufrufe (Z. 65–70).
- Sprachkonvention: Doku deutsch, Code/Kommentare englisch (CLAUDE.md).

## Gemeinsame Test-Helfer

- Es existiert **kein** gemeinsames Test-Helper-Projekt und kein Shared-Project.
  Einzige projektübergreifende Teilung: `Rezepte.Tests/TestHelpers/RepositoryPaths.cs`
  wird per `<Compile Include="..\Rezepte.Tests\TestHelpers\RepositoryPaths.cs"
  Link="TestHelpers\RepositoryPaths.cs" />` in `Rezepte.Tests.Browser.csproj` (Z. 29)
  eingebunden. Dasselbe Muster wäre für `TempDirectoryCleanup` verfügbar; alternativ
  ginge ein Platz in `Rezepte.Tests/TestHelpers/` mit `<Compile Include>` im
  Browser-Projekt (Namespace-Frage: `Rezepte.Tests.TestHelpers` ist heute der
  Namensraum der Datei und wird im Browser-Projekt via `using Rezepte.Tests.TestHelpers`
  genutzt, s. `RezepteAppFixture.cs` Z. 4).
- `Rezepte.Tests.Browser` referenziert `Rezepte.Web` (Z. 26), **nicht**
  `Rezepte.Tests` — ein Helfer in `Rezepte.Tests` wäre für das Browser-Projekt nur über
  `<Compile Include>` (Datei-Verlinkung) nutzbar, nicht über eine Projektreferenz.

## `REZEPTE_PUBLISH_DIR`-Nutzung

- Einzige Fundstelle: `RezepteAppFixture.cs` Z. 25 (`PublishDirectoryEnvironmentVariable`)
  mit Auflösung in `ResolveApplicationDllPath` (Z. 279–294). Keine Verwendung in
  Workflows, Skripten oder Doku (verifiziert per Volltextsuche) — der Override existiert,
  wird aber nirgends gesetzt; die Workflows publishen in den Standard-Pfad, den der
  Fallback-Zweig (Z. 304–309) findet.
- Verwandte Env-Variable: `REZEPTE_EXTERNAL_PLUGINS_PATH` in
  `PluginManagerTests.PluginWorkspace.GetExternalPluginRepositoryRoot` (Z. 788) —
  Override für den externen Plugin-Checkout `external/rezepte-import-plugins-private`.

## TRX- und Artefakt-Benennung

- Beide Workflows: `--logger "trx;LogFileName=test-results.trx"` → identischer Dateiname
  in `Rezepte.Tests/TestResults/` und `Rezepte.Tests.Browser/TestResults/`.
- Upload-Aktionen sammeln beide `TestResults/*.trx`-Pfade in ein Artefakt
  (`test-results-pr` / `test-results-staging`); upload-artifact@v7 erhält die
  Verzeichnisstruktur, die Dateinamen selbst bleiben uneindeutig.
- `github.run_attempt`/`github.run_id` werden nirgends in Datei- oder Artefaktnamen
  verwendet; ein Retry-Schritt müsste eigene Namen (z. B. `*-rerun.trx`) erzeugen.
- Playwright-Traces: `playwright-traces/{Prefix}-{yyyyMMdd-HHmmss}-{Guid:N}.zip`,
  geschrieben relativ zum Arbeitsverzeichnis des Testhosts aus
  `BrowserPageObjectBase.DisposeAsync` (Z. 134–142), `LoadingBarPageObject` (Z. 297–305)
  und `PluginSettingsPageObject` (Z. 267–275) — jede Page-Object-Disposal legt ein Trace
  an, auch bei grünen Tests.
- **Diskrepanz Trace-Artefakt-Pfad (verifiziert lokal):** Die Workflows laden
  `playwright-traces/` **relativ zum Repo-Root** hoch (pr-staging-ci.yml Z. 221–232,
  Kommentar: „relative to the repo root, since the Test step above runs dotnet test from
  there"). Im lokalen Baseline-Lauf (.NET SDK 11, Windows) landeten die Traces jedoch
  unter `Rezepte.Tests.Browser/bin/Release/net10.0/playwright-traces/` — d. h. das
  Arbeitsverzeichnis des Testhosts ist das Ausgabeverzeichnis der Testassembly, nicht
  der Aufrufort von `dotnet test`. Ob das Repo-Root-Verhalten in der CI (.NET SDK 10,
  Ubuntu) tatsächlich zutrifft, ist an dieser Stelle nicht verifizierbar; falls der
  Testhost wie lokal im `bin/`-Verzeichnis läuft, lädt der Schritt leer hoch
  (upload-artifact-Warnung, kein Fehler).
- `TestResults/` ist per `.gitignore` Z. 44 (`[Tt]est[Rr]esult*/`) ignoriert;
  `playwright-traces/` als Root-Verzeichnis ist nicht explizit gelistet, liegt aber
  faktisch unter `bin/` (ignoriert).

## Coverage-Mechanik

- Erhebung: `--collect:"XPlat Code Coverage"`; `coverlet.collector` 10.0.1 in beiden
  Testprojekten → `coverage.cobertura.xml` unter `<Projekt>/TestResults/<GUID>/`.
- Aggregation: ReportGenerator 5.5.11, Glob `**/TestResults/**/coverage.cobertura.xml` →
  `coverage-report/Summary.txt` (`-reporttypes:TextSummary`).
- Gate: `COVERAGE_THRESHOLD: '70'`, Zeilenabdeckung aus `Summary.txt` geparst,
  `awk`-Vergleich; fehlende Zeile = harter Fehler. Identisch in beiden Workflows.
- **Beitrag der Browser-Tests zur Rezepte.Web-Abdeckung = 0**: die App läuft
  out-of-process ohne Collector (s. workflows.md → Befunde). Das Cobertura-File von
  `Rezepte.Tests.Browser` enthält nur die Testassembly selbst. Bei einer Lane-Trennung
  ändert sich am 70-%-Gate faktisch nur der minimale Anteil der Testassembly am
  Gesamtaggregat — sofern beide Cobertura-Dateien weiter in `coverage-report/` landen.

## Build-/Publish-Koppelung der Browser-Tests

- `Rezepte.Tests.Browser.csproj` Z. 32–34: Target `PublishRezepteWebForBrowserTests`
  (`AfterTargets="ResolveProjectReferences"`) publisht `Rezepte.Web` automatisch beim
  Projektbuild, sobald `Rezepte.Web.dll` gebaut ist — lokale Browser-Tests brauchen
  daher keinen separaten Publish-Schritt; CI publisht zusätzlich explizit (idempotent).
- `Rezepte.Tests.csproj` referenziert `Rezepte.Web`, alle Produktiv-Pluginprojekte,
  `Rezepte.Import.*` und `Rezepte.Tests.PluginFixture` (Z. 31–37) — die AI-Plugins
  ziehen `Rezepte.Web.dll` per `<Reference>`+`HintPath` nach (s. testinfrastruktur.md).

## Review-Historie zu Flakes

`Docs/features/task/issue-177-…/review-abnahme.md` Z. 73 dokumentiert einen früheren
Flake von `Register_WithDemoData_CreatesDemoData` (UI-Flakiness beim Nachlesen) —
konsistent mit der aktuellen Baseline-Beobachtung und der `DemoDataWaitHelper`-Poll-Logik.
