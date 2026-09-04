# Code-Review

## Status

Keine Befunde

## Pruefumfang

- Ausgefuehrt als Hauptagent, da in dieser Umgebung keine Unteragenten-Delegation verfuegbar war.
- Geprueft wurde der aktuelle Arbeitsbaum nach Iteration 3 gegen `HEAD`.
- Schwerpunkt gemaess Auftrag:
  - vorheriger Befund zu `coverage-report/` und Absicherung ueber `.gitignore`
  - Paketversionsaenderungen in `Rezepte.Web/Rezepte.Web.csproj` und `Rezepte.Tests/Rezepte.Tests.csproj`
  - direkter `AngleSharp`-Pin
  - `Rezepte.Web/Extensions/LoggingExtensions.cs`

## Befunde

Keine.

## Gepruefte Punkte Ohne Befund

- `.gitignore:149` ignoriert jetzt `coverage-report/`. `git check-ignore -v coverage-report coverage-report\Summary.txt` trifft fuer beide Pfade auf diese Regel; `git status --short --ignored coverage-report` zeigt das Verzeichnis nur noch als ignoriert (`!! coverage-report/`). Der Befund aus Iteration 2 ist damit behoben.
- `Rezepte.Web/Rezepte.Web.csproj:13` aktualisiert `Microsoft.EntityFrameworkCore.Design` von `10.0.9` auf `10.0.11`.
- `Rezepte.Web/Rezepte.Web.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.Sqlite` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:17` aktualisiert `Microsoft.EntityFrameworkCore.InMemory` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:18` aktualisiert `Microsoft.Extensions.Caching.Memory` von `10.0.9` auf `10.0.11`.
- `Rezepte.Tests/Rezepte.Tests.csproj:21` aktualisiert `bunit` von `1.38.5` auf `1.40.0`.
- `Rezepte.Tests/Rezepte.Tests.csproj:22` pinnt `AngleSharp` direkt auf `1.7.3`.
- Der direkte `AngleSharp`-Pin ist weiterhin nachvollziehbar: `Rezepte.Tests/obj/project.assets.json` zeigt fuer `bunit.web` `1.40.0` weiterhin die Abhaengigkeit `AngleSharp` `1.2.0`; die tatsaechliche Aufloesung im Testprojekt ist durch den Top-Level-Pin `AngleSharp` `1.7.3`.
- `dotnet list Rezepte.sln package --include-transitive` bestaetigt `AngleSharp` `1.7.3` in `Rezepte.Tests` sowie `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` in `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Tests.Browser`.
- `dotnet list Rezepte.sln package --vulnerable --include-transitive` meldet fuer alle Projekte der Solution keine anfaelligen Pakete.
- `Rezepte.Web/Extensions/LoggingExtensions.cs` hat keinen funktionalen Diff: `git diff --ignore-space-at-eol -- Rezepte.Web\Extensions\LoggingExtensions.cs` ist leer, und `git hash-object Rezepte.Web\Extensions\LoggingExtensions.cs` entspricht `git rev-parse :Rezepte.Web/Extensions/LoggingExtensions.cs`. `git ls-files --eol` zeigt weiterhin nur `w/mixed` bei Repository-Vorgabe `eol=lf`.

## Verifikation

- `git diff -- .gitignore Rezepte.Web\Rezepte.Web.csproj Rezepte.Tests\Rezepte.Tests.csproj Rezepte.Web\Extensions\LoggingExtensions.cs`: nur die erwartete Ignore-Regel und Paketupdates; `LoggingExtensions.cs` erzeugt lediglich die Git-EOL-Warnung.
- `git check-ignore -v coverage-report coverage-report\Summary.txt`: beide Pfade werden durch `.gitignore:149:coverage-report/` ignoriert.
- `git status --short --ignored coverage-report`: `!! coverage-report/`.
- `dotnet list Rezepte.sln package --include-transitive`: relevante Aufloesungen wie oben dokumentiert.
- `dotnet list Rezepte.sln package --vulnerable --include-transitive`: keine anfaelligen Pakete.
- `git diff --check`: keine Whitespace-Fehler; nur die Git-EOL-Warnung zu `LoggingExtensions.cs`.
- `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`: erfolgreich, 0 Warnungen, 0 Fehler.
