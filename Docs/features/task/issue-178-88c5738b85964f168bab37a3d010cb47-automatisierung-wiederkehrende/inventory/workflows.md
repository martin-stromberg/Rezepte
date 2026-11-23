# Bestand `.github/workflows/` und `.github/actions/`

Analyse aller acht Workflows und zwei Composite Actions bezogen auf die Anforderung
„drei neue zeitgesteuerte Workflows, die Issues mit `@softwarewerkstatt`-Erwähnung
erzeugen und gegen offene Issues desselben Typs deduplizieren". Zeilenangaben
beziehen sich auf Commit `94957e9`.

## Dateiüberblick

| Datei | Anzeigename (`name:`) | Trigger | Top-Level-`permissions` | `concurrency` |
|-------|----------------------|---------|--------------------------|---------------|
| `pages.yml` | `Pages` | `push`→`main` (paths), `pull_request`→`staging`/`main` (paths), `workflow_dispatch` (Z. 20–39) | `contents: read` (Z. 41–42); `deploy`-Job zusätzlich `pages: write`, `id-token: write` (Z. 80–83) | `pages`, `cancel-in-progress: false` (Z. 44–46) |
| `pr-staging-ci.yml` | `PR CI for Staging` | `pull_request`→`staging`, Typen `opened`/`synchronize`/`reopened` (Z. 11–18) | `contents: read`, `checks: write`, `pull-requests: read` (Z. 24–27) | `pr-staging-${{ github.event.pull_request.number }}`, `cancel-in-progress: true` (Z. 20–22) |
| `release.yml` | `Release` | `push`→`main` + Tags `v*.*.*` (Z. 10–13) | `contents: write` (Z. 15–16) | `release`, `cancel-in-progress: false` (Z. 22–24) |
| `security-scan.yml` | `Security Scan` | `schedule: cron '0 4 * * 1'` + `workflow_dispatch` (Z. 8–13) | **kein `permissions`-Block** (einziger Workflow ohne explizite Permissions; verifiziert per Volltextsuche) | `security-scan-${{ github.workflow }}-${{ github.ref }}`, `cancel-in-progress: true` (Z. 15–17) |
| `staging-ci.yml` | `Pre-Release` | `push`→`staging` (Z. 14–17) | `contents: write`, `pull-requests: write` (Z. 23–25) | `staging-ci`, `cancel-in-progress: true` (Z. 19–21) |
| `staging-to-main-promotion.yml` | `Staging to Main Promotion` | `push`→`staging` (Z. 14–16) | `contents: read`, `pull-requests: write`, `issues: write` (Z. 18–21) | `staging-to-main-promotion`, `cancel-in-progress: false` (Z. 23–25) |
| `sync-staging-with-main.yml` | `Backmerge Main to Staging` | `push`→`main` (Z. 12–14) | `contents: read`, `pull-requests: write`, `issues: write` (Z. 16–19) | `sync-staging-with-main`, `cancel-in-progress: false` (Z. 21–23) |
| `verify-pr-source.yml` | `Verify PR Source` | `pull_request`→`main` (Z. 3–5) | `permissions: {}` (Z. 7, leer) | **kein `concurrency`-Block**, kein `timeout-minutes` |

Dateibenennung durchgängig kebab-case mit `.yml`-Endung (kein `.yaml` im Repo).
`on:` wird gemischt geschrieben: unquotiert in `pages.yml`, `pr-staging-ci.yml`,
`security-scan.yml`, `staging-ci.yml`; als `"on"` gequotet in `release.yml`,
`staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml`.

## Relevante Konventionen im Detail

### Trigger

- `schedule` existiert genau einmal: `security-scan.yml` Z. 9–12 mit
  `- cron: '0 4 * * 1'` (montags 04:00 UTC, per Kommentar begründet). `cron` wird
  einfach gequotet geschrieben.
- `workflow_dispatch` existiert in `pages.yml` (Z. 39) und `security-scan.yml`
  (Z. 13) — jeweils ohne `inputs`.
- Alle `push`-Trigger nutzen `branches:`-Listen; `pages.yml` zeigt zusätzlich den
  `paths:`-Filter als Konvention.

### Runner, Timeouts, Shells

- Durchgängig `runs-on: ubuntu-latest`; jeder Job außer `verify-pr-source.yml`
  hat `timeout-minutes` (5–45).
- Shell-Mischung: `run`-Schritte ohne `shell:`-Angabe laufen in bash (Default
  auf ubuntu-latest); `shell: pwsh` wird explizit gesetzt, wo PowerShell
  genutzt wird — konkret: `git fetch`/Diff-Auswertungen, `gh label create`,
  `gh pr list`/`gh pr create` mit `ConvertFrom-Json`-Auswertung und
  `Write-Host`-Meldungen, Contract-Export- und Flaky-Skript-Aufrufe
  (`staging-to-main-promotion.yml` Z. 41/54/61/80; `sync-staging-with-main.yml`
  Z. 39/46/61/71; `staging-ci.yml` Z. 151/261/274/394; `pr-staging-ci.yml`
  Z. 166/278/291; `release.yml` Z. 157; `pages.yml` Z. 62).
- Schritte ohne Checkout existieren bereits: `no-pending-backmerge` in
  `pr-staging-ci.yml` (Z. 340–351) ruft `gh pr list -R ${{ github.repository }}`
  ohne `actions/checkout` auf — Kommentar Z. 344–345 begründet das (`gh` bekommt
  `-R`, kein lokales Repo nötig); `verify-pr-source.yml` ebenfalls ohne Checkout.

### `gh`-CLI-Nutzung (alle Fundstellen)

| Befehl | Datei / Zeilen | env | Zweck |
|--------|----------------|-----|-------|
| `gh label create automated-promotion --color "0E8A16" --description "..." --force` | `staging-to-main-promotion.yml` Z. 56–60 | `GH_TOKEN: ${{ github.token }}` | Label idempotent sicherstellen |
| `gh pr list --base main --head staging --state open --json number \| ConvertFrom-Json` → `gh pr create --draft --label "automated-promotion"` | `staging-to-main-promotion.yml` Z. 63–80 | `GH_TOKEN: ${{ github.token }}` | Duplikatprüfung + Erstellung (PR) |
| `gh label create automated-backmerge --color "1D76DB" --description "..." --force` | `sync-staging-with-main.yml` Z. 48–52 | `GH_TOKEN: ${{ github.token }}` | Label idempotent sicherstellen |
| `gh pr list --base staging --head main --state open --json number \| ConvertFrom-Json` → `gh pr create --label "automated-backmerge"` | `sync-staging-with-main.yml` Z. 55–71 | `GH_TOKEN: ${{ github.token }}` | Duplikatprüfung + Erstellung (PR) |
| `gh pr list -R ${{ github.repository }} --base staging --head main --state open --json number --jq 'length'` | `pr-staging-ci.yml` Z. 346 | `GH_TOKEN: ${{ github.token }}` | Gate-Check ohne Checkout (bash) |
| `gh release create "$tag" ... --generate-notes` / `gh release upload "$tag" ... --clobber` | `release.yml` Z. 217–248 | `GH_TOKEN: ${{ github.token }}` | Release-/Asset-Verwaltung (bash) |
| `gh release create "$tag" ... --prerelease --generate-notes` | `staging-ci.yml` Z. 418–427 | `GH_TOKEN: ${{ github.token }}` | Pre-Release-Erzeugung (bash) |

**Nicht vorhanden:** `gh issue list` / `gh issue create` / `gh issue` irgendwo —
verifiziert per Volltextsuche in `.github/` und `scripts/` (0 Treffer). Es gibt
keinen Issue-erzeugenden Automatismus im Repo; das Dedup-Muster existiert aber
1:1 für PRs: „`gh ... list --state open --json number` auswerten → bei `Count -eq 0`
erstellen, sonst `Write-Host`-Meldung ohne Fehler" (`staging-to-main-promotion.yml`
Z. 68–79, `sync-staging-with-main.yml` Z. 60–70).

### Action-Versionen (gepinnte Majors)

`actions/checkout@v7`, `actions/setup-dotnet@v6` (`dotnet-version: '10.0.x'`),
`actions/setup-node@v7` (`node-version: '24'`), `actions/upload-artifact@v7`
(`retention-days: 14`), `actions/upload-pages-artifact@v5`,
`actions/deploy-pages@v5`. Keine Third-Party-Actions außer `actions/*` —
der frühere softprops-Release-Schritt wurde laut Kommentar in `release.yml`
Z. 217–221 bewusst durch `gh` ersetzt.

### Kommentar- und Namenskonventionen

- Kommentare in den Workflow-Dateien sind englisch und erklären Warum/Kontext
  (z. B. Herkunft aus `ci-target-schema.md`, Absicht von `cancel-in-progress`),
  passend zur CLAUDE.md-Regel „Program code must be written in English".
- `concurrency.group`: teils statisch (`pages`, `release`, `staging-ci`,
  `staging-to-main-promotion`, `sync-staging-with-main`), teils mit Kontext
  (`security-scan-${{ github.workflow }}-${{ github.ref }}`,
  `pr-staging-${{ github.event.pull_request.number }}`).
- Header-Kommentarblöcke unter `name:` dokumentieren Zweck und Designentscheidungen
  des Workflows (alle Dateien außer `verify-pr-source.yml`).

## Composite Actions `.github/actions/`

| Action | `runs.using` | Inputs | Inhalt |
|--------|--------------|--------|--------|
| `.github/actions/security-scan/action.yml` | `composite` | `solution-path` (Default `Rezepte.sln`), `artifact-name` (Default `vulnerable-packages-scan`) | bash-Step `dotnet list package --vulnerable --include-transitive` mit `DOTNET_CLI_UI_LANGUAGE: en`, `grep`-Befund → `::error::` + `exit 1`; danach `actions/upload-artifact@v7` mit `if: always()` (Z. 22–48) |
| `.github/actions/build-and-package/action.yml` | `composite` | `release-version`, `release-tag` (beide `required`) | `setup-dotnet@v6`, restore/publish für `linux-x64` + `win-x64`, ZIPs, `update.json`-Manifest via `jq` — alles bash (Z. 22–166) |

Muster daraus: Gemeinsame Logik, die an mehreren Stellen läuft, wird als
Composite Action ausgelagert (Explizit-Kommentar in `security-scan/action.yml`
Z. 5–10 und `build-and-package/action.yml` Z. 8–12: „only exists once instead
of being duplicated"). Composite Steps deklarieren `shell:` explizit.

## Live-Stand GitHub (Read-only-Abfrage, 2026-11-23)

- `gh` ist lokal als Account **`softwarewerkstatt`** authentifiziert
  (`gh auth status`, Token-Scopes u. a. `repo`, `workflow`) — derselbe Account,
  den die neuen Issues erwähnen sollen.
- Vorhandene Labels (`gh label list -R martin-stromberg/Rezepte`): die neun
  Standard-Labels (`bug`, `documentation`, `duplicate`, `enhancement`,
  `good first issue`, `help wanted`, `invalid`, `question`, `wontfix`) plus die
  workflow-gepflegten `automated-promotion`, `automated-backmerge`,
  `automated-sync`. **Kein** Review-/Dedup-Label existiert bisher.
- Offene Issues (`gh issue list`): u. a. #178 (diese Anforderung); kein
  Issue eines der drei Review-Typen vorhanden.

## Was für die Anforderung fehlt

- Kein Workflow mit `gh issue create`/`gh issue list` (Issue-Erzeugung ist neu).
- Keine Review-/Typ-Labels (müssten wie `automated-*` per `gh label create --force`
  angelegt werden).
- Keine gemeinsame Composite Action für „Label → Dedup → Issue".
- `security-scan.yml` als einziger Cron-Workflow hat **keinen** expliziten
  `permissions`-Block — die Anforderung verlangt `issues: write`, das dort nicht
  nötig ist (reiner Lesescan), für Issue-Erzeugung aber erforderlich wäre.
