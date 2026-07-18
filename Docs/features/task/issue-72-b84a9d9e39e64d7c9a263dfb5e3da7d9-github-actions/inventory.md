# Bestandsaufnahme: GitHub-Actions

## Ausgangslage

Die Anforderung zielt auf zwei GitHub-Actions-Pfade:

- PR-Qualitaetssicherung fuer Pull Requests gegen `main`.
- Release-Build nach gemergtem Pull Request in `main`, inklusive `release.zip` und semantischer Versionierung ab `1.0.0`.

Stand der Bestandsaufnahme: 2026-07-18.

## Repository- und GitHub-Struktur

Vorhanden ist eine `.github`-Struktur, aber kein Workflow-Verzeichnis:

```text
.github/
  copilot-instructions.md
```

Es gibt aktuell keine `.github/workflows/*.yml` oder `.github/workflows/*.yaml`. Fuer die Anforderung muessen die Workflows also neu unter `.github/workflows/` angelegt werden.

Die vorhandenen Copilot-/Projektregeln enthalten CI- und Git-Hinweise:

- CI soll `dotnet build`, `dotnet test` und `dotnet format --verify-no-changes` ausfuehren (`.github/copilot-instructions.md:173`).
- `main` ist als stabiler Release-Branch beschrieben (`.github/copilot-instructions.md:178`).
- Conventional Commits werden empfohlen: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:` (`.github/copilot-instructions.md:182`).
- Pull Requests sollen Review-Pflicht und gruenes CI vor Merge haben (`.github/copilot-instructions.md:183`).

## .NET Solution und Projekte

Die Solution `Rezepte.sln` enthaelt sieben Projekte:

| Projekt | Typ/Rolle | Target Framework |
|---------|-----------|------------------|
| `Rezepte.Web` | ASP.NET Core / Blazor Server Webanwendung | `net10.0` |
| `Rezepte.Import.Abstractions` | gemeinsame Plugin-Vertraege/DTOs | `net10.0` |
| `Rezepte.Import.Plugins.Backup` | produktives Import-Plugin | `net10.0` |
| `Rezepte.Import.Plugins.AIFoto` | produktives KI-Foto-Import-Plugin | `net10.0` |
| `Rezepte.Import.Plugins.AIUrl` | produktives KI-URL-Import-Plugin | `net10.0` |
| `Rezepte.Tests.PluginFixture` | Test-Fixture-Projekt fuer Plugin-Tests | `net10.0` |
| `Rezepte.Tests` | xUnit-Testprojekt | `net10.0` |

Alle Projektdateien deklarieren `net10.0`. Es gibt kein `global.json`, keine `Directory.Build.props` und keine bestehende zentrale Versionsdatei.

Das Webprojekt ist das Publish-Ziel. Es referenziert `Rezepte.Import.Abstractions` und baut/kopiert die drei produktiven Import-Plugins per MSBuild-Zielen automatisch in `plugins/<Projektname>/`:

- `BuildImportPlugins`
- `CopyImportPluginsToOutput`
- `CopyImportPluginsToPublish`

Zusaetzlich uebernimmt das Webprojekt externe Plugin-Artefakte aus `external/rezepte-import-plugins-private/artifacts/plugins`, falls dieser Pfad existiert. Das ist fuer GitHub Actions unkritisch, weil die Kopieroperation an `Exists(...)` gekoppelt ist; im CI ohne privates externes Repository werden diese externen Artefakte einfach nicht eingebunden.

## Testprojekt und Testbefehl

Der dokumentierte Testbefehl ist:

```powershell
dotnet test
```

Quelle: `README.md:106`.

`Rezepte.Tests` ist ein xUnit-Projekt mit:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions`
- `Moq`
- `coverlet.collector`
- `Microsoft.EntityFrameworkCore.InMemory`

Das Testprojekt referenziert alle produktiv relevanten Projekte inklusive Webprojekt und Import-Plugins. Damit ist `dotnet test` auf Solution-Ebene der sinnvollste PR-Testlauf. In den Tests existiert ausserdem ein Deployment-/Publish-Vertragstest (`Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`), der den dokumentierten framework-abhaengigen Publish-Befehl fuer `linux-x64` prueft.

Technische Implikation: Der PR-Workflow sollte mindestens `dotnet restore`, `dotnet build --no-restore` und `dotnet test --no-build` ausfuehren. Wegen der Projektregel sollte `dotnet format --verify-no-changes` ebenfalls eingeplant werden, auch wenn die fachliche Anforderung nur Tests nennt.

## Build, Publish und Release-Artefakte

README und Installationsdokumentation beschreiben `Rezepte.Web` als Deployment-Artefakt:

- .NET SDK 10 oder neuer ist Voraussetzung (`README.md:81`).
- Typischer Publish-Befehl: `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false` (`README.md:142`, `Docs/install.md:24`).
- Alternativ self-contained: `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true` (`Docs/install.md:28`).
- Framework-abhaengige Linux-Deployments benoetigen passende .NET-10-Shared-Frameworks fuer `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App` auf dem Zielsystem (`Docs/install.md:5`).
- Build und Publish der Webanwendung bauen die drei Hauptrepository-Plugins automatisch mit und kopieren sie ins Ausgabe- bzw. Publish-Verzeichnis (`README.md:79`, `Docs/help/import-plugins.md:27`).

Fuer `release.zip` ist der naheliegende Inhalt der komplette Publish-Ordner von:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false -o <publish-dir>
```

Der ZIP-Name muss exakt `release.zip` sein. Wenn GitHub-Actions-Artefakte verwendet werden, sollte entweder das hochgeladene Artefakt selbst `release.zip` enthalten oder das Artefakt so benannt sein, dass die Zuordnung zur Version erhalten bleibt. Eindeutiger waere ein GitHub Release mit Tag `v<version>` und Asset `release.zip`; dann bleibt der geforderte Dateiname stabil und die Version ist ueber Release/Tag eindeutig.

## Versionierung

Es gibt keine bestehende Versionierungsdatei, keine Tags im Arbeitsbaum-Kontext und keine vorhandene Release-Konvention in der Dokumentation. Die einzige vorhandene Konvention ist die Empfehlung von Conventional Commits in `.github/copilot-instructions.md`.

Implikationen fuer SemVer ab `1.0.0`:

- Der Release-Workflow braucht vollstaendige Git-Historie und Tags, also `actions/checkout` mit `fetch-depth: 0`.
- Ohne vorhandenen SemVer-Tag muss die erste Release-Version `1.0.0` werden.
- Danach muss der hoechste erforderliche Bump seit dem letzten Release-Tag bestimmt werden:
  - Breaking Change: Major.
  - `feat`: Minor.
  - `fix`: Patch.
  - `chore`, `docs`, `test`, `refactor` und vergleichbare Typen: kein eigener hoeherer Bump.
- Breaking Changes sind nur maschinell erkennbar, wenn Commit-Messages Conventional-Commits-konform `!` im Typ/Skope oder `BREAKING CHANGE:` im Footer enthalten.
- Bei Squash-Merges haengt die Bump-Qualitaet an der finalen Merge-/Squash-Commit-Message. Bei Merge-Commits kann alternativ die PR-Commitliste ausgewertet werden, das ist aber komplexer und empfindlicher gegen Mischformen.
- Fuer eindeutige Artefaktzuordnung ist ein Git-Tag `v<version>` oder ein GitHub Release mit diesem Tag technisch robuster als nur ein kurzlebiges Actions-Artefakt namens `release.zip`.

## PR-Testlauf: technische Bewertung

Empfohlener Trigger:

```yaml
on:
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]
```

Damit werden neue Pull Requests gegen `main` und neue Commits in bestehenden PRs abgedeckt. Ein Testfehler laesst den Workflow automatisch fehlschlagen, sofern `dotnet test` mit Nicht-Null-Exitcode endet.

Empfohlene Job-Schritte:

1. Repository auschecken.
2. .NET 10 SDK einrichten.
3. `dotnet restore Rezepte.sln`.
4. `dotnet build Rezepte.sln --configuration Release --no-restore`.
5. `dotnet test Rezepte.sln --configuration Release --no-build`.
6. Optional gemaess Repo-Regel: `dotnet format Rezepte.sln --verify-no-changes --no-restore`.

Zu beachten:

- `dotnet format --verify-no-changes` kann bestehende Formatierungsabweichungen sichtbar machen und PRs blockieren. Das entspricht der vorhandenen Projektregel, ist aber strenger als die reine Anforderung.
- Die Tests enthalten Publish-Vertragstests. Dadurch kann `dotnet test` laenger laufen und braucht ein funktionsfaehiges .NET-10-SDK inklusive Linux-RID-Publish-Unterstuetzung.

## Main-Release nach Merge: technische Bewertung

Die Anforderung sagt "nach erfolgreichem Merge eines Pull Requests in `main`", nicht "bei jedem Push auf `main`". Der passende GitHub-Actions-Trigger ist daher:

```yaml
on:
  pull_request:
    branches: [main]
    types: [closed]
```

Mit Job-Bedingung:

```yaml
if: github.event.pull_request.merged == true
```

Damit wird ein geschlossener, aber nicht gemergter Pull Request ausgeschlossen.

Empfohlene Release-Schritte:

1. Repository mit voller Historie und Tags auschecken.
2. .NET 10 SDK einrichten.
3. SemVer-Version aus Tags und Commit-Typen bestimmen.
4. `dotnet restore Rezepte.sln`.
5. `dotnet test Rezepte.sln --configuration Release` oder zumindest Build vor Release erneut pruefen.
6. `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false -o <publish-dir>`.
7. `<publish-dir>` zu `release.zip` packen.
8. Version eindeutig zuordnen:
   - bevorzugt: Tag `v<version>` erstellen und GitHub Release mit Asset `release.zip` veroeffentlichen;
   - minimal: Actions-Artefakt hochladen und Version in Artefaktname/Job-Summary/Metadatei dokumentieren.

Fuer Tag/Release-Erzeugung sind Workflow-Permissions erforderlich:

```yaml
permissions:
  contents: write
```

Wenn nur ein Actions-Artefakt hochgeladen wird, reicht in der Regel `contents: read`; die Version muss dann anderweitig nachvollziehbar gemacht werden.

## Risiken und offene Entscheidungen

- .NET 10 muss in GitHub Actions verfuegbar sein. Der Workflow sollte explizit `dotnet-version: 10.0.x` verwenden.
- Es ist offen, ob `release.zip` nur als Actions-Artefakt oder als GitHub-Release-Asset bereitgestellt werden soll. Aus SemVer-Sicht ist GitHub Release plus Tag die klarere Loesung.
- Es ist offen, ob der Workflow einen Git-Tag erzeugen darf. Ohne Tag muss der naechste Versionsstand aus anderer Persistenz abgeleitet werden, die im Repo nicht existiert.
- Es gibt keine vorhandene Versionierungsdatei. Eine reine Tag-basierte Versionierung vermeidet Repo-Dateiaenderungen im Release-Workflow.
- Die Commit-Typ-Auswertung ist nur so verlaesslich wie die Merge-Strategie und Commit-Message-Disziplin. Branch-Protection oder PR-Titel-/Commit-Linting waeren fuer dauerhafte Qualitaet sinnvoll, sind aber nicht Teil der Mindestanforderung.
- Das Webprojekt kopiert optionale Google-Credential-Dateien ins Build-Ausgabeverzeichnis, falls sie vorhanden sind. In GitHub Actions sollten solche Dateien nicht eingecheckt und nicht kuenstlich erzeugt werden, solange Tests/Publish sie nicht verlangen.
- Externe private Plugin-Artefakte werden nur kopiert, wenn sie lokal vorhanden sind. Der Release aus GitHub Actions wird daher voraussichtlich nur die drei Hauptrepository-Plugins enthalten, sofern kein separater Schritt fuer das private Plugin-Repository eingefuehrt wird.

## Fazit fuer die Planung

Die Anforderung ist ohne bestehende CI-Basis umsetzbar. Es sollten zwei Workflows angelegt werden:

- `pr.yml` fuer PRs gegen `main` mit Restore, Build, Test und optional Format-Check.
- `release.yml` fuer `pull_request.closed` gegen `main` mit `merged == true`, SemVer-Ermittlung ab Tags, Publish von `Rezepte.Web`, ZIP-Erzeugung und Upload/Veroeffentlichung von `release.zip`.

Die technische Kernentscheidung fuer die Planung ist die Release-Ablage. Wegen der geforderten eindeutigen Zuordnung der Version zum Artefakt ist `v<version>`-Tag plus GitHub Release mit Asset `release.zip` die robusteste Variante.
