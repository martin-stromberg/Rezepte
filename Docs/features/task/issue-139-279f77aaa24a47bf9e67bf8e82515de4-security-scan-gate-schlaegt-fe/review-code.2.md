# Code-Review

## Status

Befunde vorhanden

## Pruefumfang

- Ausgefuehrt als Hauptagent, da in dieser Umgebung keine Unteragenten-Delegation verfuegbar war.
- Geprueft wurde der aktuelle Arbeitsbaum nach Iteration 2 gegen `HEAD`.
- Schwerpunkt gemaess Auftrag:
  - Paketversionsaenderungen in `Rezepte.Web/Rezepte.Web.csproj` und `Rezepte.Tests/Rezepte.Tests.csproj`
  - direkter `AngleSharp`-Pin und dessen Begruendung
  - `Rezepte.Web/Extensions/LoggingExtensions.cs`
  - generierte Artefakte, insbesondere `coverage-report/`

## Befunde

### 1. `coverage-report/` ist unversioniert und nicht ignoriert

**Schweregrad:** Mittel

`git status --short` weist `coverage-report/` als unversioniertes Verzeichnis aus. `git check-ignore -v coverage-report coverage-report\Summary.txt` liefert keinen Ignore-Treffer; die vorhandene `.gitignore` ignoriert zwar einzelne `coverage*.json`, `coverage*.xml` und `coverage*.info`, aber nicht das ReportGenerator-Ausgabeverzeichnis `coverage-report/`.

Damit kann das generierte Artefakt bei einem breiten Staging-Befehl wie `git add -A` versehentlich in den Commit gelangen. Das widerspricht dem Plan, nach dem generierte Coverage-/Buildartefakte nicht committed werden sollen.

**Empfehlung:** `coverage-report/` vor dem Commit entfernen oder explizit ignorieren, sofern dieses Verzeichnis regelmaessig lokal erzeugt wird. Mindestens muss vor dem Commit verifiziert werden, dass `coverage-report/` nicht gestaged ist.

## Gepruefte Punkte Ohne Befund

- `Rezepte.Web/Rezepte.Web.csproj:13` aktualisiert `Microsoft.EntityFrameworkCore.Design` von `10.0.9` auf `10.0.11`.
- `Rezepte.Web/Rezepte.Web.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.Sqlite` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.InMemory` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:18` aktualisiert `Microsoft.Extensions.Caching.Memory` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:21` aktualisiert `bunit` von `1.38.5` auf `1.40.0`.
- `Rezepte.Tests/Rezepte.Tests.csproj:22` pinnt `AngleSharp` direkt auf `1.7.3`.
- Der direkte `AngleSharp`-Pin ist nachvollziehbar begruendet: `Rezepte.Tests/obj/project.assets.json` zeigt fuer `bunit.web` `1.40.0` weiterhin die Abhaengigkeit `AngleSharp` `1.2.0`; der direkte Top-Level-Pin hebt die tatsaechliche Testprojekt-Aufloesung auf `1.7.3`, ohne produktiven Code zu veraendern.
- `dotnet list Rezepte.sln package --include-transitive` bestaetigt aktuell `AngleSharp` `1.7.3` in `Rezepte.Tests` sowie `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` in `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Tests.Browser`.
- `dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet fuer alle Projekte der Solution keine anfaelligen Pakete.
- An den CI-/Security-Scan-Dateien `.github/actions/security-scan/action.yml`, `.github/workflows/pr-staging-ci.yml`, `.github/workflows/staging-ci.yml` und `.github/workflows/security-scan.yml` gibt es keinen Diff.
- `Rezepte.Web/Extensions/LoggingExtensions.cs` hat keinen inhaltlichen Diff. `git hash-object Rezepte.Web\Extensions\LoggingExtensions.cs` entspricht `git rev-parse :Rezepte.Web/Extensions/LoggingExtensions.cs`; `git ls-files --eol` zeigt lediglich `w/mixed` bei Repository-Vorgabe `eol=lf`. Das ist kein funktionaler Befund, sollte aber vor dem Commit nicht als fachliche Aenderung behandelt werden.

## Verifikation

- `git status --short`: `coverage-report/` ist unversioniert; `Rezepte.Web/Extensions/LoggingExtensions.cs` erscheint im Status.
- `git diff -- Rezepte.Web/Rezepte.Web.csproj`: nur die EF-Core-Paketupdates auf `10.0.11`.
- `git diff -- Rezepte.Tests/Rezepte.Tests.csproj`: Paketupdates auf `10.0.11`, `bunit` `1.40.0` und direkter `AngleSharp`-Pin `1.7.3`.
- `git diff --ignore-space-at-eol -- Rezepte.Web/Extensions/LoggingExtensions.cs`: kein inhaltlicher Diff.
- `git diff --check`: keine Whitespace-Fehler; nur die Git-EOL-Warnung zu `LoggingExtensions.cs`.
- `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`: erfolgreich, 0 Warnungen, 0 Fehler.
