# Anforderung

## Titel

Bugfix: Release-Workflow scheitert im Testlauf mit ungueltigem Argument fuer `Rezepte.Tests.Browser.dll`

## Ausgangslage

Der GitHub-Actions-Release-Workflow fuehrt im Release-Konfigurationslauf Tests aus. Dabei werden zwei Testprojekte fuer den Testlauf gefunden:

- `Rezepte.Tests.Browser`
- `Rezepte.Tests`

Der Testlauf bricht jedoch ab, weil der Testrunner die gebaute Browser-Testassembly als ungueltiges Argument bewertet:

`/home/runner/work/Rezepte/Rezepte/Rezepte.Tests.Browser/bin/Release/net10.0/Rezepte.Tests.Browser.dll`

Die Fehlermeldung verweist darauf, die Hilfeoption fuer gueltige Argumente zu verwenden.

## Problem

Der Release-Workflow ruft den Testlauf fuer mehrere Testprojekte oder Testassemblies offenbar so auf, dass mindestens die Browser-Testassembly `Rezepte.Tests.Browser.dll` als ungueltiges Kommandozeilenargument an den Testrunner uebergeben wird.

Dadurch schlagen Release-Laeufe in GitHub Actions fehl, obwohl die Testprojekte gefunden werden.

## Ziel

Der Release-Workflow soll alle vorgesehenen Testprojekte im Release-Kontext erfolgreich ausfuehren koennen, einschliesslich `Rezepte.Tests.Browser`, ohne dass eine Testassembly als ungueltiges Argument an den Testrunner uebergeben wird.

## Erwartetes Verhalten

- Der Release-Workflow startet den Testschritt in GitHub Actions auf `ubuntu-latest`.
- Der Testschritt fuehrt die relevanten Tests aus `Rezepte.Tests.Browser` und `Rezepte.Tests` korrekt aus.
- Der Testlauf verwendet eine von .NET SDK 10.0 beziehungsweise dem verwendeten Testrunner unterstuetzte Aufrufsyntax.
- Der Testlauf bricht nicht mit der Meldung ab, dass `Rezepte.Tests.Browser.dll` ein ungueltiges Argument sei.

## Reproduktion

1. Release-Workflow in GitHub Actions ausloesen, zum Beispiel per Push, Tag oder manuellem Trigger.
2. Warten, bis der Testschritt fuer die Release-Konfiguration ausgefuehrt wird.
3. Ausgabe des Schritts pruefen, der `dotnet test` oder ein vergleichbares Testtool fuer `Rezepte.Tests.Browser` und `Rezepte.Tests` ausfuehrt.
4. Fehler beobachten: `The argument .../Rezepte.Tests.Browser.dll is invalid.`

## Umgebung

- Betriebssystem: `ubuntu-latest` auf GitHub Actions Runner
- Browser: nicht zutreffend, da CI/CD-Umgebung
- .NET: SDK/Runtime Version 10.0 laut Workflow-Log

## Vermuteter betroffener Bereich

- GitHub-Actions-Release-Workflow, voraussichtlich `.github/workflows/release.yml`
- Testaufruf im Release-Workflow
- Ermittlung oder Uebergabe mehrerer Testprojekte beziehungsweise Testassemblies
- Sonderbehandlung von Browser-Tests in `Rezepte.Tests.Browser`, falls erforderlich

## Hinweise zur Analyse

- Pruefen, ob der Workflow `dotnet test` mit mehreren `.dll`-Pfaden in einem einzelnen Aufruf startet.
- Pruefen, ob Test-Discovery-Patterns gebaute Testassemblies erfassen und diese anschliessend faelschlich wie Projekte oder gueltige Testrunner-Argumente behandeln.
- Pruefen, ob `Rezepte.Tests.Browser` ueber Projektdatei, Solution, Testliste oder separaten Aufruf getestet werden muss.
- Pruefen, ob Browser-basierte Tests zusaetzliche Vorbereitung im CI-Kontext benoetigen, zum Beispiel Browser-Installation oder Playwright/Selenium-spezifische Schritte.

## Akzeptanzkriterien

- Der Release-Workflow enthaelt einen gueltigen Testaufruf fuer die betroffenen Testprojekte.
- `Rezepte.Tests.Browser` wird nicht als ungueltiges Argument an den Testrunner uebergeben.
- `Rezepte.Tests` wird weiterhin im Release-Testlauf beruecksichtigt.
- Der Release-Testschritt ist mit .NET SDK/Runtime 10.0 kompatibel.
- Der Workflow kann in GitHub Actions auf `ubuntu-latest` bis ueber den Testschritt hinaus erfolgreich laufen, sofern die Tests selbst fachlich erfolgreich sind.

## Nicht-Ziele

- Keine fachliche Aenderung an Rezept-, Import- oder UI-Funktionalitaet.
- Keine Entfernung der Browser-Tests aus dem Release-Workflow, sofern sie technisch ausfuehrbar sind.
- Keine dauerhafte Deaktivierung des Release-Testschritts.
