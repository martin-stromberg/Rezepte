# Importarchitektur und Laufzeitfluss

## Ausgangslage

Der Import besteht aus zwei Pfaden:

- Synchroner Legacy-Pfad ueber `IImportService.ImportAsync(...)` in `Rezepte.Web/Services/Import/ImportService.cs`.
- Sessionbasierter Pfad ueber `ImportOrchestrator.StartImportAsync(...)` in `Rezepte.Web/Services/Import/ImportOrchestrator.cs`.

Die aktuelle UI nutzt fuer Datei- und URL-Import bereits den sessionbasierten Pfad. Dieser ist fuer die Anforderung entscheidend, weil nur er einen Hintergrundlauf und Pollingstatus abbildet.

## Orchestrator

`ImportOrchestrator` verwaltet eine `ConcurrentDictionary<string, ImportSession>` und speichert pro Session aktuell:

- `Status`
- `WaitingForConfirmation`
- `ConfirmationPrompt`
- `ConfirmationTcs`
- `Result`

Relevante Stellen:

- `ImportSession` ist in `Rezepte.Web/Services/Import/ImportOrchestrator.cs:23` definiert.
- `StartImportAsync(...)` startet die Hintergrundverarbeitung in `Rezepte.Web/Services/Import/ImportOrchestrator.cs:34`.
- Interaktive Handler werden in `Rezepte.Web/Services/Import/ImportOrchestrator.cs:93` erkannt.
- `SessionInteraction` implementiert `IImportInteraction` ab `Rezepte.Web/Services/Import/ImportOrchestrator.cs:171`.

## Plugin-Auswahl

Der Orchestrator fragt `IPluginManager.GetActiveHandlersAsync(...)` ab und prueft Handler der Reihe nach:

1. `handler.UserId = userId`
2. Streamposition auf Start setzen
3. `CanHandleAsync(...)`
4. erster passender Handler verarbeitet den Import
5. Ergebnis wird persistiert

Das Verhalten ist in `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs` abgesichert, insbesondere Reihenfolge und Abbruch nach einem passenden fehlerhaften Plugin.

## Relevanz fuer Sammlungen

Der Orchestrator ist der richtige Ort fuer den Laufzustand, weil die UI den Sessionstatus ohnehin pollt. Fuer Massenimport fehlen dort:

- Sammlungsvorschau als strukturierter Sessionzustand.
- Auswahlantwort mit Rezept-IDs/URLs und Zielkochbuch je Eintrag.
- Per-Rezept-Status: `Pending`, `Importing`, `Success`, `Failed`.
- Fehlermeldung pro Sammlungseintrag.
- Zustand "Dialog geschlossen, Import laeuft weiter" ohne Sessionabbruch.

## Rueckwaertskompatibilitaet

Bestehende Einzelrezept-Plugins sollten weiter ueber `IImportHandler` funktionieren. Der neue Sammlungsfluss sollte additiv sein, z. B. ueber ein optionales Interface fuer collection-faehige Handler oder eine Erweiterung des interaktiven Importvertrags. Der synchrone `IImportService`-Pfad sollte nicht zur primaeren Umsetzung fuer Sammlungen werden.

