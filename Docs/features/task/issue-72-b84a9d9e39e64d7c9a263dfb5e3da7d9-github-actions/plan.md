# Umsetzungsplan: GitHub Actions

## Zielbild

Es werden zwei neue Workflows unter `.github/workflows/` angelegt:

- `pr.yml`: prueft Pull Requests gegen `main` bei Erstellung, Wiedereroeffnung und neuen Commits.
- `release.yml`: baut nach jedem Push auf `main` das Projekt `Rezepte.Web`, erzeugt `release.zip` und ordnet das Artefakt einer SemVer-Version zu.

Der Release-Workflow verwendet ausschliesslich den automatisch vorhandenen `GITHUB_TOKEN`. Es werden keine zusaetzlichen App-Secrets eingeplant.

## Umsetzungsschritte

1. Verzeichnis `.github/workflows/` anlegen.
2. Pull-Request-Workflow `.github/workflows/pr.yml` erstellen.
3. Release-Workflow `.github/workflows/release.yml` erstellen.
4. Workflows lokal syntaktisch pruefen, soweit ohne GitHub-Runner moeglich.
5. Plan- und Implementierungsartefakte nicht mit bestehenden, fremden Aenderungen vermischen.

## Pull-Request-Workflow

Datei: `.github/workflows/pr.yml`

Trigger:

```yaml
on:
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]
```

Job-Konzept:

- Runner: `ubuntu-latest`.
- Checkout mit Standard-Historie, weil fuer PR-Restore/Build/Test keine Tags noetig sind.
- .NET SDK: `10.0.x` via `actions/setup-dotnet`.
- Befehle auf Solution-Ebene:
  - `dotnet restore Rezepte.sln`
  - `dotnet build Rezepte.sln --configuration Release --no-restore`
  - `dotnet test Rezepte.sln --configuration Release --no-build`
  - `dotnet format Rezepte.sln --verify-no-changes --no-restore`

Der Format-Check wird eingeplant, weil die vorhandenen Projektregeln CI mit `dotnet format --verify-no-changes` verlangen. Falls `dotnet test` oder ein anderer Schritt fehlschlaegt, schlaegt der GitHub-Actions-Lauf automatisch fehl.

Empfohlene Zusatzoptionen:

- `timeout-minutes: 20` fuer den PR-Job.
- `concurrency` pro PR-Ref, damit veraltete Laeufe bei neuen Commits abgebrochen werden:

```yaml
concurrency:
  group: pr-${{ github.event.pull_request.number }}
  cancel-in-progress: true
```

## Release-Workflow

Datei: `.github/workflows/release.yml`

Trigger:

```yaml
on:
  push:
    branches: [main]
```

Begruendung: Ein Merge in `main` erzeugt einen Push auf `main`; ein nur geschlossener, nicht gemergter Pull Request erzeugt keinen Main-Push und startet damit keinen Release-Build. Direkte Pushes auf `main` werden ebenfalls gebaut, was zur formulierten Variante "Merge bzw. Push auf main" passt.

Workflow-Permissions:

```yaml
permissions:
  contents: write
```

`contents: write` ist fuer das Erstellen von Tags und optionalen GitHub Releases mit dem eingebauten `GITHUB_TOKEN` erforderlich. Weitere Secrets sind nicht erforderlich.

Job-Konzept:

- Runner: `ubuntu-latest`.
- Checkout mit voller Historie und Tags:

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

- .NET SDK: `10.0.x`.
- Version bestimmen.
- Restore, Build/Test und Publish ausfuehren.
- Publish-Ordner zu `release.zip` packen.
- `release.zip` als Actions-Artefakt hochladen.
- Bei neuer SemVer-Version Tag `v<version>` erzeugen und ein GitHub Release mit Asset `release.zip` erstellen.

Publish-Befehl:

```bash
dotnet publish Rezepte.Web/Rezepte.Web.csproj \
  --configuration Release \
  --framework net10.0 \
  --runtime linux-x64 \
  --self-contained false \
  --output artifacts/publish
```

ZIP-Erzeugung:

```bash
cd artifacts/publish
zip -r ../release.zip .
```

Das hochgeladene Actions-Artefakt soll den stabilen Dateinamen `release.zip` enthalten. Der Artefaktname selbst soll Version und Commit enthalten, zum Beispiel `release-1.0.0-${{ github.sha }}`, damit auch Actions-Artefakte eindeutig zuordenbar bleiben.

## Versionierung

Die Versionierung erfolgt tag-basiert mit Tags im Format `vMAJOR.MINOR.PATCH`.

Regeln:

- Wenn kein SemVer-Tag existiert, ist die erste Version `1.0.0`.
- Danach wird seit dem letzten SemVer-Tag die hoechste erforderliche Aenderung aus den Commit-Messages bestimmt:
  - Major bei `type!:` oder Footer `BREAKING CHANGE:`.
  - Minor bei `feat` ohne Breaking Change.
  - Patch bei `fix` ohne Major/Minor.
  - Keine SemVer-Erhoehung bei `chore`, `docs`, `test`, `refactor` und sonstigen nicht relevanten Typen.
- Wenn seit dem letzten Tag keine SemVer-relevanten Commits vorhanden sind, wird kein neuer SemVer-Tag erzeugt. Der Workflow baut trotzdem `release.zip` und laedt es als Actions-Artefakt mit Version, Commit-SHA und Run-ID in der Metadatenzuordnung hoch.

Geplanter robuster Implementierungsansatz:

1. Letzten SemVer-Tag ermitteln:

```bash
last_tag="$(git tag --list 'v[0-9]*.[0-9]*.[0-9]*' --sort=-v:refname | head -n 1)"
```

2. Commit-Range bestimmen:

- Ohne Tag: alle Commits bis `HEAD`.
- Mit Tag: `last_tag..HEAD`.

3. Commit-Messages auswerten:

```bash
git log --format=%B "$range"
```

4. Bump-Prioritaet bestimmen:

- Breaking Change gewinnt immer.
- Danach `feat`.
- Danach `fix`.
- Sonst `none`.

5. Version berechnen:

- Kein vorheriger Tag: `1.0.0`.
- Major: `MAJOR+1.0.0`.
- Minor: `MAJOR.MINOR+1.0`.
- Patch: `MAJOR.MINOR.PATCH+1`.
- None: letzte Version beibehalten, keinen neuen Tag erzeugen.

6. Version in GitHub-Actions-Outputs schreiben, zum Beispiel:

```bash
echo "version=$version" >> "$GITHUB_OUTPUT"
echo "create_tag=$create_tag" >> "$GITHUB_OUTPUT"
```

7. Eine kleine `release-metadata.json` in den Publish-Ordner schreiben, bevor gezippt wird:

```json
{
  "version": "1.0.0",
  "commit": "<sha>",
  "runId": "<github.run_id>",
  "createdAt": "<utc timestamp>"
}
```

Damit bleibt die Zuordnung auch dann eindeutig, wenn fuer nicht SemVer-relevante Main-Pushes kein neuer Tag entsteht.

## GitHub Release und Artefaktablage

Der Workflow soll immer `release.zip` als Actions-Artefakt hochladen:

```yaml
- uses: actions/upload-artifact@v4
  with:
    name: release-${{ steps.version.outputs.version }}-${{ github.sha }}
    path: artifacts/release.zip
```

Wenn `create_tag == true`, soll zusaetzlich ein Git-Tag und ein GitHub Release erzeugt werden. Das kann ohne externe Actions mit GitHub CLI erfolgen, weil `gh` auf `ubuntu-latest` verfuegbar ist und `GITHUB_TOKEN` genutzt werden kann:

```bash
git tag "v$version"
git push origin "v$version"
gh release create "v$version" artifacts/release.zip \
  --title "v$version" \
  --notes "Automated release for $GITHUB_SHA"
```

So ist `release.zip` sowohl als laufbezogenes Actions-Artefakt verfuegbar als auch bei SemVer-relevanten Releases dauerhaft am Tag `v<version>` abgelegt.

## Tests und Validierung

Nach der Implementierung sind folgende Pruefungen auszufuehren:

- `dotnet restore Rezepte.sln`
- `dotnet build Rezepte.sln --configuration Release --no-restore`
- `dotnet test Rezepte.sln --configuration Release --no-build`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore`

Zusaetzlich:

- YAML-Dateien auf korrekte Einrueckung und gueltige Action-Versionen pruefen.
- Sicherstellen, dass `release.yml` `fetch-depth: 0` verwendet.
- Sicherstellen, dass `release.zip` aus dem Publish-Ordner von `Rezepte.Web` erzeugt wird.
- Sicherstellen, dass die Versionierungslogik beim fehlenden Tag `1.0.0` liefert.
- Sicherstellen, dass die Workflow-Dateien keine zusaetzlichen Secrets referenzieren.

## Risiken und Gegenmassnahmen

- Risiko: `dotnet format --verify-no-changes` kann vorhandene Formatierungsabweichungen sichtbar machen.
  Gegenmassnahme: Das ist durch die vorhandene Repo-Regel gewollt; falls es bestehende Abweichungen gibt, werden sie separat sichtbar statt im Workflow versteckt.

- Risiko: Conventional Commits sind nur empfohlen, nicht technisch erzwungen.
  Gegenmassnahme: Die Release-Logik wertet robuste Standardmuster aus (`feat`, `fix`, `!`, `BREAKING CHANGE:`). Nicht passende Messages fuehren zu keinem SemVer-Bump statt zu einem falschen hoeheren Bump.

- Risiko: Bei nicht SemVer-relevanten Main-Pushes kann kein neuer Tag erzeugt werden, ohne die SemVer-Regeln zu verletzen.
  Gegenmassnahme: Der Build laedt trotzdem `release.zip` als eindeutig benanntes Actions-Artefakt hoch und schreibt Metadaten mit Version, Commit und Run-ID in das ZIP.

- Risiko: .NET 10 muss auf GitHub Actions verfuegbar sein.
  Gegenmassnahme: Der Workflow pinnt `actions/setup-dotnet` auf `dotnet-version: 10.0.x`.

## Betroffene Dateien

- Neu: `.github/workflows/pr.yml`
- Neu: `.github/workflows/release.yml`
- Neu: `docs/features/task/issue-72-b84a9d9e39e64d7c9a263dfb5e3da7d9-github-actions/plan.md`

## Offene Punkte

Keine.
