# CI-Workflows — Bestand

Zustand der Workflows `.github/workflows/` bezogen auf die Test-Lane-Trennung,
TRX-/Coverage-Artefakte und Timeouts. Zeilenangaben beziehen sich auf den
untersuchten Commit `7c959cc`.

## `pr-staging-ci.yml` (PR → `staging`)

Jobs: `detect-backmerge` → parallel `static-checks`, `build-and-test`, `no-pending-backmerge` (+ `back-merge-skip`).

| Aspekt | Stand | Fundstelle |
|--------|-------|------------|
| Runner | `ubuntu-latest` | Zeilen 31, 71, 107 |
| Job-Timeout `build-and-test` | `timeout-minutes: 30` | Zeile 108 |
| .NET-Version | `10.0.x` (setup-dotnet@v6) | Zeilen 78–80, 113–116 |
| Build-Reihenfolge | `dotnet restore Rezepte.sln` → `dotnet build Rezepte.Web` → `dotnet build Rezepte.Tests` → `dotnet build Rezepte.Tests.Browser` (jeweils Release, `--no-restore`) | Zeilen 118–128 |
| Playwright-Setup | `playwright.ps1 install --with-deps chromium` aus `Rezepte.Tests.Browser/bin/Release`; Schritt bricht hart ab, wenn `playwright.ps1` fehlt | Zeilen 130–137 |
| Publish für Browser-Tests | `dotnet publish Rezepte.Web … --no-restore` (Standard-Publish-Output; `REZEPTE_PUBLISH_DIR` wird **nicht** gesetzt) | Zeilen 139–140 |
| **Test-Schritt (Gate 4)** | `dotnet test Rezepte.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` — ein einziger blockierender Schritt für **beide** Testprojekte; Kommentar Zeilen 143–146 nennt explizit „no best-effort test category (unlike FinanceManager/Softwareschmiede's OsInterface split) - both test projects are blocking" | Zeilen 142–151 |
| Coverage-Aggregation | ReportGenerator 5.5.11 (`dotnet tool install -g`), `-reports:**/TestResults/**/coverage.cobertura.xml` → `coverage-report/`, `-reporttypes:TextSummary` | Zeilen 153–157 |
| Coverage-Gate (Gate 5) | `COVERAGE_THRESHOLD: '70'`; liest `Line coverage:` aus `coverage-report/Summary.txt`, `awk`-Vergleich `cov >= thr`; schlägt fehl, wenn Zeile fehlt | Zeilen 159–174 |
| Contract-Export | `scripts/Export-ImportContract.ps1` + optionalem ApiCompat-Vergleich | Zeilen 176–201 |
| Artefakte | `coverage-report-pr` (`coverage-report/`, 14 d), `test-results-pr` (`Rezepte.Tests/TestResults/*.trx` + `Rezepte.Tests.Browser/TestResults/*.trx`, 14 d), `playwright-traces-pr` (`playwright-traces/`, 14 d; alle `if: always()`) | Zeilen 203–232 |

**TRX-Benennung:** Beide Projekte schreiben **denselben Dateinamen** `test-results.trx`
in ihre jeweiligen `TestResults/`-Verzeichnisse (`Rezepte.Tests/TestResults/test-results.trx`,
`Rezepte.Tests.Browser/TestResults/test-results.trx`). Der Upload erfasst beide Pfade; es
gibt **keine** Unterscheidung pro Testprojekt im Dateinamen und **keinen**
Run-Attempt-Bezug (`github.run_attempt` wird nirgends verwendet — verifiziert per
Volltextsuche in `.github/workflows/`).

## `staging-ci.yml` (Push → `staging`, „Pre-Release")

Jobs: `detect-backmerge` → parallel `static-checks`, `build-and-test` → `version` → `prerelease`.
`version` hat `needs: [detect-backmerge, static-checks, build-and-test]` (Zeile 213) —
`build-and-test` ist damit **hartes Pre-Release-Gate**.

| Aspekt | Stand | Fundstelle |
|--------|-------|------------|
| Runner / Job-Timeout | `ubuntu-latest`, `timeout-minutes: 30` | Zeilen 96–97 |
| Build-/Playwright-/Publish-Schritte | identisch zu `pr-staging-ci.yml` | Zeilen 107–129 |
| **Test-Schritt** | identisch: `dotnet test Rezepte.sln … --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"` | Zeilen 131–136 |
| Coverage-Report + 70-%-Gate | identisch | Zeilen 138–157 |
| Artefakte | `coverage-report-staging`, `test-results-staging` (beide TestResults-Pfade), `playwright-traces-staging` | Zeilen 184–209 |
| Downstream | `version` (semantic-release Dry-Run + RC-Nummer), `prerelease` (build-and-package + `gh release create --prerelease`) | Zeilen 211–332 |

## `release.yml` (Push → `main` / Tag `v*.*.*`) — Randbefund

Außerhalb des benannten Scopes, aber relevant für die Lane-Diskussion: Der
Release-Gate-Schritt „Release gate - tests" führt die Tests **bereits projektweise
getrennt** aus — `dotnet test Rezepte.Tests … --no-build` **und**
`dotnet test Rezepte.Tests.Browser … --no-build` (Zeilen 132–141) —, allerdings beide
**blockierend** im selben Step ohne TRX/Coverage. Playwright-Install und
Rezepte.Web-Publish laufen vorher (Zeilen 118–130). Eine Lane-Trennung in den
Staging-Workflows berührt diese Stelle nicht; ob die Browser-Lane hier künftig ebenfalls
best-effort werden soll, ist im Scope der Anforderung nicht geregelt.

## Übrige Workflows

`pages.yml`, `security-scan.yml` (Composite-Action `.github/actions/security-scan`),
`staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml` —
keine Testausführung, für die Anforderung nicht relevant.

## Befunde zur Lane-Trennung

- Es existiert **kein** `continue-on-error`, kein `--filter`, kein Trait-basiertes
  Filtering und kein Retry in irgendeinem Workflow (verifiziert per Volltextsuche).
- `dotnet test Rezepte.sln` adressiert beide Testprojekte gemeinsam; nicht-testende
  Projekte (`Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost` — letzteres ein
  EXE-Host für `msTools.Updater` ohne Test-SDK) liefern keine Tests.
- Das Coverage-Gate aggregiert `coverage.cobertura.xml` aus **beiden** Projekten. Die
  Browser-Tests starten `Rezepte.Web` als eigenen `dotnet`-Kindprozess
  (`RezepteAppFixture.StartApplicationProcess`, Zeilen 181–219) **ohne**
  Coverage-Instrumentierung — Beleg im Code: `ProcessStartInfo` setzt nur
  `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Default`, `Jwt__Key`,
  `RateLimiting__Authentication__PermitLimit` + `GetEnvironmentOverrides()`; keine
  Coverlet-/Collector-Variablen. Der Browser-Lane-Beitrag zur `Rezepte.Web`-Zeilenabdeckung
  ist damit faktisch **null**; die Cobertura-Datei von `Rezepte.Tests.Browser` deckt nur
  die Testassembly selbst ab. Die aus der Lane-Trennung resultierende
  Coverage-Veränderung ist entsprechend klein, aber **nicht exakt null** (Anteil der
  Testassembly im Aggregat) — die genaue Quote ist nicht gemessen worden.
- TRX-/Artefakt-Namen sind heute weder pro Projekt noch pro Run-Attempt eindeutig.
