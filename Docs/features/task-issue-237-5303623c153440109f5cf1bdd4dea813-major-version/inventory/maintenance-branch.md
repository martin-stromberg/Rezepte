# Bestandsaufnahme: Voraussetzungen auf dem Maintenance-Branch (`1.x`)

## Was für `push -> 1.x -> Release` auf dem Branch selbst existieren muss

GitHub Actions wertet bei `push`-Triggern die Workflow-Datei **des gepushten Commits** aus. Auf dem Kandidaten-Commit `bf108b7` (letzter .NET-10-Stand von `main`, Promotion #231) existieren bereits:

| Datei | Stand auf `bf108b7` | Erforderliche Änderung für `1.x` |
|-------|---------------------|----------------------------------|
| `.github/workflows/release.yml` | `on.push.branches: [main]` (Z. 12), `tags: ['v*.*.*']` (Z. 13), `dotnet-version: '10.0.x'` (Z. 46) | `branches` um `'1.x'` ergänzen (SDK-Pin ist bereits korrekt für .NET 10) |
| `release.config.js` | `branches: ["main"]` (Z. 74) | `"1.x"` hinzufügen (impliziter `range`) oder `{ name: <freier Name>, range: "1.x" }` |
| `scripts/resolve-release-version.mjs` | `AUTOMATIC_RELEASE_BRANCHES = ["main"]` (Z. 23) | `"1.x"` ergänzen, sonst `Unsupported release ref 'branch:1.x'` (Z. 53) |
| `package.json`/`package-lock.json`, `node_modules`-Setup via `npm ci` | vorhanden (semantic-release 25.x, Node-Engine `24.x`) | keine |
| `contract-baselines/`, `scripts/Export-ImportContract.ps1`, `.github/actions/build-and-package` | vorhanden | keine (build-and-package pinned dort auf .NET 10 — Nachweis: Workflow-Stand `bf108b7` nutzt `dotnet-version: '10.0.x'`) |

Wichtig: Diese Änderungen müssen **auf dem `1.x`-Branch committet** werden (erste Änderung auf `1.x` selbst). Konsistente Erweiterung auf `staging`/`main` ist für spätere Maintenance-Linien sinnvoll, ersetzt aber nicht den Commit auf `1.x` — ein Push auf `1.x` sieht nur den dortigen Stand.

## Workflow-/Gate-Landschaft für `1.x`

- `release.yml` löst nach Erweiterung auf `1.x` denselben Voll-Pfad aus (Version via semantic-release Dry-Run → Tests → Assets → GitHub Release) — eine reduzierte Variante existiert nicht (offene Frage 6 der Anforderung).
- `verify-pr-source.yml` greift nur bei PRs gegen `main` (Trigger `pull_request.branches: [main]`, `.github/workflows/verify-pr-source.yml:3-5`): PRs gegen `1.x` sind **nicht** quellbeschränkt — Feature-Branches können direkt dorthin gemergt werden (deckt sich mit Scope-Ausschluss 3: kein Rückfluss nach `main` nötig).
- `pr-staging-ci.yml` triggert nur für Basis `staging`: PRs gegen `1.x` erhalten **keine** PR-CI (Lücke — Tests liefen dann nur noch im Release-Gate von `release.yml`, das bei `release_action=create` beide Testprojekte blockierend ausführt, `release.yml:132-141`).
- `staging-ci.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml` triggern ausschließlich auf `staging`- bzw. `main`-Pushes — keine Kollision mit `1.x`.
- Der `detect-backmerge`-Check vergleicht fest gegen `origin/main` (staging-ci.yml:41-57); ein `1.x`-Push berührt ihn nicht.
- `concurrency.group: release` (release.yml:22-24) serialisiert Releases über beide Linien — gewollt, da `resolve-release-version.mjs` repo-weit gegen Tags/Releases prüft.
- Reparatur-Logik `incompleteReleases` (resolve-release-version.mjs:236-253) betrachtet **alle** stabilen Releases linienübergreifend — beidseitige „fremde" Reparaturen sind möglich (siehe version-resolution.md).
- Tag-Namespace `v*.*.*` ist gemeinsam: manuelle `v1.y.z`-Tag-Pushes nutzen den `manual`-Pfad der Workflow-Datei des getaggten Commits.

## Branchpunkt

- `main` HEAD `bf108b7` ist der letzte .NET-10-Stand (nachgewiesen: `Directory.Build.props` dort `net10.0`; die .NET-11-Migration `e9aed2d` liegt nur auf `staging`, `git merge-base`-/Log-Nachweis über Elternkette).
- `v1.22.0` (Commit `79fb92d`) ist direkter Merge-Parent von `bf108b7` → für einen `1.x`-Branch ist der letzte In-Range-Tag erreichbar; semantic-release-Range-Voraussetzung erfüllt.
- Zu dokumentierender SHA für die Anforderung: **`bf108b702907d2ee715bdc0341cedece5d55bf3a`** (= aktueller `origin/main`-Stand; falls die Promotion vor der Branch-Anlage gemergt wird, explizit von diesem SHA abzweigen).
