# GitHub Actions

Das Repository enthält Workflows für den zweistufigen Staging-Flow.

## Branch-Modell

- Feature-Branches öffnen Pull Requests gegen `staging`.
- Nach erfolgreicher CI auf `staging` wird automatisch ein Draft-PR `staging -> main` erstellt.
- Nur PRs aus `staging` sind nach `main` erlaubt.
- Nach jedem Merge/Push auf `main` wird ein Sync-PR `main -> staging` erstellt, damit Release-Tags auf `staging` erreichbar bleiben.
- `main` bleibt für stabile Releases und erzeugt `release.zip`.

## Pull-Request-Prüfung auf staging

Der Workflow `.github/workflows/pr-staging-ci.yml` startet für Pull Requests gegen `staging`, wenn ein PR erstellt, wieder geöffnet oder durch neue Commits aktualisiert wird.

Reine Back-Merge-PRs (`main -> staging`) erkennt der Job `detect-backmerge` und alle weiteren Checks werden übersprungen. Solange ein automatisierter Back-Merge-PR `main -> staging` offen ist, schlägt der Job `no pending backmerge` für reguläre PRs fehl — der Back-Merge wird immer zuerst gemergt, damit er nicht durch einen dazwischengemergten Feature-PR veraltet oder konfliktbehaftet wird.

Für alle anderen PRs fuehrt der Workflow auf `ubuntu-latest` mit .NET `10.0.x` diese Prüfungen aus:

- `dotnet restore Rezepte.sln`
- `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
- Playwright-Browser-Installation
- `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
- `dotnet test Rezepte.sln --configuration Release --no-build`
- `./scripts/Export-ImportContract.ps1 -OutputDirectory artifacts/contract-export`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore`
- `dotnet list Rezepte.sln package --vulnerable --include-transitive`

Der Vulnerability-Scan ist im Job `static checks` ein blockierendes Gate.
Transitive NuGet-Pakete werden dabei mitgeprüft; ein gemeldetes verwundbares
Paket lässt den Pull-Request-Check fehlschlagen. Paketupdates müssen daher den
Scan für die gesamte Solution ohne Befunde hinterlassen.

Neue Commits in einem bestehenden Pull Request brechen veraltete PR-Läufe ab und starten die Prüfung für den aktuellen Stand neu.

Wenn gespeicherte ApiCompat-Referenzen unter `contract-baselines/import-contract/<semver>/` vorhanden sind, installiert der Workflow `Microsoft.DotNet.ApiCompat.Tool` und der Contract-Export muss gegen `Rezepte.Import.Abstractions.dll` und `Rezepte.Import.PluginSdk.dll` aus der ausgewählten Baseline bestehen. Ohne explizite `-ApiCompatBaselineVersion` verwendet das Skript die neueste gespeicherte SemVer-Baseline bis zur aktuellen Contract-Version; ohne passende gespeicherte Baseline wird der historische Vergleich protokolliert übersprungen.

Schlaegt dieser Schritt mit `CP0001`/`CP0002`-Meldungen fehl, weil eine PR die öffentliche API von `Rezepte.Import.Abstractions` oder `Rezepte.Import.PluginSdk` erweitert hat, ist das beabsichtigt: Der Vergleich läuft im `--strict-mode` und meldet auch additive Änderungen. Das Vorgehen zum Aufloesen (Versions-Bump, neue Baseline, betroffene Tests) ist im Abschnitt „Öffentliche API im Plugin-Vertrag ändern (Breaking-Change-Workflow)" in [Import-Plugins](import-plugins.md) beschrieben.

## Staging-Branch-CI

Der Workflow `.github/workflows/staging-ci.yml` startet für jeden Push auf `staging`. Er prüft zunaechst, ob der aktuelle Tree identisch zu `main` ist (reiner Back-Merge), und fuehrt in diesem Fall keine Checks aus. Andernfalls läuft dieselbe Build-/Test-/Format-/Contract-Validierung wie im PR-Workflow.

Nach einem erfolgreichen Lauf startet `.github/workflows/staging-to-main-promotion.yml` und erstellt einen Draft-PR `staging -> main`, falls noch keiner existiert. Der eigentliche Merge in `main` erfordert weiterhin eine manuelle Freigabe.

## Quellenprüfung für main

Der Workflow `.github/workflows/verify-pr-source.yml` verhindert, dass Pull Requests von anderen Branches als `staging` nach `main` geöffnet werden. PRs aus `main` selbst (Rücksync) laufen gegen `staging` und werden durch `pr-staging-ci.yml` abgedeckt.

## Synchronisation staging mit main

Der Workflow `.github/workflows/sync-staging-with-main.yml` startet nach jedem Push auf `main` und erstellt einen PR `main -> staging`, falls `staging` hinter `main` zurückliegt. Dies hält die Release-Tags und History auf `staging` konsistent. Der Sync-PR sollte mit "Create a merge commit" gemergt werden.

## Release-Build

Der Workflow `.github/workflows/release.yml` startet bei jedem Push auf `main` sowie bei gepushten Tags im Muster `v*.*.*`. Da nur PRs aus `staging` nach `main` gelangen, entspricht ein Push auf `main` faktisch dem Merge des Promotion-PRs. `scripts/resolve-release-version.mjs` klassifiziert den Lauf anschließend als automatisches Release (Branch-Push), manuelles Release (Tag-Push) oder Asset-Reparatur.

Der Release-Job checkt den Merge-Commit aus, baut die Anwendung sowie beide Testprojekte, fuehrt Tests aus, publisht `Rezepte.Web` framework-abhängig für `net10.0` und `linux-x64` und erstellt daraus `release.zip`. Zusätzlich erzeugt `scripts/Export-ImportContract.ps1` ein separates Import-Contract-ZIP mit `contract-export.json`, Dateihashes, ZIP-SHA-256-Metadaten und ApiCompat-Baseline-DLLs. Web-ZIP und Contract-ZIP werden als getrennte GitHub-Actions-Artefakte hochgeladen.

Vor den Browser-Tests installiert der Release-Workflow die Playwright-Abhängigkeiten für Chromium über das beim Build von `Rezepte.Tests.Browser` erzeugte `playwright.ps1`-Skript. Ausserdem wird `Rezepte.Web` vorab ohne runtime-spezifisches Ausgabeziel veröffentlicht, damit die Browser-Testfixture den erwarteten Publish-Output findet.

Die Release-Tests werden projektweise gestartet:

- `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build`
- `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build`

Der Release-Workflow übergibt keine gebauten Testassemblies als freie Argumente an den Testrunner. Dadurch bleibt der Testaufruf mit dem .NET-10-Testrunner kompatibel und `Rezepte.Tests.Browser.dll` wird nicht als ungültiges Kommandozeilenargument interpretiert.

Das Contract-ZIP ist für externe Import-Plugin-Repositories credential-frei abrufbar, sobald es als Release-Asset veröffentlicht wurde. Die Release-Notizen nennen `contractVersion`, `sourceCommit`, ZIP-SHA-256, den Contract-Assetnamen und die konkrete URL `https://github.com/<owner>/<repo>/releases/download/<tag>/rezepte-import-contract-<contractVersion>.zip`. Dieselbe URL wird in der als Release-Asset veröffentlichten `contract-export.metadata.json` als `artifactUrl` ergaenzt. Actions-Artefakte aus nicht taggenden Merges bleiben CI-Artefakte und sind nicht der dokumentierte credential-freie Plugin-Importpfad. Ein normaler Plugin-Build laedt diesen Stand nicht automatisch herunter; Updates werden im Plugin-Repository manuell mit konkreter Artefakt-URL und erwartetem ZIP-SHA-256 ausgefuehrt.

## Versionierung

Die Versionierung basiert auf Git-Tags im Format `vMAJOR.MINOR.PATCH`.

- Wenn noch kein passender Tag existiert, wird `1.0.0` verwendet.
- `feat:` erhoeht die Minor-Version.
- `fix:` erhoeht die Patch-Version.
- `type!:` oder `BREAKING CHANGE:` erhoeht die Major-Version.
- Andere Commit-Typen erzeugen keinen neuen SemVer-Tag.

Bei einer neuen SemVer-Version erstellt der Workflow zusätzlich einen GitHub Release mit `release.zip`, dem Import-Contract-ZIP und `contract-export.metadata.json` als Assets. Für nicht SemVer-relevante Merges werden trotzdem Actions-Artefakte mit Version und Merge-Commit im Namen abgelegt.

## GitHub Pages

Der Workflow `.github/workflows/pages.yml` baut die statische Präsentationsseite (Quellen unter `site/`, assembliert durch `scripts/Build-Site.ps1` nach `artifacts/pages`) und liefert sie über GitHub Pages unter `https://martin-stromberg.github.io/Rezepte/` aus.

| Trigger | Verhalten |
|---|---|
| `pull_request` -> `staging`/`main` | Job `build` (Assemblieren + `check-site.mjs`-Validierung + `upload-pages-artifact`). Kein Deploy – PRs deployen nie. |
| `push` -> `main` | `build` + `deploy` (deckungsgleich mit `release.yml`; die Seite folgt dem Release-Stand). |
| `workflow_dispatch` | Manuelles Re-Deploy auf `main`, u. a. als Recovery nach der Pages-Aktivierung. |

Ein `paths`-Filter auf `site/**`, `Docs/screenshots/**`, `Rezepte.Web/wwwroot/icons/**`, die beiden Skripte und die Workflow-Datei verhindert Laeufe bei seitenfremden Aenderungen.

Permissions sind gestaffelt: Top-Level `contents: read`; nur der `deploy`-Job deklariert `contents: read`, `pages: write` und `id-token: write` und läuft im Environment `github-pages`. `actions/configure-pages` wird bewusst nicht verwendet – die Action bräuchte die `pages`-Permission bereits im Build-Job und würde in PR-Läufen mit 403 scheitern, solange Pages nicht aktiviert ist.

**Einmalige Voraussetzung (Maintainer-Schritt):** In den Repository-Einstellungen muss unter Settings -> Pages -> Build and deployment die Source auf "GitHub Actions" stehen. War das beim ersten Lauf auf `main` noch nicht geschehen, schlägt `deploy-pages` mit "Pages site not found" fehl; Recovery = Settings-Schritt nachholen und den Workflow per `workflow_dispatch` erneut starten. Details und Pflegeprozess: [GitHub Page](../github-pages.md).
