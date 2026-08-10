# Bestandsaufnahme: Einfuehrung staging-Branch

## Projektuebersicht

- Repository: `Rezepte` (.NET 10 Web-Projekt mit Plugin-Import-System)
- Loesungsdatei: `Rezepte.sln`
- Hauptprojekte:
  - `Rezepte.Web` (Web-Anwendung)
  - `Rezepte.Tests` (Tests)
  - `Rezepte.Tests.Browser` (Playwright-Browsertests)
  - `Rezepte.Tests.PluginFixture`
  - `Rezepte.Import.*` (Plugin-Projekte)
- Runtime: .NET 10.0.x, `linux-x64` fuer Release-Publish
- Branching: Aktuell direkte PRs gegen `main`; Release laeuft auf `pull_request_target: closed` fuer `main`.

## Bestehende CI-Artefakte

### `.github/workflows/pr.yml`
- Trigger: `pull_request` auf `main` (opened, synchronize, reopened)
- Schritte:
  - Checkout
  - Setup .NET 10.0.x
  - `dotnet restore Rezepte.sln`
  - Build: `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Tests.Browser`
  - Playwright-Installation
  - Publish fuer Browsertests
  - `dotnet test Rezepte.sln` (Release, no-build)
  - Import-Plugin-Contract-Export mit optionaler API-Compat-Pruefung
  - Formatpruefung: `dotnet format Rezepte.sln --verify-no-changes --no-restore`

### `.github/workflows/release.yml`
- Trigger: `pull_request_target` auf `main`, `types: [closed]`, `if: github.event.pull_request.merged == true`
- Permissions: `contents: write`
- Schritte:
  - Checkout Merge-Commit
  - Semantic-Release-Versionierung anhand letztem `v*`-Tag und Conventional Commits
  - Build, Test, Publish `Rezepte.Web` als `linux-x64` Release
  - Erstellung von `release.zip`
  - Contract-Export und Artefakt-Upload
  - Git-Tag und GitHub Release mit `gh release create`

### Weitere Verzeichnisse
- `.github/ISSUE_TEMPLATE/`: Issue-Templates
- `contract-baselines/import-contract/`: API-Compat-Baseline
- `scripts/Export-ImportContract.ps1`: Contract-Export-Skript
- `github-workflow-1/`, `github-workflow-2/`: Vergleichsrepositories mit Referenz-Workflows

## Luecken gegenueber der Anforderung

1. Kein `staging`-Branch in den CI-Workflows modelliert.
2. Feature-PRs laufen gegen `main` statt `staging`.
3. Keine automatische Erstellung eines Draft-PRs `staging -> main`.
4. Kein Rücksync `main -> staging`.
5. Keine explizite Back-Merge-Erkennung.
6. Keine Branch-Schutzpruefung fuer PR-Quellen auf `main`.

## Detaildokumente

- `inventory/existing-workflows.md` – Zusammenfassung der vorhandenen `.github/workflows/pr.yml` und `release.yml`
- `inventory/reference-workflows.md` – Bewertung der Vergleichsworkflows aus `github-workflow-1` und `github-workflow-2`
