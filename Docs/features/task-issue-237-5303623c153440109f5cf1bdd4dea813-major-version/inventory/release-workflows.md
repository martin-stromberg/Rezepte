# Bestandsaufnahme: Release-/CI-Workflows

Stand: Branch `task/issue-237-5303623c153440109f5cf1bdd4dea813-major-version`, HEAD `e9aed2d` (= `staging`-Stand inkl. .NET-11-Migration).

## `.github/workflows/release.yml` (Name: `Release`)

- Trigger (Zeilen 10–13): `push` auf `branches: [main]` und `tags: ['v*.*.*']`. Kein `pull_request`-, `workflow_dispatch`- oder Branch-Filter für andere Branches — ein Push auf einen Maintenance-Branch löst den Workflow aktuell **nicht** aus.
- `permissions: contents: write` (Zeilen 15–16); `concurrency.group: release`, `cancel-in-progress: false` (Zeilen 22–24) — serialisiert alle Release-Läufe (würde bei mehreren Release-Branches weiterhin greifen).
- Ein Job `release` auf `ubuntu-latest`, `timeout-minutes: 30` (Zeilen 27–29).
- Schritte:
  - Checkout `fetch-depth: 0`, `fetch-tags: true` (Zeilen 32–36), Node 24 (38–41), .NET `11.0.x` (43–46), `npm ci` (48–49).
  - `Resolve release version` (51–61): `node scripts/resolve-release-version.mjs` mit `GITHUB_TOKEN`; liefert Outputs `released`, `reason`, `version`, `tag`, `release_kind`, `release_action`.
  - Asset-Reparatur-Pfad (63–91): bei `release_action == 'upload-existing'` Checkout des Tags, Verifikation des Commit, Wiederherstellung von `.github/actions/build-and-package` vom Trigger-Commit.
  - `release_action == 'create'`: Restore, Build `Rezepte.Web` mit `-p:Version`, Build beider Testprojekte, Playwright-Install, `dotnet publish` (102–130).
  - Release-Gate Tests (132–141): `dotnet test Rezepte.Tests --no-build` **und** `dotnet test Rezepte.Tests.Browser --no-build` — im Release-Pfad (anders als in PR/staging-CI) **blockierend**, keine OsInterface-/Best-Effort-Trennung.
  - `build-and-package` Composite Action (143–148): publish self-contained `net11.0` für linux-x64 + win-x64, ZIPs `release-linux-x64.zip`/`release-win-x64.zip`, `update.json` (siehe `.github/actions/build-and-package/action.yml` Header).
  - `Export import plugin contract` (150–195): `./scripts/Export-ImportContract.ps1`; installiert `Microsoft.DotNet.ApiCompat.Tool` und vergleicht gegen `contract-baselines/import-contract/`, wenn vorhanden; schreibt `artifactUrl` in `contract-export.metadata.json`.
  - Drei sich gegenseitig ausschließende Publish-Pfade:
    - `release_kind == 'automatic'` (197–215): `npm run release` = `semantic-release` mit `RELEASE_ASSET_PATHS` (beide ZIPs, Contract-ZIP, Metadata-JSON) + `RELEASE_MANIFEST_PATH=artifacts/update.json`.
    - `release_kind == 'manual'` (217–234): `gh release create` mit `--generate-notes`.
    - `release_action == 'upload-existing'` (236–248): `gh release upload --clobber`.
- Wichtig für den Maintenance-Zweig: Der Workflow-Trigger wird aus der Workflow-Datei **des gepushten Commits** gelesen. Auf `bf108b7` (letzter .NET-10-Stand von `main`) existiert die Datei bereits mit `branches: [main]` und `dotnet-version: '10.0.x'` (Nachweis: `git show bf108b7:.github/workflows/release.yml`, Zeilen 12 und 46). Ein Release-Pfad für `1.x` erfordert daher eine Änderung **auf dem `1.x`-Branch selbst**.

## `.github/workflows/staging-ci.yml` (Name: `Pre-Release`)

- Trigger (Zeilen 14–17): `push` nur auf `staging`.
- `detect-backmerge` (28–60): setzt `is_backmerge=true`, wenn `origin/main` direkter Merge-Parent ist oder der Tree identisch zu `origin/main` (`git diff --quiet`) — dann werden alle Folgejobs übersprungen. Vergleicht fest gegen `origin/main`; ein Maintenance-Branch beeinflusst diese Logik nicht.
- `static-checks` (62–90), `build-and-test` (92–190) und `browser-tests` (192–304, `continue-on-error: true`) laufen parallel; `browser-tests` ist bewusst **nicht** in den `needs` des `version`-Jobs.
- `version`-Job (306–372), `needs: [detect-backmerge, static-checks, build-and-test]`:
  - `Determine next version` (332–356): `npx semantic-release --dry-run --no-ci --branches staging`. Der `--branches`-CLI-Parameter **ersetzt** die `branches`-Konfiguration aus `release.config.js` vollständig für diesen Aufruf; ein zusätzlicher Maintenance-Eintrag in `release.config.js` verändert die RC-Ermittlung daher nicht.
  - `Determine RC number` (358–372): `rc_number = Anzahl vorhandener Tags "v<version>-rc.*" + 1`; Outputs `rc_tag` (`vX.Y.Z-rc.N`) und `rc_version` (`X.Y.Z-rc.N`).
- `prerelease`-Job (374–427), nur bei `changed == 'true'`: `build-and-package` mit RC-Version/Tag, Contract-Export (nur Validierung, kein Asset), `gh release create --prerelease --generate-notes` (418–427).

## `.github/workflows/staging-to-main-promotion.yml`

- Trigger (Zeilen 14–16): `push` nur auf `staging`.
- Erstellt bei `commits_ahead != 0` (Tree-Diff zu `origin/main`) einen **Draft-PR** `staging -> main`, Titel `Automated promotion from staging to main`, Label `automated-promotion` (43–76); dupliziert keinen offenen PR. Merge ist manuelle Maintainer-Aktion (Kommentar Zeilen 3–8).
- Reagiert nicht auf Maintenance-Branch-Pushes; PR-Basis fest `main`, Head fest `staging`.

## `.github/workflows/sync-staging-with-main.yml` (Name: `Backmerge Main to Staging`)

- Trigger (Zeilen 12–14): `push` nur auf `main`.
- Erstellt PR `main -> staging` (`Backmerge from main to staging`, Label `automated-backmerge`), wenn `staging` hinter `origin/main` liegt (41–67).
- Kommentar (Zeilen 8–10): Der Back-Merge **muss** mit "Create a merge commit" gemergt werden, damit der Release-Merge-Commit und sein Tag von `staging` erreichbar bleiben.
- Nach einem Push auf einen `1.x`-Maintenance-Branch passiert hier nichts — es gibt keinen Rückfluss-Mechanismus (Deckt sich mit Scope-Ausschluss 3/5 der Anforderung).

## `.github/workflows/verify-pr-source.yml`

- Trigger (Zeilen 3–5): `pull_request` nur gegen `main`; `permissions: {}`.
- Einziger Check (13–20): schlägt fehl, wenn `github.head_ref != "staging"`.
- Gilt **nur** für PRs nach `main`. PRs gegen einen `1.x`-Branch werden von keiner Regel eingeschränkt — aber auch von keinem Workflow geprüft (siehe unten).

## `.github/workflows/pr-staging-ci.yml` (Name: `PR CI for Staging`)

- Trigger (Zeilen 11–18): `pull_request` gegen `staging` (`opened`/`synchronize`/`reopened`).
- Jobs: `detect-backmerge` (30–62, gleiche Logik wie staging-ci), `static-checks` (64–101: `dotnet format --verify-no-changes --severity error`, Security-Scan, Build mit `TreatWarningsAsErrors`), `build-and-test` (103–205: deterministische Lane `Category!=OsInterface` + Coverage-Gate 70 % + Contract-Export/ApiCompat), `browser-tests` (207–326, `continue-on-error: true`, inkl. OsInterface-Subset, Flaky-Retry via `scripts/Retry-FlakyTests.ps1`, Analyse via `scripts/Analyze-FlakyTestResults.ps1`), `no-pending-backmerge` (328–351: blockiert, solange ein offener Back-Merge-PR `main -> staging` existiert), `back-merge-skip` (353–359).
- PRs gegen `1.x` lösen diesen Workflow **nicht** aus: auf dem Maintenance-Branch gäbe es aktuell **keinerlei PR-CI** (Lücke).

## Übrige Workflows (Vollständigkeit)

`pages.yml` (push `main`/PR `staging`/`main`, Docs-Pages-Deploy — ein Maintenance-Release triggert keinen Pages-Lauf), `security-scan.yml` (wöchentlicher Cron + `workflow_dispatch`), `review-*.yml` (Zeitpläne), `flaky-test-quarantine` existiert nur im Plugin-Repo. `schedule`-Trigger wirken nur auf dem Default-Branch `main` (Doku: `Docs/help/github-actions.md:83`).
