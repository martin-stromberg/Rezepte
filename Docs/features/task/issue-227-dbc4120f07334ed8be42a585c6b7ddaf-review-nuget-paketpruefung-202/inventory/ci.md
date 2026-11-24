# CI-/Workflow-Inventar: NuGet-Prüfung und Test-Gates

Stand der Aufnahme: 2026-11-24, Commit `6f7982882b5ef3515491356c135394fa275718eb`.
Quellen: `.github/workflows/`, `.github/actions/`, `docs/testing.md`,
`docs/dependencies.md`.

## Review-Auslöser

### `.github/workflows/review-nuget-packages.yml`

- Schedule: wöchentlich dienstags 05:00 UTC (`cron: '0 5 * * 2'`), plus `workflow_dispatch`.
- Läuft nur auf dem Default-Branch `main`; auf `staging`/Task-Branches inert.
- Einziger Job `create-review-issue` ruft die Composite Action
  `./.github/actions/create-review-issue` auf (Label `review-nuget-check`,
  Duplikatsschutz: es wird nur ein Issue erzeugt, wenn kein offenes mit dem Label
  existiert; Titel `[Review] NuGet-Paketprüfung – <yyyy-MM-dd>`).
- Der Issue-Body enthält die `@softwarewerkstatt`-Anweisung (Updates, Security,
  Schwachstellen, Pflegezustand, Breaking Changes; unbedenkliche Empfehlungen direkt
  als PR umsetzen) — Issue #227 ist die konkrete Instanz.

### `.github/actions/create-review-issue/action.yml`

Sequenz „Label sicherstellen → per `gh issue list --label` deduplizieren →
`gh issue create`". Wird auch von `review-architecture-security.yml` und
`review-compliance.yml` genutzt.

## Sicherheits-Gates

### `.github/workflows/security-scan.yml`

- Schedule: wöchentlich montags 04:00 UTC, plus `workflow_dispatch`.
- Schritte: Checkout → `setup-dotnet` `10.0.x` → `dotnet restore Rezepte.sln` →
  Composite Action `./.github/actions/security-scan` (Artifact `vulnerable-packages-scan`).

### `.github/actions/security-scan/action.yml`

- Führt `dotnet list Rezepte.sln package --vulnerable --include-transitive` aus,
  schreibt `vulnerable-packages-scan.log`, matcht auf
  `has the following vulnerable packages|Severity` und schlägt mit Exit 1 fehl.
- Wird identisch in `security-scan.yml`, `pr-staging-ci.yml` (Gate 2 des Jobs
  `static-checks`) und `staging-ci.yml` verwendet.

## PR-/Staging-Pipeline

### `.github/workflows/pr-staging-ci.yml` (PRs gegen `staging`)

| Job | Inhalt | Blockierend |
|-----|--------|-------------|
| `detect-backmerge` | Erkennt Back-Merges `main`→`staging`, überspringt dann alle Gates | — |
| `static-checks` | `dotnet format --verify-no-changes --severity error` (Gate 1), Security-Scan via Composite Action (Gate 2), `dotnet build Rezepte.sln -c Release -p:TreatWarningsAsErrors=true` (Gate 3) | ja |
| `build-and-test` | Build `Rezepte.Web` Release, Build `Rezepte.Tests` Release, `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` + Coverage (Gate 4), ReportGenerator + Coverage-Gate ≥ 70 % (Gate 5), ApiCompat-Vertragsexport (`scripts/Export-ImportContract.ps1`, Baselines `contract-baselines/import-contract`) | ja |
| `browser-tests` | Build inkl. `Rezepte.Tests.Browser`, Playwright-Chromium-Install, `dotnet publish Rezepte.Web`, `dotnet test Rezepte.Tests.Browser`, `dotnet test Rezepte.Tests --filter "Category=OsInterface"`, Flaky-Retry (`scripts/Retry-FlakyTests.ps1`), Flaky-Analyse (`scripts/Analyze-FlakyTestResults.ps1`), Uploads (TRX, Playwright-Traces, App-Logs) | nein (`continue-on-error: true`) |
| `no-pending-backmerge` | PR darf nicht mergen, solange ein offener Back-Merge-PR `main`→`staging` existiert | ja |

Build-Reihenfolge im CI-Kommentar dokumentiert: `Rezepte.Web` muss vor den
Testprojekten gebaut werden, weil die AI-Plugin-Projekte `Rezepte.Web.dll` per
`HintPath` referenzieren.

### `.github/workflows/staging-ci.yml` (Push auf `staging`)

Gleiche Job-Struktur/Gates wie `pr-staging-ci.yml` (Format-Check, Security-Scan,
`TreatWarningsAsErrors`-Build, deterministische Test-Lane mit Coverage,
Best-Effort-Browser-Lane); zusätzlich Versions-/Release-nahe Jobs.

### Weitere Workflows mit Test-/Build-Bezug

- `release.yml`: `dotnet restore`, `dotnet build Rezepte.Web` + `Rezepte.Tests` +
  `Rezepte.Tests.Browser` (Release), dann `dotnet test` beider Testprojekte ungefiltert.
- `verify-pr-source.yml`, `staging-to-main-promotion.yml`,
  `sync-staging-with-main.yml`, `pages.yml`, `review-architecture-security.yml`,
  `review-compliance.yml`: für die Paketprüfung nicht unmittelbar relevant
  (Workflow-Regeln, Promotion, Review-Issue-Erzeugung anderer Typen).

## Projektübliche Testbefehle (Quellen: `docs/testing.md`, Workflows)

| Suite | Befehl | Lane |
|-------|--------|------|
| `Rezepte.Tests` (deterministisch) | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "Category!=OsInterface"` | blockierend (CI) |
| `Rezepte.Tests` (OsInterface-Teilmenge) | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "Category=OsInterface"` | best-effort (CI, Browser-Job) |
| `Rezepte.Tests` (lokal komplett) | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` | lokal |
| `Rezepte.Tests.Browser` | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` (nach Playwright-Chromium-Install via `playwright.ps1`) | best-effort (CI) |
| Vulnerability-Gate | `dotnet list Rezepte.sln package --vulnerable --include-transitive` | blockierend (CI) |

Trait-Mechanik: `[Trait("Category", "OsInterface")]` kennzeichnet
umgebungssensitive Tests; `Flaky` erlaubt genau einen Retry (siehe
`docs/help/os-interface-tests.md`, `Rezepte.Tests/TestHelpers/TestCategories.cs`).
