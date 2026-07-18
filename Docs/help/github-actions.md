# GitHub Actions

Das Repository enthaelt zwei Workflows fuer Pull Requests und Releases.

## Pull-Request-Pruefung

Der Workflow `.github/workflows/pr.yml` startet fuer Pull Requests gegen `main`, wenn ein PR erstellt, wieder geoeffnet oder durch neue Commits aktualisiert wird.

Der Workflow fuehrt auf `ubuntu-latest` mit .NET `10.0.x` diese Pruefungen aus:

- `dotnet restore Rezepte.sln`
- `dotnet build Rezepte.sln --configuration Release --no-restore`
- `dotnet test Rezepte.sln --configuration Release --no-build`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore`

Neue Commits in einem bestehenden Pull Request brechen veraltete PR-Laeufe ab und starten die Pruefung fuer den aktuellen Stand neu.

## Release-Build

Der Workflow `.github/workflows/release.yml` startet, wenn ein Pull Request gegen `main` geschlossen und tatsaechlich gemergt wurde. Geschlossene, nicht gemergte Pull Requests loesen keinen Release-Build aus.

Der Release-Job checkt den Merge-Commit aus, fuehrt Tests aus, publisht `Rezepte.Web` framework-abhaengig fuer `net10.0` und `linux-x64` und erstellt daraus `release.zip`. Das ZIP wird als GitHub-Actions-Artefakt hochgeladen.

## Versionierung

Die Versionierung basiert auf Git-Tags im Format `vMAJOR.MINOR.PATCH`.

- Wenn noch kein passender Tag existiert, wird `1.0.0` verwendet.
- `feat:` erhoeht die Minor-Version.
- `fix:` erhoeht die Patch-Version.
- `type!:` oder `BREAKING CHANGE:` erhoeht die Major-Version.
- Andere Commit-Typen erzeugen keinen neuen SemVer-Tag.

Bei einer neuen SemVer-Version erstellt der Workflow zusaetzlich einen GitHub Release mit `release.zip` als Asset. Fuer nicht SemVer-relevante Merges wird trotzdem ein Actions-Artefakt mit Version und Merge-Commit im Namen abgelegt.
