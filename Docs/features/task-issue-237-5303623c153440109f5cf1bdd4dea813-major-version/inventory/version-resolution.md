# Bestandsaufnahme: Versionsermittlung (`release.config.js`, `resolve-release-version.mjs`, semantic-release)

## `release.config.js` (Repo-Wurzel, 77 Zeilen)

- `module.exports = { branches: ["main"], tagFormat: "v${version}", plugins: ... }` (Zeilen 73–77).
- Kommentar Zeilen 3–11: `staging` ist **bewusst nicht** als Prerelease-Branch eingetragen; die RC-Versionsermittlung läuft ausschließlich über den `--branches staging`-Override im `version`-Job von `staging-ci.yml`. Begründung: `@semantic-release/github` soll bei Staging-Pushes keine eigenen Releases/Tags erzeugen.
- `releasePlugins` (26–57): `@semantic-release/commit-analyzer` + `@semantic-release/release-notes-generator` jeweils mit Preset `conventionalcommits` (nur **Kleinbuchstaben**-Typen, Kommentar Zeilen 30–32: `"Fix:"` wird nicht erkannt), `@semantic-release/github` mit `assets` aus `RELEASE_ASSET_PATHS`/`RELEASE_ASSET_PATH`/`RELEASE_MANIFEST_PATH` und `successComment/failComment: false` (Token-Rechte, dokumentierter Vorfall msTools.Updater).
- `dryRunPlugins` (64–71): nur `commit-analyzer`, aktiviert über `RESOLVE_DRY_RUN === "true"` (Zeile 76) — verwendet vom Dry-Run im Auflösungsskript und implizit vom `--branches`-Override.
- Referenz-Implementierung: `release.config.js` verweist (Zeilen 6–8) auf "Softwareschmiede's release.config.js" — per `gh api` abgerufen: identische Struktur, `branches: ["main"]`, **kein** Maintenance-/Range-Beispiel. Die zitierte Datei `ci-target-schema.md` ist weder in diesem Repo noch unter `martin-stromberg/Softwareschmiede` (Root + `docs/`) auffindbar — offenbar ein externes Design-Dokument; nur Verweise in Kommentaren existieren.

## `scripts/resolve-release-version.mjs` (381 Zeilen)

| Symbol | Zeilen | Funktion |
|--------|--------|----------|
| `VERSION_PATTERN` | 19 | `vX.Y.Z` (optional Prerelease-Suffix) für manuelle Tags |
| `NEXT_RELEASE_PATTERN` | 20 | Parst `The next release version is X.Y.Z...` aus der semantic-release-Ausgabe |
| `AUTOMATIC_RELEASE_BRANCHES` | 21–23 | `["main"]` — Kommentar: Staging-Pushes erreichen das Skript nie |
| `parseManualTag` | 25–36 | Wirft bei Nicht-`v*`-Tags bzw. ungültigem SemVer |
| `parseNextReleaseVersion` | 38–42 | Entfernt ANSI-Sequenzen, extrahiert Version oder `null` |
| `classifyWorkflowRef` | 44–54 | `refType:"tag"` → `{kind:"manual"}`; `refType:"branch"` **und** Name in `AUTOMATIC_RELEASE_BRANCHES` → `{kind:"automatic"}`; sonst `throw "Unsupported release ref '<type>:<name>'."` |
| `readImportContractVersion` | 60–69 | Liest `<ImportContractVersion>` aus `Directory.Build.props` (aktuell `0.3.0`) |
| `expectedReleaseAssetNames` | 75–84 | `release-linux-x64.zip`, `release-win-x64.zip`, `update.json`, `rezepte-import-contract-<contractVersion>.zip`, `contract-export.metadata.json` |
| `releaseHasExpectedAsset` | 86–91 | Prüft alle 5 Assets (`state === "uploaded"`, `size > 0`) |
| `runSemanticReleaseDryRun` | 101–128 | `node_modules/semantic-release/bin/semantic-release.js --dry-run` mit `CI=true`, `RESOLVE_DRY_RUN=true` |
| `remoteTagExists` | 130–145 | `git ls-remote --tags origin refs/tags/<tag>` |
| `listGitHubReleases` | 185–209 | Paginiert `GET /releases` (100/Seite) |
| `incompleteReleases` | 236–253 | Nur **stabile** Releases (keine Prereleases) mit fehlenden Assets — dokumentierter Vorfall-Schutz (msTools.Updater, Zeilen 238–243) |
| `resolveReleaseVersion` | 278–369 | Ablauf siehe unten |

Ablauf `resolveReleaseVersion`:
1. `classifyWorkflowRef` aus `GITHUB_REF_TYPE`/`GITHUB_REF_NAME` (283–286).
2. `manual` (Tag-Push): existiert Release → Assets prüfen → ggf. `upload-existing` (Reparatur) oder `released=false` (`existing-release`); sonst `released=true`, `action=create` (293–328).
3. `automatic` (Branch-Push): semantic-release-Dry-Run → Version gefunden → Release-/Tag-Kollisionsprüfung → `action=create`; Tag existiert bereits → harter Fehler `will not be overwritten` (330–357).
4. Kein neues Release, aber unvollständige stabile Releases vorhanden → Reparatur des **ersten** Treffers aus der (API-sorthistorisch neuesten) Liste (359–363).
5. Sonst `released=false`, `reason=no-release` (365–368).

Bruchpunkt für einen Maintenance-Branch `1.x`:
- Push auf `1.x` löst `release.yml` heute gar nicht aus (Trigger `[main]`).
- Würde der Trigger erweitert, ohne `AUTOMATIC_RELEASE_BRANCHES` anzupassen, würde `classifyWorkflowRef` mit `Unsupported release ref 'branch:1.x'` fehlschlagen (Zeile 53) — der Workflow läuft rot, ohne dass etwas veröffentlicht wird.
- Wechselwirkung beider Linien: `incompleteReleases` scannt **alle** stabilen Releases des Repos unabhängig von der Versionslinie — eine `1.x`-Maintenance-Release mit fehlendem Asset könnte durch einen späteren `main`-Push „repariert" werden (und umgekehrt); der Reparaturpfad checkt den jeweiligen Tag aus (release.yml 63–80), baut also den korrekten Commit, allerdings mit dem Stand von `build-and-package` des Trigger-Commits.
- Tag-Namespace `v*.*.*` ist gemeinsam: ein manuell gepushter `v1.y.z`-Tag löst denselben `manual`-Pfad aus (Workflow-Datei des getaggten Commits).
- Keine Node-seitige Testsuite für dieses Skript vorhanden: `package.json` hat kein `test`-Skript, keine `*.test.mjs`-/`node:test`-Dateien im Repo (Recherche ohne Treffer). Die Annahme „vorhandene Node-Tests mitziehen" aus `requirement.md:53` entfällt damit faktisch.

## semantic-release: Maintenance-Branch-Semantik

- `branches` akzeptiert Strings oder Objekte `{ name, range, prerelease, channel }`. Ein Branch-Name, der selbst ein Range-Muster ist (`1.x`, `1.1.x`, `1.x.x`), wird automatisch als Maintenance-Branch mit `range` = Name behandelt → `branches: ["main", "1.x"]` genügt für einen Branch namens `1.x`.
- Ein frei gewählter Name (z. B. `maintenance/net10`) benötigt die explizite Form `{ name: "maintenance/net10", range: "1.x" }`.
- Versionslogik auf einer Maintenance-Linie: semantic-release bestimmt den letzten Tag **innerhalb der Range**, der auf dem Branch erreichbar ist, und berechnet daraus die nächste Version **innerhalb der Range** (`fix` auf `v1.22.0` → `v1.22.1`, `feat` → `v1.23.0`; Major-Bumps würden die Range verlassen und werden dort nicht erzeugt).
- Voraussetzung: Der Branch muss so abgezweigt werden, dass ein zur Range passender Tag in der Historie erreichbar ist. Bei `1.x` ab `bf108b7` ist `v1.22.0` (Commit `79fb92d`, direkter Merge-Parent) erreichbar — erfüllt.
- Kanal: Ohne `channel`-Angabe gehen Maintenance-Releases auf den Default-Kanal — ein `v1.22.1`-Release nach `v2.0.0` ist eine normale stabile GitHub-Release (kein Prerelease). Ob GitHub sie als „Latest" markiert bzw. ob `releases/latest`-Consumer (App-Updater über `update.json`, siehe `Rezepte.Web/appsettings.json:36-44` `ApplicationUpdates`, und `GitHubReleaseClient.GetLatestReleaseAsync` — letzterer nur für Plugin-Updates fremder Repos) ein älteres `v1.x`-Release als aktuell ansehen, ist ein **offener Risikopunkt** für die Planung.
- Verifikation vorab möglich: `npx semantic-release --dry-run --no-ci --branches <branch>` auf einem lokalen Checkout des Maintenance-Branches.

## Aktueller Versionsstand (Nachweis)

- Höchster stabiler Tag auf `main`: `v1.22.0` (Commit `79fb92d`, Promotion #222). `gh release list`: `v1.22.0` = „Latest".
- `main` HEAD `bf108b7` (Promotion #231): Release-Lauf am 2026-11-24 erfolgreich, erzeugte **kein** Release (`chore:`/`docs:`/`plan:`-Commits erzeugen keinen Bump) — nächste main-Version ohne Gegensteuer wäre `1.23.0` (`feat:` aus der .NET-11-Migration), passend zum vorhandenen RC-Tag `v1.23.0-rc.1` auf `staging`.
- Kein Commit in der Historie trägt `BREAKING CHANGE:` oder ein `type!:`-Präfix (Recherche `git log --grep=BREAKING -i` auf `main`+`staging`, sowie `type!:`-Muster über 200 Commits): Ein Major-Bump hat in diesem Repo noch nie stattgefunden; die nächste Promotion erzeugt ohne Eingriff `v1.23.0`, nicht `v2.0.0`.
