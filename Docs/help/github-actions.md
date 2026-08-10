# GitHub Actions

Das Repository enthaelt Workflows fuer den zweistufigen Staging-Flow.

## Branch-Modell

- Feature-Branches oeffnen Pull Requests gegen `staging`.
- Nach erfolgreicher CI auf `staging` wird automatisch ein Draft-PR `staging -> main` erstellt.
- Nur PRs aus `staging` sind nach `main` erlaubt.
- Nach jedem Merge/Push auf `main` wird ein Sync-PR `main -> staging` erstellt, damit Release-Tags auf `staging` erreichbar bleiben.
- `main` bleibt fuer stabile Releases und erzeugt `release.zip`.

## Pull-Request-Pruefung auf staging

Der Workflow `.github/workflows/pr.yml` startet fuer Pull Requests gegen `staging`, wenn ein PR erstellt, wieder geoeffnet oder durch neue Commits aktualisiert wird.

Reine Back-Merge-PRs (`main -> staging`) erkennt der Workflow an `github.head_ref == 'main'` und ueberspringen alle weiteren Checks.

Fuer alle anderen PRs fuehrt der Workflow auf `ubuntu-latest` mit .NET `10.0.x` diese Pruefungen aus:

- `dotnet restore Rezepte.sln`
- `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
- Playwright-Browser-Installation
- `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
- `dotnet test Rezepte.sln --configuration Release --no-build`
- `./scripts/Export-ImportContract.ps1 -OutputDirectory artifacts/contract-export`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore`

Neue Commits in einem bestehenden Pull Request brechen veraltete PR-Laeufe ab und starten die Pruefung fuer den aktuellen Stand neu.

Wenn gespeicherte ApiCompat-Referenzen unter `contract-baselines/import-contract/<semver>/` vorhanden sind, installiert der Workflow `Microsoft.DotNet.ApiCompat.Tool` und der Contract-Export muss gegen `Rezepte.Import.Abstractions.dll` und `Rezepte.Import.PluginSdk.dll` aus der ausgewaehlten Baseline bestehen. Ohne explizite `-ApiCompatBaselineVersion` verwendet das Skript die neueste gespeicherte SemVer-Baseline bis zur aktuellen Contract-Version; ohne passende gespeicherte Baseline wird der historische Vergleich protokolliert uebersprungen.

Schlaegt dieser Schritt mit `CP0001`/`CP0002`-Meldungen fehl, weil eine PR die oeffentliche API von `Rezepte.Import.Abstractions` oder `Rezepte.Import.PluginSdk` erweitert hat, ist das beabsichtigt: Der Vergleich laeuft im `--strict-mode` und meldet auch additive Aenderungen. Das Vorgehen zum Aufloesen (Versions-Bump, neue Baseline, betroffene Tests) ist im Abschnitt „Oeffentliche API im Plugin-Vertrag aendern (Breaking-Change-Workflow)" in [Import-Plugins](import-plugins.md) beschrieben.

## Staging-Branch-CI

Der Workflow `.github/workflows/staging-ci.yml` startet fuer jeden Push auf `staging`. Er prueft zunaechst, ob der aktuelle Tree identisch zu `main` ist (reiner Back-Merge), und fuehrt in diesem Fall keine Checks aus. Andernfalls laeuft dieselbe Build-/Test-/Format-/Contract-Validierung wie im PR-Workflow.

Nach einem erfolgreichen Lauf startet `.github/workflows/staging-to-main-promotion.yml` und erstellt einen Draft-PR `staging -> main`, falls noch keiner existiert. Der eigentliche Merge in `main` erfordert weiterhin eine manuelle Freigabe.

## Quellenpruefung fuer main

Der Workflow `.github/workflows/verify-pr-source.yml` verhindert, dass Pull Requests von anderen Branches als `staging` nach `main` geoeffnet werden. PRs aus `main` selbst (Ruecksync) laufen gegen `staging` und werden durch `pr.yml` abgedeckt.

## Synchronisation staging mit main

Der Workflow `.github/workflows/sync-staging-with-main.yml` startet nach jedem Push auf `main` und erstellt einen PR `main -> staging`, falls `staging` hinter `main` zurueckliegt. Dies haelt die Release-Tags und History auf `staging` konsistent. Der Sync-PR sollte mit "Create a merge commit" gemergt werden.

## Release-Build

Der Workflow `.github/workflows/release.yml` startet, wenn ein Pull Request gegen `main` geschlossen und tatsaechlich gemergt wurde. Geschlossene, nicht gemergte Pull Requests loesen keinen Release-Build aus.

Der Release-Job checkt den Merge-Commit aus, baut die Anwendung sowie beide Testprojekte, fuehrt Tests aus, publisht `Rezepte.Web` framework-abhaengig fuer `net10.0` und `linux-x64` und erstellt daraus `release.zip`. Zusaetzlich erzeugt `scripts/Export-ImportContract.ps1` ein separates Import-Contract-ZIP mit `contract-export.json`, Dateihashes, ZIP-SHA-256-Metadaten und ApiCompat-Baseline-DLLs. Web-ZIP und Contract-ZIP werden als getrennte GitHub-Actions-Artefakte hochgeladen.

Vor den Browser-Tests installiert der Release-Workflow die Playwright-Abhaengigkeiten fuer Chromium ueber das beim Build von `Rezepte.Tests.Browser` erzeugte `playwright.ps1`-Skript. Ausserdem wird `Rezepte.Web` vorab ohne runtime-spezifisches Ausgabeziel veroeffentlicht, damit die Browser-Testfixture den erwarteten Publish-Output findet.

Die Release-Tests werden projektweise gestartet:

- `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build`
- `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build`

Der Release-Workflow uebergibt keine gebauten Testassemblies als freie Argumente an den Testrunner. Dadurch bleibt der Testaufruf mit dem .NET-10-Testrunner kompatibel und `Rezepte.Tests.Browser.dll` wird nicht als ungueltiges Kommandozeilenargument interpretiert.

Das Contract-ZIP ist fuer externe Import-Plugin-Repositories credential-frei abrufbar, sobald es als Release-Asset veroeffentlicht wurde. Die Release-Notizen nennen `contractVersion`, `sourceCommit`, ZIP-SHA-256, den Contract-Assetnamen und die konkrete URL `https://github.com/<owner>/<repo>/releases/download/<tag>/rezepte-import-contract-<contractVersion>.zip`. Dieselbe URL wird in der als Release-Asset veroeffentlichten `contract-export.metadata.json` als `artifactUrl` ergaenzt. Actions-Artefakte aus nicht taggenden Merges bleiben CI-Artefakte und sind nicht der dokumentierte credential-freie Plugin-Importpfad. Ein normaler Plugin-Build laedt diesen Stand nicht automatisch herunter; Updates werden im Plugin-Repository manuell mit konkreter Artefakt-URL und erwartetem ZIP-SHA-256 ausgefuehrt.

## Versionierung

Die Versionierung basiert auf Git-Tags im Format `vMAJOR.MINOR.PATCH`.

- Wenn noch kein passender Tag existiert, wird `1.0.0` verwendet.
- `feat:` erhoeht die Minor-Version.
- `fix:` erhoeht die Patch-Version.
- `type!:` oder `BREAKING CHANGE:` erhoeht die Major-Version.
- Andere Commit-Typen erzeugen keinen neuen SemVer-Tag.

Bei einer neuen SemVer-Version erstellt der Workflow zusaetzlich einen GitHub Release mit `release.zip`, dem Import-Contract-ZIP und `contract-export.metadata.json` als Assets. Fuer nicht SemVer-relevante Merges werden trotzdem Actions-Artefakte mit Version und Merge-Commit im Namen abgelegt.
