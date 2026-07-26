# GitHub Actions

Das Repository enthaelt zwei Workflows fuer Pull Requests und Releases.

## Pull-Request-Pruefung

Der Workflow `.github/workflows/pr.yml` startet fuer Pull Requests gegen `main`, wenn ein PR erstellt, wieder geoeffnet oder durch neue Commits aktualisiert wird.

Der Workflow fuehrt auf `ubuntu-latest` mit .NET `10.0.x` diese Pruefungen aus:

- `dotnet restore Rezepte.sln`
- `dotnet build Rezepte.sln --configuration Release --no-restore`
- `dotnet test Rezepte.sln --configuration Release --no-build`
- `./scripts/Export-ImportContract.ps1 -OutputDirectory artifacts/contract-export`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore`

Neue Commits in einem bestehenden Pull Request brechen veraltete PR-Laeufe ab und starten die Pruefung fuer den aktuellen Stand neu.

Wenn gespeicherte ApiCompat-Referenzen unter `contract-baselines/import-contract/<semver>/` vorhanden sind, installiert der Workflow `Microsoft.DotNet.ApiCompat.Tool` und der Contract-Export muss gegen `Rezepte.Import.Abstractions.dll` und `Rezepte.Import.PluginSdk.dll` aus der ausgewaehlten Baseline bestehen. Ohne explizite `-ApiCompatBaselineVersion` verwendet das Skript die neueste gespeicherte SemVer-Baseline bis zur aktuellen Contract-Version; ohne passende gespeicherte Baseline wird der historische Vergleich protokolliert uebersprungen.

Schlaegt dieser Schritt mit `CP0001`/`CP0002`-Meldungen fehl, weil eine PR die oeffentliche API von `Rezepte.Import.Abstractions` oder `Rezepte.Import.PluginSdk` erweitert hat, ist das beabsichtigt: Der Vergleich laeuft im `--strict-mode` und meldet auch additive Aenderungen. Das Vorgehen zum Aufloesen (Versions-Bump, neue Baseline, betroffene Tests) ist im Abschnitt „Oeffentliche API im Plugin-Vertrag aendern (Breaking-Change-Workflow)" in [Import-Plugins](import-plugins.md) beschrieben.

## Release-Build

Der Workflow `.github/workflows/release.yml` startet, wenn ein Pull Request gegen `main` geschlossen und tatsaechlich gemergt wurde. Geschlossene, nicht gemergte Pull Requests loesen keinen Release-Build aus.

Der Release-Job checkt den Merge-Commit aus, fuehrt Tests aus, publisht `Rezepte.Web` framework-abhaengig fuer `net10.0` und `linux-x64` und erstellt daraus `release.zip`. Zusaetzlich erzeugt `scripts/Export-ImportContract.ps1` ein separates Import-Contract-ZIP mit `contract-export.json`, Dateihashes, ZIP-SHA-256-Metadaten und ApiCompat-Baseline-DLLs. Web-ZIP und Contract-ZIP werden als getrennte GitHub-Actions-Artefakte hochgeladen.

Das Contract-ZIP ist fuer externe Import-Plugin-Repositories credential-frei abrufbar, sobald es als Release-Asset veroeffentlicht wurde. Die Release-Notizen nennen `contractVersion`, `sourceCommit`, ZIP-SHA-256, den Contract-Assetnamen und die konkrete URL `https://github.com/<owner>/<repo>/releases/download/<tag>/rezepte-import-contract-<contractVersion>.zip`. Dieselbe URL wird in der als Release-Asset veroeffentlichten `contract-export.metadata.json` als `artifactUrl` ergaenzt. Actions-Artefakte aus nicht taggenden Merges bleiben CI-Artefakte und sind nicht der dokumentierte credential-freie Plugin-Importpfad. Ein normaler Plugin-Build laedt diesen Stand nicht automatisch herunter; Updates werden im Plugin-Repository manuell mit konkreter Artefakt-URL und erwartetem ZIP-SHA-256 ausgefuehrt.

## Versionierung

Die Versionierung basiert auf Git-Tags im Format `vMAJOR.MINOR.PATCH`.

- Wenn noch kein passender Tag existiert, wird `1.0.0` verwendet.
- `feat:` erhoeht die Minor-Version.
- `fix:` erhoeht die Patch-Version.
- `type!:` oder `BREAKING CHANGE:` erhoeht die Major-Version.
- Andere Commit-Typen erzeugen keinen neuen SemVer-Tag.

Bei einer neuen SemVer-Version erstellt der Workflow zusaetzlich einen GitHub Release mit `release.zip`, dem Import-Contract-ZIP und `contract-export.metadata.json` als Assets. Fuer nicht SemVer-relevante Merges werden trotzdem Actions-Artefakte mit Version und Merge-Commit im Namen abgelegt.
