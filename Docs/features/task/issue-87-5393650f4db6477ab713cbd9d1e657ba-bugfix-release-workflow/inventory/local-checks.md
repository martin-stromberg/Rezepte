# Detail: Lokale Pruefungen

## Ausgefuehrte Pruefungen

Arbeitsbranch:

```text
task/issue-87-5393650f4db6477ab713cbd9d1e657ba-bugfix-release-workflow
```

Lokales SDK:

```text
.NET SDK 10.0.302
Host 10.0.10
OS Windows win-x64
global.json nicht vorhanden
```

Lokale `dotnet test --help`-Ausgabe beschreibt den VSTest-basierten Befehl. Die allgemeine Nutzung ist:

```text
dotnet test [options] [[--] <additional arguments>...]
```

Der Hilfetext weist ausserdem darauf hin, dass Microsoft.Testing.Platform ueber `global.json` aktiviert wird. In diesem Repository gibt es keine `global.json`.

## Dateisuche

Gefundene relevante Testaufrufe:

- `.github/workflows/pr.yml`: `dotnet test Rezepte.sln --configuration Release --no-build`
- `.github/workflows/release.yml`: `dotnet test Rezepte.sln --configuration Release --no-build`
- `README.md`: allgemeiner lokaler Hinweis `dotnet test`
- `Docs/help/github-actions.md`: dokumentierter CI-Testaufruf `dotnet test Rezepte.sln --configuration Release --no-build`

Gefundene Browser-Testvorbereitung:

- Nur `.github/workflows/pr.yml` sucht `playwright.ps1` unter `Rezepte.Tests.Browser/bin/Release`.
- Nur `.github/workflows/pr.yml` ruft `pwsh "$PLAYWRIGHT_SCRIPT" install --with-deps chromium` auf.
- Nur `.github/workflows/pr.yml` publisht `Rezepte.Web` vor dem Solution-Testlauf.

## Nicht ausgefuehrt

Es wurde fuer diese Bestandsaufnahme kein kompletter Restore-/Build-/Testlauf ausgefuehrt. Die Inventur basiert auf statischer Analyse der Workflow-, Solution- und Projektdateien sowie auf der lokalen SDK-Information.

## Hinweise fuer die Umsetzung

- Falls lokal validiert wird, kann ein Windows-Lauf die GitHub-Actions-Linux-Umgebung nur begrenzt abbilden. Entscheidend bleibt `ubuntu-latest`.
- Ein lokaler Testlauf kann trotzdem Syntax- und Buildprobleme finden, insbesondere fuer explizite Projektaufrufe.
- Bei Aenderungen am Release-Workflow sollte die YAML-Datei nach Moeglichkeit so strukturiert werden, dass die Browser-Testvorbereitung dem PR-Workflow entspricht und keine Testassembly-Pfade manuell an den Testrunner uebergeben werden.
