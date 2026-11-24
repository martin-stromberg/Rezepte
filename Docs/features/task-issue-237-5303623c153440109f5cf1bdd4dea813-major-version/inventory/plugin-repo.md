# Bestandsaufnahme: Repository `martin-stromberg/Rezepte.Plugins` (privat)

Abgefragt via `gh api` (Stand 2026-11-24). Default-Branch: `main`; kein `staging`-Branch vorhanden.

## Release-Machinerie — **nicht** semantic-release

| Erwartetes Artefakt (Schema des Hauptrepos) | Vorhanden? |
|----------------------------------------------|------------|
| `release.config.js` | **nein** |
| `package.json`/`node_modules` (semantic-release) | **nein** |
| `scripts/resolve-release-version.mjs` | **nein** |
| `global.json` | **nein** (SDK-Pin nur per `setup-dotnet` in den Workflows) |
| `staging`-/`staging-ci`-/`promotion`-/`sync`-/`verify-pr-source`-Workflows | **nein** |
| Eigene Workflows | `ci.yml`, `release.yml`, `flaky-test-quarantine.yml` |

### `.github/workflows/release.yml` („Release Plugins")

- Trigger: `pull_request types: [closed]` **ohne `branches`-Filter** — läuft bei jedem gemergten PR auf **jeden** Branch (`if: github.event.pull_request.merged == true`).
- Ablauf: Checkout des `merge_commit_sha` → `dotnet 10.0.x` → Contract-Validierung (`Validate-ContractExport.ps1`, `Test-ContractExport.ps1`, `Test-ContractApiCompatibility.ps1` mit `REQUIRE_CONTRACT_API_BASELINE=true` gegen `contract/api-baseline/*.dll`) → `dotnet build` → PluginUsability-Tests → Rest-Tests → `Test-GetNextReleaseVersion.ps1` → PR-Commits via GitHub-API lesen → `scripts/Get-NextReleaseVersion.ps1` → ggf. `publish-plugins.ps1` → `release.zip` → `git tag vX.Y.Z` + `gh release create` (nur `release.zip` als Asset).
- `scripts/Get-NextReleaseVersion.ps1`: eigene Conventional-Commits-Logik — `type!:`-Header oder `BREAKING[ -]CHANGE:`-Footer → `major`, `feat:` → `minor`, `fix:` → `patch`, sonst `none`. Basis: **`$semVerTags[0]` = global höchster `vX.Y.Z`-Tag** (`git tag --list`, absteigend sortiert) — **keine Branch-/Range-Logik**.
- Konsequenz für einen `0.x`-Maintenance-Branch im Plugin-Repo: Ein gemergter Fix-PR auf einen Maintenance-Branch würde den Workflow auslösen (kein Branch-Filter) und vom global höchsten Tag aus bumpen — nach `v1.0.0` auf `main` ergäbe das `v1.0.1` statt `v0.3.8`. Die dortige Versionierung kennt keine Maintenance-Linien; für den im Issue geforderten Plugin-Maintenance-Branch müsste `release.yml`/`Get-NextReleaseVersion.ps1` branch-sensitiv gemacht oder die Release-Erzeugung für Nicht-`main`-Merges eingeschränkt werden.
- Merge-Konvention dort: `allow_merge_commit + allow_rebase_merge: true`, `allow_squash_merge: false`, `merge_commit_title: MERGE_MESSAGE` („Merge pull request #N …"), `merge_commit_message: PR_TITLE`. Die Versionsberechnung liest die **Commits des PR** via API — ein `BREAKING CHANGE:`-Footer in einem Feature-Commit des gemergten PRs löst den Major-Bump aus (`v0.3.7` → `v1.0.0`), unabhängig vom Merge-Message-Text.

### `.github/workflows/ci.yml`

`pull_request -> [main]`: Contract-Validierung + ApiCompat (Baseline erforderlich), `dotnet build`/`test` der `Rezepte.Import.Plugins.sln`, `Test-GetNextReleaseVersion.ps1`. PRs gegen andere Basen (z. B. künftiger Maintenance-Branch) erhalten keine CI.

## Technischer Stand

- `Directory.Build.props`: `net10.0`, `Nullable`/`ImplicitUsings`, `ImportContractVersion 0.3.0` — identische Vertragsversion wie im Hauptrepo (`Directory.Build.props:6`).
- `Rezepte.Import.Abstractions` + `Rezepte.Import.PluginSdk` liegen als **importierte Quellen** im Repo (Contract-Export-Mechanismus), plus `Rezepte.Import.PluginRunner`, 6 Produktiv-Plugins (`Chefkoch`, `SecondSource`…`SixthSource`), `Shared`, `Tests`, `Rezepte.Import.Plugins.sln`, `publish-plugins.ps1`.
- Contract-State: `contract/contract-export.json` (`exportFormat: rezepte-import-contract-v1`, `contractVersion: 0.3.0`), `contract/contract-files.json`, `contract/contract-lock.json` (zeigt auf Contract-ZIP `rezepte-import-contract-0.3.0.zip` aus App-Release **`v1.6.0`**, `sourceCommit 0c35ee2`, `updatedAt 2026-07-26`), `contract/api-baseline/*.dll` für ApiCompat. Updates laufen manuell via `scripts/Update-ContractExport.ps1 -ArtifactUrl … -ArtifactSha256 …`.
- Releases: `v0.2.0`…`v0.3.7` (alles stabil, `v0.3.7` = latest). README badged auf `.NET 10`.
- Doku: `docs/help/ci-release-package.md` beschreibt den Workflow (inkl. Versionsregeln-Tabelle), `docs/help/contract-export.md` den Contract-Pfad — beide bei einem Major-Schritt zu prüfen/ergänzen.

## Implikationen für das dortige Folge-Issue

- .NET-11-Anhebung: `Directory.Build.props` (`net10.0` → `net11.0`), `dotnet-version: 10.0.x` → `11.0.x` in `ci.yml` + `release.yml`, ggf. Package-Updates; kein `global.json` anzupassen.
- Major-Release `v1.0.0`: durch `feat!:`/`BREAKING CHANGE:` in einem gemergten PR-Commit — die Custom-Versionierung unterstützt das bereits.
- Maintenance-Branch im Plugin-Repo hat **kein** bestehendes Muster: weder semantische Branch-Konvention noch range-fähige Versionslogik; `release.yml` würde bei Maintenance-Merges fehlzünden (siehe oben).
- Contract-Update-Pfad: Nach einem neuen App-Release mit geändertem Contract-ZIP muss `contract-lock.json` manuell via `Update-ContractExport.ps1` auf neue URL/SHA-256 gesetzt werden; ob `contractVersion 0.3.0` steigt, ist die offene Frage 2 der Anforderung.
